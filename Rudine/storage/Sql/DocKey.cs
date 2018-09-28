using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rudine.Web;

namespace Rudine.Storage.Sql
{
    /// <summary>
    ///     Summary description for DocumentKey
    /// </summary>
    [Table("DocKey")]
    public class DocKey : BaseAutoIdent
    {
        [Key]
        [Column(Order = 1)]
        public override int Id
        {
            get { return base.Id; }
            set { base.Id = value; }
        }

        [Key]
        [MaxLength(50)]
        [Column(Order = 2)]
        public string KeyName { get; set; }

        [MaxLength(50)]
        [Column(Order = 3)]
        [Required]
        public string KeyVal { get; set; }
    }
}