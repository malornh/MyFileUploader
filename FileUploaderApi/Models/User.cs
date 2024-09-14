using System.ComponentModel.DataAnnotations;

namespace FileUploaderApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[\w\.\-]+@[\w\-]+(\.[\w]{2,3})+$", ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
        public List<FileDetails>? Files { get; set; }
    }
}
