namespace TaskMaster.API.DTOs
{
    public record UpdateProjectDto(
        string Name, 
        string? Description, 
        DateTime DueDate, 
        string Status
        );
}
