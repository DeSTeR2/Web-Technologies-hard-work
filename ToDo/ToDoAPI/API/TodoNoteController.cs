using Amazon.S3;
using Amazon.S3.Model;
using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ToDoAPI.Model;
using ToDoAPI.Settings;
using ToDoAPI.Tool;

namespace ToDoAPI.API;

[Route("api/[controller]")]
[ApiController]
public class TodoNoteController : ControllerBase
{
    private readonly CalendarController _cal;
    private readonly IHubContext<TodoSyncHub> _hub;
    private readonly MongoDbService _mongo;

    public TodoNoteController(MongoDbService mongo, CalendarController cal, IHubContext<TodoSyncHub> hub)
    {
        _cal = cal;
        _mongo = mongo;
        _hub = hub;
    }

    [NonAction]
    public async Task<TodoNote> FindNoteAsync(string id)
    {
        return await _mongo.FindNoteAsync(id);
    }

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

    [HttpPost("{listId}")]
    public async Task<IActionResult> Add(string listId,
        [FromQuery] string name,
        [FromQuery] string context,
        [FromQuery] TodoStatus status = TodoStatus.Waiting,
        [FromQuery] DateTime endDate = default)
    {
        try
        {
            var builder = new TodoNote.Builder();
            if (!string.IsNullOrWhiteSpace(name)) builder.AddName(name);
            if (!string.IsNullOrWhiteSpace(context)) builder.AddContent(context);
            if (endDate != default) builder.AddEndDate(endDate);
            builder.AddStatus(status);

            var note = builder.Build();
            await _mongo.CreateNoteAsync(note);
            await _mongo.AddNoteToListAsync(listId, note.Id!);


            await _hub.Clients.Group($"list:{listId}")
                .SendAsync("message", new
                {
                    type = "NOTE_CREATED",
                    payload = new
                    {
                        id = note.Id,
                        title = note.Name,
                        content = note.Content,
                        status = note.Status,
                        endDate = note.EndDate
                    }
                });


            return Ok(note.Id);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{listId}/notes")]
    public async Task<IActionResult> CreateAndAttachNote(string listId, [FromBody] CreateNoteDto dto)
    {
        if (dto == null) return BadRequest("Request body is required.");

        if (!string.IsNullOrWhiteSpace(dto.ClientId))
            try
            {
                var existing = await _mongo.FindNoteAsync(dto.ClientId);
                try
                {
                    await _mongo.AddNoteToListAsync(listId, existing.Id!);
                }
                catch
                {
                }

                return Ok(new { id = existing.Id });
            }
            catch
            {
            }

        var builder = new TodoNote.Builder();
        if (!string.IsNullOrWhiteSpace(dto.Name)) builder.AddName(dto.Name);
        if (!string.IsNullOrWhiteSpace(dto.Content)) builder.AddContent(dto.Content);
        if (dto.EndDate.HasValue) builder.AddEndDate(dto.EndDate.Value);
        builder.AddStatus(dto.Status);

        var note = builder.Build();

        if (!string.IsNullOrWhiteSpace(dto.ClientId)) note.Id = dto.ClientId;

        try
        {
            await _mongo.CreateNoteAsync(note);
        }
        catch (InvalidOperationException ex)
        {
            try
            {
                var existing = await _mongo.FindNoteAsync(note.Id!);
                await _mongo.AddNoteToListAsync(listId, existing.Id!);
                return Ok(new { id = existing.Id });
            }
            catch
            {
                return StatusCode(500, "Failed to create or recover existing note: " + ex.Message);
            }
        }

        await _mongo.AddNoteToListAsync(listId, note.Id!);


        await _hub.Clients.Group($"list:{listId}")
            .SendAsync("message", new
            {
                type = "NOTE_CREATED",
                payload = new
                {
                    id = note.Id,
                    title = note.Name,
                    content = note.Content,
                    status = note.Status,
                    endDate = note.EndDate
                }
            });


        return CreatedAtAction(nameof(Get), new { id = note.Id }, new { id = note.Id });
    }

    [HttpPatch("{id}/content")]
    public async Task<IActionResult> PatchContent(string id, [FromBody] PatchContentDto dto)
    {
        if (dto == null) return BadRequest("Body required");
        if (dto.Content == null) return BadRequest("content required");

        try
        {
            var updated = await _mongo.PatchNoteContentAsync(id, dto.Content);


            await BroadcastNote(id, "NOTE_UPDATED", new { id, content = dto.Content });


            return Ok(updated);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPatch("{id}/title")]
    public async Task<IActionResult> UpdateContextWithTitle(string id, [FromQuery] string title)
    {
        try
        {
            var updated = await _mongo.PatchNoteTitleAsync(id, title);


            await BroadcastNote(id, "NOTE_UPDATED", new { id, title });


            return Ok(updated);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPatch("{id}/date")]
    public async Task<IActionResult> UpdateContextWithDate(string id, [FromQuery] DateTime endDate, [FromQuery] DateTime startDate)
    {
        try
        {
            if (endDate == default || startDate == default)
                return BadRequest("Dates required");

            var updated = await _mongo.PatchNoteEndDateAsync(id, endDate);
            updated = await _mongo.PatchNoteStartDateAsync(id, startDate);

            await BroadcastNote(id, "NOTE_UPDATED", new { id, endDate, startDate });

            var meetingEvent = new Event
            {
                Id = id,
                Summary = updated.Name,
                Description = updated.Content,
                Start = new EventDateTime
                {
                    DateTime = startDate,
                    TimeZone = "UTC" // Adjust the time zone accordingly
                },
                End = new EventDateTime
                {
                    DateTime = endDate,
                    TimeZone = "UTC" // Adjust the time zone accordingly
                },
                Reminders = new Event.RemindersData
                {
                    UseDefault = false, // Set custom reminders
                    Overrides = new List<EventReminder>
                    {
                        new() { Method = "popup", Minutes = 10 }, // Reminder 10 minutes before the meeting
                        new() { Method = "email", Minutes = 30 } // Email reminder 30 minutes before the meeting
                    }
                }
            };
            var googleEvent = await _cal.UpdateAsync(id, meetingEvent);

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
            var note = await _mongo.FindNoteAsync(id);
            var oldStatus = note.Status;
            var updated = await _mongo.PatchNoteStatusAsync(id, status);
            await BroadcastNote(id, "NOTE_MOVED", new
            {
                id,
                fromStatus = (int)oldStatus!,
                toStatus = (int)status
            });
            return Ok(updated);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
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



    [HttpPost("{id}/image")]
    public async Task<IActionResult> UploadImages(string id, [FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest("No image files provided.");

        try
        {
            var note = await _mongo.FindNoteAsync(id);

            note.ImageUrls?.Add(filePath);

            if (note.ImageUrls != null)
                await _mongo.UpdateNoteImagesAsync(id, note.ImageUrls);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error uploading images: " + ex.Message);
        }
    }


    [HttpGet("{id}/images")]
    public async Task<IActionResult> GetNoteImages(string id)
    {
        try
        {
            var note = await _mongo.FindNoteAsync(id);
            if (note == null)
                return NotFound($"Note with ID '{id}' not found.");

            return Ok(note.ImageUrls ?? new List<string>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error fetching images: " + ex.Message);
        }
    }

    [HttpDelete("{id}/images")]
    public async Task<IActionResult> DeleteNoteImage(string id, [FromQuery] string imageUrl, [FromQuery] string bucketName = "your-default-bucket-name")
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("imageUrl is required.");

        try
        {
            var note = await _mongo.FindNoteAsync(id);
            if (note == null)
                return NotFound($"Note with ID '{id}' not found.");

            using var scope = HttpContext.RequestServices.CreateScope();
            var s3 = scope.ServiceProvider.GetRequiredService<IAmazonS3>();

            var uri = new Uri(imageUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            await s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            });

            note.ImageUrls?.Remove(imageUrl);
            await _mongo.UpdateNoteImagesAsync(id, note.ImageUrls ?? new List<string>());

            return Ok($"Deleted image '{key}' for note '{id}'.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error deleting image: " + ex.Message);
        }
    }

    private async Task<string?> GetListIdForNote(string noteId)
    {
        var lists = await _mongo.GetAllListsAsync();
        return lists.FirstOrDefault(l => l.NoteIds?.Contains(noteId) == true)?.Id;
    }

    private async Task BroadcastNote(string noteId, string type, object payload)
    {
        var listId = await GetListIdForNote(noteId);
        if (listId != null)
            await _hub.Clients.Group($"list:{listId}")
                .SendAsync("message", new { type, payload });
    }

    public class CreateNoteDto
    {
        public string? ClientId { get; set; }
        public string? Name { get; set; }
        public string? Content { get; set; }
        public TodoStatus Status { get; set; } = TodoStatus.Waiting;
        public DateTime? EndDate { get; set; }
    }

    public class PatchContentDto
    {
        public string? Content { get; set; }
    }
}