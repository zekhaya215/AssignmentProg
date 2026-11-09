using System.ComponentModel.DataAnnotations;

namespace AssignmentProg.Models
{
    public class Assignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime DueDate { get; set; }
    }

}