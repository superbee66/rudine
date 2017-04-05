using Rudine.Web;

namespace Rudine
{
    public class ImporterLightDoc
    {
        public string DocSrc
        {
            get { return LightDoc.DocSrc; }
        }

        public string DocTitleLi
        {
            get
            {
                return string.Format(
                    "{0} {1} \"{2}\" {3}",
                    string.IsNullOrWhiteSpace(ExceptionMessage)
                        ? "Success, "
                        : "Fail, ",
                    LightDoc.DocSubmitDate,
                    LightDoc.DocTitle,
                    ExceptionMessage);
            }
        }

        public string ExceptionMessage { get; set; }

        public string ImportDocSrc { get; set; }

        public LightDoc LightDoc { get; set; }
    }
}