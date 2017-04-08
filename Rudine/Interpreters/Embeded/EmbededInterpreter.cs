using Rudine.Template.Embeded;
using Rudine.Util.Zips;
using Rudine.Web;

namespace Rudine.Interpreters.Embeded
{
    public class EmbededInterpreter : DocByteInterpreter
    {
        public override string ContentFileExtension =>
            "docrev";

        public override string ContentType =>
            "application/octet-stream";

        public override BaseDoc Create(string DocTypeName) =>
            Runtime.ActivateBaseDoc(
                EmbededTemplateController.MY_ONLY_DOC_NAME,
                EmbededTemplateController.MY_ONLY_DOC_VERSION.ToString());

        public override string GetDescription(string DocTypeName) =>
            null;

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) =>
            null;

        public override bool Processable(string DocTypeName, string DocRev) =>
            DocTypeName.Equals(EmbededTemplateController.MY_ONLY_DOC_NAME)
            &&
            DocRev.Equals(EmbededTemplateController.MY_ONLY_DOC_VERSION.ToString());

        public override BaseDoc Read(byte[] DocData, bool DocRevStrict = false) =>
            Compressor.Decompress<BaseDoc>(DocData);

        public override DocProcessingInstructions ReadDocPI(byte[] DocData) =>
            Read(DocData);

        public override string ReadDocTypeName(byte[] DocData) =>
            ReadDocPI(DocData).DocTypeName;

        public override string ReadDocRev(byte[] DocData) =>
            ReadDocPI(DocData).solutionVersion;

        public override void Validate(byte[] DocData) { }

        public override byte[] WriteByte<T>(T source, bool includeProcessingInformation = true) =>
            Compressor.Compress(source);

        public override byte[] WritePI(byte[] DocData, DocProcessingInstructions pi) =>
            WriteByte(SetPI(Read(DocData), pi));
    }
}