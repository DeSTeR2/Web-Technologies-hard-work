using Microsoft.AspNetCore.Mvc;
using ToDoAPI.Model;
using ToDoApp.Services;

namespace ToDoAPI.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoNoteController : ControllerBase
    {
        private readonly MongoDbService _mongo;

        public TodoNoteController(MongoDbService mongo) => _mongo = mongo;

        [NonAction]
        public async Task<TodoNote> FindNoteAsync(string id) => await _mongo.FindNoteAsync(id);

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
                return NotFound(e.Message);
            }
        }

        [HttpPatch("{id}/{content}")]
        public async Task<IActionResult> UpdateContext(string id, string content)
        {
            try
            {
                var updated = await _mongo.PatchNoteContentAsync(id, content);
                return Ok(updated);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPatch("{id}/enddate")]
        public async Task<IActionResult> UpdateContextWithEndDate(string id, [FromQuery] DateTime endDate)
        {
            try
            {
                var updated = await _mongo.PatchNoteEndDateAsync(id, endDate);
                return Ok(updated);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateContextWithStatus(string id, [FromQuery] TodoStatus status)
        {
            try
            {
                var updated = await _mongo.PatchNoteStatusAsync(id, status);
                return Ok(updated);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost("{listId}")]
        public async Task<IActionResult> Add(string listId, [FromQuery] string name, [FromQuery] string context, [FromQuery] TodoStatus status, [FromQuery] DateTime endDate)
        {
            try
            {
                var builder = new TodoNote.Builder();
                if (!string.IsNullOrWhiteSpace(name))
                    builder.AddName(name);

                if (!string.IsNullOrWhiteSpace(context))
                    builder.AddContent(context);

                if (endDate != default)
                    builder.AddEndDate(endDate);

                builder.AddStatus(status);

                var note = builder.Build();
                await _mongo.CreateNoteAsync(note);

                // add note id to list (silently ignore failure to add to list? we bubble exception)
                await _mongo.AddNoteToListAsync(listId, note.Id!);

                return Ok(note.Id);
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
                var deleted = await _mongo.DeleteNoteAsync(id);
                return Ok(deleted);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
        }
    }
}
