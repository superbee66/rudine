using System.Globalization;
using System.Resources;
using System.Threading;

namespace Rudine.Util.Xsds
{
    internal sealed class Res
    {
        internal const string Logo = "Logo";

        internal const string HelpDescription = "HelpDescription";

        internal const string HelpUsage = "HelpUsage";

        internal const string HelpOptions = "HelpOptions";

        internal const string HelpClasses = "HelpClasses";

        internal const string HelpDataset = "HelpDataset";

        internal const string HelpElement = "HelpElement";

        internal const string HelpFields = "HelpFields";

        internal const string HelpOrder = "HelpOrder";

        internal const string HelpEnableDataBinding = "HelpEnableDataBinding";

        internal const string HelpEnableLinqDataSet = "HelpEnableLinqDataSet";

        internal const string HelpLanguage = "HelpLanguage";

        internal const string HelpNamespace = "HelpNamespace";

        internal const string HelpNoLogo = "HelpNoLogo";

        internal const string HelpOut = "HelpOut";

        internal const string HelpType = "HelpType";

        internal const string HelpUri = "HelpUri";

        internal const string HelpAdvanced = "HelpAdvanced";

        internal const string HelpParameters = "HelpParameters";

        internal const string HelpArguments = "HelpArguments";

        internal const string HelpArgumentsDescription = "HelpArgumentsDescription";

        internal const string MoreHelp = "MoreHelp";

        internal const string Error = "Error";

        internal const string ErrInvalidArgument = "ErrInvalidArgument";

        internal const string ErrLanguage = "ErrLanguage";

        internal const string ErrCodeDomProvider = "ErrCodeDomProvider";

        internal const string ErrLoadAssembly = "ErrLoadAssembly";

        internal const string ErrUnknownNodeType = "ErrUnknownNodeType";

        internal const string ErrInputFileTypes = "ErrInputFileTypes";

        internal const string ErrClassOrDataset = "ErrClassOrDataset";

        internal const string ErrGeneral = "ErrGeneral";

        internal const string ErrGenerateDataSetClass = "ErrGenerateDataSetClass";

        internal const string ErrGenerateClassesForSchema = "ErrGenerateClassesForSchema";

        internal const string Warning = "Warning";

        internal const string UnhandledNode = "UnhandledNode";

        internal const string FileNotFound = "FileNotFound";

        internal const string SchemaValidationWarning = "SchemaValidationWarning";

        internal const string SchemaValidationWarningDetails = "SchemaValidationWarningDetails";

        internal const string SchemaValidationWarningDetailsSource = "SchemaValidationWarningDetailsSource";

        internal const string NoClassesGenerated = "NoClassesGenerated";

        internal const string NoTypesGenerated = "NoTypesGenerated";

        internal const string SchemaValidationError = "SchemaValidationError";

        internal const string XsdParametersValidationError = "XsdParametersValidationError";

        internal const string ErrorPosition = "ErrorPosition";

        internal const string MultipleFilesFoundMatchingInclude4 = "MultipleFilesFoundMatchingInclude4";

        internal const string InfoWrittingFile = "InfoWrittingFile";

        internal const string InfoVersionComment = "InfoVersionComment";

        private static Res loader;

        private readonly ResourceManager resources;

        private static CultureInfo Culture
        {
            get { return null; }
        }

        internal Res() { resources = new ResourceManager("XsdRes", GetType().Assembly); }

        private static Res GetLoader()
        {
            if (loader == null)
            {
                Res value = new Res();
                Interlocked.CompareExchange(ref loader, value, null);
            }
            return loader;
        }

        public static string GetString(string name, params object[] args)
        {
            return name;

            Res res = GetLoader();
            if (res == null)
                return null;
            string @string = res.resources.GetString(name, Culture);
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string text = args[i] as string;
                    if (text != null && text.Length > 1024)
                        args[i] = text.Substring(0, 1021) + "...";
                }
                return string.Format(CultureInfo.CurrentCulture, @string, args);
            }
            return @string;
        }

        public static string GetString(string name)
        {
            Res res = GetLoader();
            if (res == null)
                return null;
            return name; //res.resources.GetString(name, Res.Culture);
        }
    }
}