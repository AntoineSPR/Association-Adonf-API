using System.ComponentModel.DataAnnotations;

namespace AssociationAdonfAPI.Models
{
    public class UserCreateDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Name { get; set; }

        public UserApp ToUserApp()
        {
            return new UserApp
            {
                Email = Email,
                UserName = Email,
                Name = Name
            };
        }

        public UserApp ToSimpleUser(UserApp user)
        {
            user.Name = Name;
            return user;
        }
    }
}
