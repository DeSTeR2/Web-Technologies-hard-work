using Microsoft.EntityFrameworkCore;
using ToDoAPI.Model;

namespace ToDoApp.Context;

public class ToDoContext : DbContext
{
    public DbSet<TodoList> ToDoLists { get; set; }
    public DbSet<TodoNote> TodoNotes { get; set; }

    public ToDoContext(DbContextOptions options) : base(options)
    {
        
    }
}