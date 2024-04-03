using System.ComponentModel.DataAnnotations;

namespace MVC.Models.DTO
{
    public class UserDto
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MinLength(1)]
        public string Name { get; set; }

        [Required]
        [MinLength(1)]
        [EmailAddress]
        public string Email { get; set; }

        public DateTime? Birthdate { get; set; }

        public string? Gender { get; set; }

        public string? Citizenship { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

    }
}