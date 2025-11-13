using System.ComponentModel.DataAnnotations;

namespace TaskMaster.API.DTOs
{
    public record LoginDto(
        [Required][EmailAddress] string Email,
        [Required] string Password
        );
}
