using System.ComponentModel.DataAnnotations;
using MVC.Models.DTO;

namespace MVC.Models
{
    public class ManagerModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public bool MainManager { get; set; } = false;
    }
}
