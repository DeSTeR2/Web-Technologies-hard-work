using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoAPI.Model;
using ToDoApp.Context;

namespace ToDoAPI.API;

[ApiController]
[Route("api/[controller]")]
public class TodoListController : ControllerBase
{
    private readonly ToDoContext _toDoContext;

    public TodoListController(ToDoContext toDoContext) => _toDoContext = toDoContext;

    [NonAction]
    public async Task<TodoList> FindListAsync(string id) => 
        await _toDoContext.ToDoLists.FirstOrDefaultAsync(l => l.Id == id) 
        ?? throw new InvalidOperationException();

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(_toDoContext.ToDoLists);
    }

    [HttpGet("{id}/todos")]
    public async Task<IActionResult> GetTodos(string id)
    {
        try
        {
            var list = await FindListAsync(id);
            return Ok(list.Notes ?? new List<TodoNote>());
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }
    
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

    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] string name)
    {
        try
        {
            var list = new TodoList(Guid.NewGuid().ToString(), name);
            _toDoContext.ToDoLists.Add(list);
            await _toDoContext.SaveChangesAsync();
            return Ok(list);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var list = await FindListAsync(id);
            _toDoContext.ToDoLists.Remove(list);
            await _toDoContext.SaveChangesAsync();
            return Ok(list);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }                           
    }
}