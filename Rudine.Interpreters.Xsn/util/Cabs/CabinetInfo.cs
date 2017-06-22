using System.Runtime.InteropServices;

namespace Rudine.Interpreters.Xsn.util.Cabs
{
    [StructLayout(LayoutKind.Sequential)]
    internal class CabinetInfo //Cabinet API: "FDCABINETINFO"
    {
        public int cbCabinet;
        public short cFiles;
        public short cFolders;
        public int fReserve;
        public int hasnext;
        public int hasprev;
        public short iCabinet;
        public short setID;
    }
}