namespace TaskMaster.API.DTOs
{
    public record NoteDto(int Id, string Content, string AuthorName, DateTime CreatedAt);
}
