namespace TaskMaster.API.DTOs;

public record ProjectDto(
    int Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime DueDate,
    string Status,
    int TaskCount,
    int CompletedTaskCount,
    int ProgressPercentage,
    string CurrentUserRole,
    List<ProjectMemberDto> Members,
    bool HasUnreadMessages
);