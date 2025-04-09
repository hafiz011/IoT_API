
namespace UserRoleAPI.Models
{
    public class UpdateAccountModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public Address Address { get; set; }
        public IFormFile ImagePath { get; set; }
    }
}
