using System.ComponentModel.DataAnnotations;

namespace TaskMaster.API.DTOs
{
    public record RegisterDto(
        [Required] string Name,
        [Required][EmailAddress] string Email,
        [Required] string password
        );
}
