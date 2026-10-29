using System.ComponentModel.DataAnnotations;

namespace api.Dtos.UserDtos
{
    public class RegisterUserDto
    {   
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Lösenorden matchar inte.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
