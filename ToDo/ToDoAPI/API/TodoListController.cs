using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ToDoAPI.Model;
using ToDoAPI.Settings;
using ToDoAPI.Tool;

namespace ToDoAPI.API;

[ApiController]
[Route("api/[controller]")]
public class TodoListController : ControllerBase
{
    private readonly IHubContext<TodoSyncHub> _hub;
    private readonly MongoDbService _mongo;
    private readonly UserDataService _userService;

    public TodoListController(MongoDbService mongo, UserDataService userService, IHubContext<TodoSyncHub> hub)
    {
        _mongo = mongo;
        _userService = userService;
        _hub = hub;
    }

    [NonAction]
    public async Task<IActionResult> FindListAsyncResult(string id)
    {
        try
        {
            var list = await _mongo.FindListAsync(id);
            return Ok(list);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lists = await _mongo.GetAllListsAsync();
        return Ok(lists);
    }

    [HttpGet("ByActiveUser")]
    public async Task<IActionResult> GetByUser()
    {
        var lists = await _mongo.GetAllListsByUserIdAsync(_userService.GetCurrentUserId());
        return Ok(lists);
    }

    [HttpGet("{id}/todos")]
    public async Task<IActionResult> GetTodos(string id)
    {
        try
        {
            var noteIds = await _mongo.GetNoteIdsForListAsync(id);
            return Ok(noteIds);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        try
        {
            var list = await _mongo.FindListAsync(id);
            return Ok(list);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetByUserId(string id)
    {
        try
        {
            var filterLists = await _mongo.GetAllListsAsync();
            var userLists = filterLists.FindAll(l => l.OwnerId == id);
            return Ok(userLists);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPatch("note/{id}")]
    public async Task<IActionResult> AddNote(string id, [FromBody] string noteId)
    {
        try
        {
            var updated = await _mongo.AddNoteToListAsync(id, noteId);


            var note = await _mongo.FindNoteAsync(noteId);
            await Broadcast(id, "NOTE_CREATED", new
            {
                id = note.Id,
                title = note.Name,
                content = note.Content,
                status = note.Status,
                endDate = note.EndDate
            });


            return Ok(updated);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("note/{id}")]
    public async Task<IActionResult> DeleteNote(string id, [FromBody] string noteId)
    {
        try
        {
            var note = await _mongo.FindNoteAsync(noteId);
            var oldStatus = note?.Status ?? TodoStatus.Waiting;

            var updated = await _mongo.RemoveNoteFromListAsync(id, noteId);


            await Broadcast(id, "NOTE_DELETED", new { id = noteId, status = (int)oldStatus });


            return Ok(updated);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] string name)
    {
        try
        {
            var ownerId = _userService.GetCurrentUserId();
            var list = await _mongo.CreateListAsync(ownerId ?? Guid.NewGuid().ToString(), name);
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
            var list = await _mongo.DeleteListAsync(id);
            return Ok(list);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    private Task Broadcast(string listId, string type, object payload)
    {
        return _hub.Clients.Group($"list:{listId}")
            .SendAsync("message", new { type, payload });
    }
}