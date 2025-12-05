namespace TaskMaster.API.DTOs
{
    public record CreateTaskDto(string Title, string Description, DateTime DueDate, string Priority, int? AssignedToUserId);
}
