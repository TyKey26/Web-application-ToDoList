namespace TodoApp.DTOs;

public record TodoResponse(
    int Id,
    string Title,
    bool Completed,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record CreateTodoDto(string Title);

public record UpdateTodoDto(string? Title, bool? Completed);