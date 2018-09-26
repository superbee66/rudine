using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rudine.Storage.Sql
{
    /// <summary>
    ///     Summary description for DocumentKey
    /// </summary>
    [Table("DocKey")]
    public class DocKey
    {
        [Key]
        [Column(Order = 1)]
        public int Id { get; set; }

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