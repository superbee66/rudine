using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Rudine.Util.Cabs
{
    internal class CabExtract : IDisposable
    {
        private const int CpuTypeUnknown = -1;

        /// <summary>
        /// </summary>
        private const int ExtractFileAttemptLimit = 10;

        private readonly List<CabDecompressFile> _decompressFiles;
        private readonly CabError _erf;
        private readonly FdiMemAllocDelegate _femAllocDelegate;
        private readonly FdiFileCloseDelegate _fileCloseDelegate;
        private readonly FdiFileOpenDelegate _fileOpenDelegate;
        private readonly FdiFileReadDelegate _fileReadDelegate;
        private readonly FdiFileSeekDelegate _fileSeekDelegate;
        private readonly FdiFileWriteDelegate _fileWriteDelegate;
        private readonly byte[] _inputData;
        private readonly FdiMemFreeDelegate _memFreeDelegate;
        private bool _disposed;
        private IntPtr _hfdi;

        public CabExtract(byte[] inputData)
        {
            _fileReadDelegate = FileRead;
            _fileOpenDelegate = InputFileOpen;
            _femAllocDelegate = MemAlloc;
            _fileSeekDelegate = FileSeek;
            _memFreeDelegate = MemFree;
            _fileWriteDelegate = FileWrite;
            _fileCloseDelegate = InputFileClose;
            _inputData = inputData;
            _decompressFiles = new List<CabDecompressFile>();
            _erf = new CabError();
            _hfdi = IntPtr.Zero;
        }

        private IntPtr FdiContext
        {
            get
            {
                if (_hfdi == IntPtr.Zero)
                {
                    _hfdi = FdiCreate(_femAllocDelegate,
                        _memFreeDelegate,
                        _fileOpenDelegate,
                        _fileReadDelegate,
                        _fileWriteDelegate,
                        _fileCloseDelegate,
                        _fileSeekDelegate,
                        _erf);
                    if (_hfdi == IntPtr.Zero)
                        throw new ApplicationException("Failed to create FDI context.");
                }
                return _hfdi;
            }
        }

        public void Dispose() { Dispose(true); }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_hfdi != IntPtr.Zero)
                {
                    FdiDestroy(_hfdi);
                    _hfdi = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        public bool ExtractFile(string fileName, out byte[] outputData, out int outputLength)
        {
            for (int attempt = 0; attempt < ExtractFileAttemptLimit; attempt++)
                try
                {
                    if (_disposed)
                        throw new ObjectDisposedException("CabExtract");

                    CabDecompressFile fileToDecompress = new CabDecompressFile
                    {
                        Found = false,
                        Name = fileName
                    };

                    _decompressFiles.Add(fileToDecompress);

                    FdiCopy(FdiContext, NotifyCallback);

                    if (fileToDecompress.Found)
                    {
                        outputData = fileToDecompress.Data;
                        outputLength = fileToDecompress.Length;
                        _decompressFiles.Remove(fileToDecompress);
                        return true;
                    }
                } catch (Exception ex)
                {
                    Thread.Sleep(1000);
                }

            outputData = null;
            outputLength = 0;
            return false;
        }

        //In an ideal world, this would take a stream, but Cabinet.dll seems to want to open the input several times.
        public static bool ExtractFile(byte[] inputData, string fileName, out byte[] outputData, out int length)
        {
            int i = 10;
            while (i-- != 0)
                try
                {
                    using (CabExtract decomp = new CabExtract(inputData))
                        return decomp.ExtractFile(fileName, out outputData, out length);
                } catch (Exception)
                {
                    Thread.Sleep(1000);
                }

            outputData = null;
            length = 0;
            return false;
        }

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FDICopy", CharSet = CharSet.Ansi)]
        private static extern bool FdiCopy(
            IntPtr hfdi,
            string cabinetName,
            string cabinetPath,
            int flags,
            FdiNotifyDelegate fnNotify,
            IntPtr fnDecrypt,
            IntPtr userData);

        private static bool FdiCopy(
            IntPtr hfdi,
            FdiNotifyDelegate fnNotify)
        {
            return FdiCopy(hfdi,
                "<notused>",
                "<notused>",
                0,
                fnNotify,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FDICreate", CharSet = CharSet.Ansi)]
        private static extern IntPtr FdiCreate(
            FdiMemAllocDelegate fnMemAlloc,
            FdiMemFreeDelegate fnMemFree,
            FdiFileOpenDelegate fnFileOpen,
            FdiFileReadDelegate fnFileRead,
            FdiFileWriteDelegate fnFileWrite,
            FdiFileCloseDelegate fnFileClose,
            FdiFileSeekDelegate fnFileSeek,
            int cpuType,
            [MarshalAs(UnmanagedType.LPStruct)] CabError erf);

        private static IntPtr FdiCreate(
            FdiMemAllocDelegate fnMemAlloc,
            FdiMemFreeDelegate fnMemFree,
            FdiFileOpenDelegate fnFileOpen,
            FdiFileReadDelegate fnFileRead,
            FdiFileWriteDelegate fnFileWrite,
            FdiFileCloseDelegate fnFileClose,
            FdiFileSeekDelegate fnFileSeek,
            CabError erf)
        {
            return FdiCreate(fnMemAlloc,
                fnMemFree,
                fnFileOpen,
                fnFileRead,
                fnFileWrite,
                fnFileClose,
                fnFileSeek,
                CpuTypeUnknown,
                erf);
        }

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FDIDestroy", CharSet = CharSet.Ansi)]
        private static extern bool FdiDestroy(IntPtr hfdi);

        [DllImport("cabinet.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FDIIsCabinet", CharSet = CharSet.Ansi)]
        private static extern bool FdiIsCabinet(
            IntPtr hfdi,
            IntPtr hf,
            [MarshalAs(UnmanagedType.LPStruct)] CabinetInfo cabInfo);

        private int FileRead(IntPtr hf, byte[] buffer, int cb)
        {
            Stream stream = StreamFromHandle(hf);
            return stream.Read(buffer, 0, cb);
        }

        private int FileSeek(IntPtr hf, int dist, int seektype)
        {
            Stream stream = StreamFromHandle(hf);
            return (int) stream.Seek(dist, (SeekOrigin) seektype);
        }

        private int FileWrite(IntPtr hf, byte[] buffer, int cb)
        {
            Stream stream = StreamFromHandle(hf);
            stream.Write(buffer, 0, cb);
            return cb;
        }

        private int InputFileClose(IntPtr hf)
        {
            Stream stream = StreamFromHandle(hf);
            stream.Close();
            ((GCHandle) (hf)).Free();
            return 0;
        }

        private IntPtr InputFileOpen(string fileName, int oflag, int pmode)
        {
            MemoryStream stream = new MemoryStream(_inputData);
            GCHandle gch = GCHandle.Alloc(stream);
            return (IntPtr) gch;
        }

        public bool IsCabinetFile(out CabinetInfo cabinfo)
        {
            if (_disposed)
                throw new ObjectDisposedException("CabExtract");

            MemoryStream stream = new MemoryStream(_inputData);
            GCHandle gch = GCHandle.Alloc(stream);

            try
            {
                CabinetInfo info = new CabinetInfo();
                bool ret = FdiIsCabinet(FdiContext, (IntPtr) gch, info);
                cabinfo = info;
                return ret;
            } finally
            {
                stream.Close();
                gch.Free();
            }
        }

        public static bool IsCabinetFile(byte[] inputData, out CabinetInfo cabinfo)
        {
            using (CabExtract decomp = new CabExtract(inputData))
                return decomp.IsCabinetFile(out cabinfo);
        }

        private IntPtr MemAlloc(int cb) { return Marshal.AllocHGlobal(cb); }

        private void MemFree(IntPtr mem) { Marshal.FreeHGlobal(mem); }

        private IntPtr NotifyCallback(FdiNotificationType fdint, FdiNotification fdin)
        {
            switch (fdint)
            {
                case FdiNotificationType.CopyFile:
                    return OutputFileOpen(fdin);
                case FdiNotificationType.CloseFileInfo:
                    return OutputFileClose(fdin);
                default:
                    return IntPtr.Zero;
            }
        }

        private IntPtr OutputFileClose(FdiNotification fdin)
        {
            CabDecompressFile extractFile = _decompressFiles.Single(ef => ef.Handle == fdin.hf);
            Stream stream = StreamFromHandle(fdin.hf);

            extractFile.Found = true;
            extractFile.Length = (int) stream.Length;

            if (stream.Length > 0)
            {
                extractFile.Data = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(extractFile.Data,
                    0,
                    (int) stream.Length);
            }

            stream.Close();
            return IntPtr.Zero;
        }

        private IntPtr OutputFileOpen(FdiNotification fdin)
        {
            CabDecompressFile extractFile = _decompressFiles.SingleOrDefault(ef => ef.Name == fdin.psz1);

            if (extractFile != null)
            {
                MemoryStream stream = new MemoryStream();
                GCHandle gch = GCHandle.Alloc(stream);
                extractFile.Handle = (IntPtr) gch;
                return extractFile.Handle;
            }

            //Don't extract
            return IntPtr.Zero;
        }

        private static Stream StreamFromHandle(IntPtr hf) { return (Stream) ((GCHandle) hf).Target; }

        private class CabDecompressFile
        {
            public byte[] Data { get; set; }

            public bool Found { get; set; }

            public IntPtr Handle { get; set; }

            public int Length { get; set; }

            public string Name { get; set; }
        }

        //If any of these classes end up with a different size to its C equivalent, we end up with crash and burn.
        [StructLayout(LayoutKind.Sequential)]
        private class CabError //Cabinet API: "ERF"
        {
            public int erfOper;
            public int erfType;
            public int fError;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int32 FdiFileCloseDelegate(IntPtr hf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr FdiFileOpenDelegate(string fileName, int oflag, int pmode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int32 FdiFileReadDelegate(IntPtr hf,
                                                   [In] [Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2,
                                                       ArraySubType = UnmanagedType.U1)] byte[] buffer, int cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int32 FdiFileSeekDelegate(IntPtr hf, int dist, int seektype);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int32 FdiFileWriteDelegate(IntPtr hf,
                                                    [In] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2,
                                                        ArraySubType = UnmanagedType.U1)] byte[] buffer, int cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr FdiMemAllocDelegate(int numBytes);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FdiMemFreeDelegate(IntPtr mem);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class FdiNotification //Cabinet API: "FDINOTIFICATION"
        {
            public short attribs;
            public int cb;
            public short date;
            public int fdie;
            public IntPtr hf;
            public short iCabinet;
            public short iFolder;
            public string psz1;
            public string psz2;
            public string psz3;
            public short setID;
            public short time;
            public IntPtr userData;
        }

        private enum FdiNotificationType
        {
            CabinetInfo,
            PartialFile,
            CopyFile,
            CloseFileInfo,
            NextCabinet,
            Enumerate
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr FdiNotifyDelegate(FdiNotificationType fdint, [In] [MarshalAs(UnmanagedType.LPStruct)] FdiNotification fdin);
    }
}