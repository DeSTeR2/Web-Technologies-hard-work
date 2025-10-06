using Microsoft.VisualBasic;

namespace ToDoAPI.Model;

public class TodoList
{
    public string? Id { get; set; }
    public string? OwnerId { get; set; }
    public string? Name { get; set; }
    public IEnumerable<TodoNote>? Notes { get; set; }

    public TodoList(string ownerId, string name)
    {
        Id = Guid.NewGuid().ToString();
        OwnerId = ownerId;
        Name = name;

        AddNote(new TodoNote.Builder().Build());
    }

    public void AddNote(TodoNote note) => Notes?.Append(note);
}