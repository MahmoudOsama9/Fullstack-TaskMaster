namespace TaskMaster.API.DTOs
{
    public record TaskDto(
    int Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    int? AssignedUserId,
    string? AssignedUserName
);

}
