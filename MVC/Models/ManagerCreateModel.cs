using System.ComponentModel.DataAnnotations;

namespace MVC.Models
{
    public class ManagerCreateModel
    {
        [Required]
        public Guid Id { get; set; }

        //[Required]
        public bool MainManager { get; set; } = false;
    }
}
