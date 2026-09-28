using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Регистрация БД
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Автоматическое применение миграций при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // создаёт БД, если её нет, и применяет миграции

    // Необязательно: seed-данные
    if (!db.Todos.Any())
    {
        db.Todos.AddRange(
            new TodoApp.Models.TodoItem { Title = "Изучить EF Core", Completed = false },
            new TodoApp.Models.TodoItem { Title = "Сделать ToDoList", Completed = true, CompletedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.MapControllers();

app.Run();