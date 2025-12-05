namespace TaskMaster.API.DTOs
{
    public record ChatMessageDto(
        int Id,
        string Content,
        int SenderId,
        string SenderName
        , DateTime CreatedAt);
}
