namespace TaskMaster.API.DTOs
{
    public record ProjectDto(
        int Id,
        string Name,
        string? Description,
        DateTime CreatedAt,
        DateTime DueDate,
        string Status,
        int TaskCount
        );
}
