using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoAPI.Model;
using ToDoApp.Context;

namespace ToDoAPI.API;

[Route("api/[controller]")]
[ApiController]
public class TodoNoteController : ControllerBase
{
    private readonly ToDoContext _todoContext;
    private readonly TodoListController _todoListController;

    public TodoNoteController(ToDoContext todoContext, TodoListController todoListController)
    {
        _todoContext = todoContext;
        _todoListController = todoListController;
    }

    [NonAction]
    public async Task<TodoNote> FindNoteAsync(string id) =>
        await _todoContext.TodoNotes.FirstOrDefaultAsync(note => note.Id == id) 
        ?? throw new InvalidOperationException();
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var note = await FindNoteAsync(id);
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }

    [HttpPatch(template: "{id}/{content}")]
    public async Task<IActionResult> UpdateContext(string id, string content)
    {
        try
        {
            var note = await FindNoteAsync(id);
            note.Content = content;
            await _todoContext.SaveChangesAsync();
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
    
    [HttpPatch(template: "{id}/{endDate:datetime}")]
    public async Task<IActionResult> UpdateContext(string id, DateTime endDate)
    {
        try
        {
            var note = await FindNoteAsync(id);
            note.EndDate = endDate;
            await _todoContext.SaveChangesAsync();
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
    
    [HttpPatch(template: "{id}/{status}")]
    public async Task<IActionResult> UpdateContext(string id, TodoStatus status)
    {
        try
        {
            var note = await FindNoteAsync(id);
            note.Status = status;
            await _todoContext.SaveChangesAsync();
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
    
    
    [HttpPost("{listId}")]
    public async Task<IActionResult> Add(string listId, [FromQuery] string context, [FromQuery] DateTime endDate)
    {
        var builder = new TodoNote.Builder();
        if (context != string.Empty)
            builder.AddContent(context);
        
        if (endDate != default)
            builder.AddEndDate(endDate);

        var note = builder.Build();
        var list = await _todoListController.FindListAsync(listId);
        list.AddNote(note);
        await _todoContext.SaveChangesAsync();
        return Ok(note);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var note = await FindNoteAsync(id);
            _todoContext.TodoNotes.Remove(note);
            await _todoContext.SaveChangesAsync();
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
}