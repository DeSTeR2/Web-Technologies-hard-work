using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Mvc;
using ToDoAPI.Services;

namespace ToDoAPI.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly GoogleCalendarService _cal;

        public CalendarController(GoogleCalendarService cal) => _cal = cal;

        #region Get Upcoming Events

        [HttpGet("events")]
        public async Task<IActionResult> GetUpcoming([FromQuery] int max = 10)
        {
            try
            {
                var events = await _cal.ListUpcomingEventsAsync(max);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching events: {ex.Message}");
            }
        }

        #endregion

        #region Create Event

        [HttpPost("events")]
        public async Task<IActionResult> CreateAsync([FromBody] Event dto)
        {
            if (dto.Start?.DateTime == null || dto.End?.DateTime == null)
                return BadRequest("Start and End times are required.");

            try
            {
                var createdEvent = await _cal.CreateEventAsync(
                    dto.Id,
                    dto.Summary!,
                    dto.Start.DateTime.Value,
                    dto.End.DateTime.Value,
                    dto.Description,
                    dto.Location
                );

                return CreatedAtAction(nameof(GetUpcoming), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating event: {ex.Message}");
            }
        }

        #endregion

        #region Update Event

        [HttpPut("events/{eventId}")]
        public async Task<IActionResult> UpdateAsync(string eventId, [FromBody] Event dto)
        {
            if (dto.Start?.DateTime == null || dto.End?.DateTime == null)
                return BadRequest("Start and End times are required.");

            try
            {
                // Fetch the existing event to ensure it exists
                var existingEvent = await _cal.GetEventAsync(eventId);
                if (existingEvent == null)
                    return NotFound("Event not found.");

                existingEvent.Summary = dto.Summary ?? existingEvent.Summary;
                existingEvent.Description = dto.Description ?? existingEvent.Description;
                existingEvent.Start = dto.Start ?? existingEvent.Start;
                existingEvent.End = dto.End ?? existingEvent.End;
                existingEvent.Location = dto.Location ?? existingEvent.Location;
                existingEvent.Attendees = dto.Attendees ?? existingEvent.Attendees;

                // Update the event
                var updatedEvent = await _cal.UpdateEventAsync(eventId, existingEvent);
                return Ok(updatedEvent);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating event: {ex.Message}");
            }
        }

        #endregion

        #region Delete Event

        [HttpDelete("events/{eventId}")]
        public async Task<IActionResult> Delete(string eventId)
        {
            try
            {
                await _cal.DeleteEventAsync(eventId);
                return NoContent(); // Event deleted successfully
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting event: {ex.Message}");
            }
        }

        #endregion
    }
}
