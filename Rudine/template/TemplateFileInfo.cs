using Rudine.Web;

namespace Rudine.Template
{
    public class TemplateFileInfo : IDocURN
    {
        public string FileName { get; set; }
        public string DocTypeName { get; set; }
        public string solutionVersion { get; set; }
    }
}