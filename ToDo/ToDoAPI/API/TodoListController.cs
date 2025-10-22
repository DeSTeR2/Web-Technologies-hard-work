using Microsoft.AspNetCore.Mvc;
using ToDoAPI.Settings;
using ToDoAPI.Tool;

namespace ToDoAPI.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodoListController : ControllerBase
    {
        private readonly MongoDbService _mongo;
        private readonly UserDataService _userService;

        public TodoListController(MongoDbService mongo, UserDataService userService)
        {
            _mongo = mongo;
            _userService = userService;
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
                var updated = await _mongo.RemoveNoteFromListAsync(id, noteId);
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
    }
}
