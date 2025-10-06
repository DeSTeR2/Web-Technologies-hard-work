using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoAPI.Model;
using ToDoApp.Context;

namespace ToDoAPI.API;

[Route("api/[controller]")]
[ApiController]
public class TodoNoteController(ToDoContext todoContext, TodoListController todoListController) : ControllerBase
{
    [NonAction]
    public async Task<TodoNote> FindNoteAsync(string id) =>
        await todoContext.TodoNotes.FirstOrDefaultAsync(note => note.Id == id) 
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
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
    
    
    [HttpPost("leaderboard/{leaderboardId}")]
    public async Task<IActionResult> Add(string leaderboardId, [FromQuery] string context, [FromQuery] DateTime endDate)
    {
        var builder = new TodoNote.Builder();
        if (context != string.Empty)
            builder.AddContent(context);
        
        if (endDate != default)
            builder.AddEndDate(endDate);

        var note = builder.Build();
        var list = await todoListController.FindListAsync(leaderboardId);
        list.AddNote(note);
        return Ok(note);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var note = await FindNoteAsync(id);
            todoContext.TodoNotes.Remove(note);
            return Ok(note);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }
}