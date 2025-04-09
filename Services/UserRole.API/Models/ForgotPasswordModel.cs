using System.ComponentModel.DataAnnotations;

namespace UserRoleAPI.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
