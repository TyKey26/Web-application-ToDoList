using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.DTOs;
using TodoApp.Models;

namespace TodoApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TodosController> _logger;

    public TodosController(AppDbContext db, ILogger<TodosController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static TodoResponse ToDto(TodoItem t) =>
        new(t.Id, t.Title, t.Completed, t.CreatedAt, t.CompletedAt);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetAll(
        [FromQuery] string? filter = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Todos.AsNoTracking().AsQueryable();

        if (filter == "active")
            query = query.Where(t => !t.Completed);
        else if (filter == "completed")
            query = query.Where(t => t.Completed);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => EF.Functions.Like(t.Title, $"%{search}%"));

        var items = await query
            .OrderBy(t => t.Completed)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => ToDto(t))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoResponse>> GetById(int id)
    {
        var item = await _db.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<TodoResponse>> Create([FromBody] CreateTodoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { error = "Title is required" });

        var item = new TodoItem
        {
            Title = dto.Title.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Todos.Add(item);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Создана задача {Id}: {Title}", item.Id, item.Title);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TodoResponse>> Update(int id, [FromBody] UpdateTodoDto dto)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return NotFound();

        if (dto.Title is not null)
        {
            var title = dto.Title.Trim();
            if (title.Length == 0)
                return BadRequest(new { error = "Title cannot be empty" });
            item.Title = title;
        }

        if (dto.Completed is bool completed)
        {
            item.Completed = completed;
            item.CompletedAt = completed ? DateTime.UtcNow : null;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item is null) return NotFound();

        _db.Todos.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("completed")]
    public async Task<IActionResult> ClearCompleted()
    {
        var completed = await _db.Todos.Where(t => t.Completed).ToListAsync();
        _db.Todos.RemoveRange(completed);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = completed.Count });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var total = await _db.Todos.CountAsync();
        var done = await _db.Todos.CountAsync(t => t.Completed);
        return Ok(new
        {
            total,
            completed = done,
            active = total - done
        });
    }
}