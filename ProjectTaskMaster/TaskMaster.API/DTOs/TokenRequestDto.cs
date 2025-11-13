using System.ComponentModel.DataAnnotations;

namespace TaskMaster.API.DTOs
{
    public record TokenRequestDto(
        [Required] string AccessToken,
        [Required] string RefreshToken
    );
}
