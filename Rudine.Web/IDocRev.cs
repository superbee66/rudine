using System.Collections.Generic;

namespace Rudine.Web
{
    public interface IDocRev 
    {
        /// <summary>
        /// If the SchemaXSD's DocRevEntry in the FileList exists; it will be excluded this hash
        /// </summary>
        string DocFilesMD5 { get; }
        /// <summary>
        /// BaseDoc's created from this DocRev will be named with the following information
        /// </summary>
        DocURN DocURN { get; set; }
        List<DocRevEntry> DocFiles { get; set; }
        /// <summary>
        /// string literal that is valid XSD  used to compose an IDocModel & finally a BaseDoc from
        /// </summary>
        string DocSchema { get; set; }
    }
}