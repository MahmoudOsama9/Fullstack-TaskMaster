using System.ComponentModel.DataAnnotations;

namespace TaskMaster.API.DTOs
{
    public record CreateProjectDto(
        [Required]
        [MaxLength(100)]
        string Name,
        
        [MaxLength(500)]
        string? Description,
        
        [Required]
        DateTime DueDate
        );

}
