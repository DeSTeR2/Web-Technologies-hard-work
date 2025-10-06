using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoAPI.Model;
using ToDoApp.Context;

namespace ToDoAPI.API;

[ApiController]
[Route("api/[controller]")]
public class TodoListController(ToDoContext todoContext) : ControllerBase
{
    [NonAction]
    public async Task<TodoList> FindListAsync(string id) => 
        await todoContext.ToDoLists.FirstOrDefaultAsync(l => l.Id == id) 
        ?? throw new InvalidOperationException();

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var list = await FindListAsync(id);
            return Ok(list);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetByUserId(string id)
    {
        try
        {
            var list = await FindListAsync(id);
            return Ok(list);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }
    }

    [HttpPost("user/{id}")]
    public IActionResult Add(string id, string name = "")
    {
        try
        {
            var list = new TodoList(id, name);
            todoContext.ToDoLists.Add(list);
            return Ok(list);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var list = await FindListAsync(id);
            todoContext.ToDoLists.Remove(list);
            return Ok(list);
        }
        catch (Exception e)
        {
            return NotFound(e);
        }                           
    }
}