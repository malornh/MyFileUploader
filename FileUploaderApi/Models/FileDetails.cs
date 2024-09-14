using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUploaderApi.Models
{
    public class FileDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }

        [Required(ErrorMessage = "Atleast one file is required")]
        public byte[] Data { get; set; }
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
