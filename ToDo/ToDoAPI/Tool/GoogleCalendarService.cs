using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Extensions.Configuration;

namespace ToDoAPI.Tool;

public class GoogleCalendarService
{
    private readonly CalendarService _calendarService;
    private readonly GoogleAuthorizationCodeFlow _googleAuthorizationCodeFlow;
    private IConfiguration _configuration;

    public GoogleCalendarService(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }


    public async Task<Event> CreateEventAsync(string id, string summary, DateTime start, DateTime end, string description = "", string location = "", IList<EventAttendee> attendees = null)
    {
        var newEvent = new Event
        {
            Id = id,
            Summary = summary,
            Description = description,
            Location = location,
            Start = new EventDateTime
            {
                DateTime = start,
                TimeZone = "UTC"
            },
            End = new EventDateTime
            {
                DateTime = end,
                TimeZone = "UTC"
            },
            Attendees = attendees
        };

        var createdEvent = await _calendarService.Events.Insert(newEvent, "primary").ExecuteAsync();
        return createdEvent;
    }


    public async Task<Event> UpdateEventAsync(string eventId, string summary, DateTime start, DateTime end, string description = "", string location = "", IList<EventAttendee> attendees = null)
    {
        var eventToUpdate = await _calendarService.Events.Get("primary", eventId).ExecuteAsync();

        eventToUpdate.Summary = summary;
        eventToUpdate.Description = description;
        eventToUpdate.Location = location;
        eventToUpdate.Start.DateTime = start;
        eventToUpdate.End.DateTime = end;
        eventToUpdate.Attendees = attendees;

        var updatedEvent = await _calendarService.Events.Update(eventToUpdate, "primary", eventId).ExecuteAsync();
        return updatedEvent;
    }


    public async Task DeleteEventAsync(string eventId)
    {
        await _calendarService.Events.Delete("primary", eventId).ExecuteAsync();
    }


    public async Task<Event> GetEventAsync(string eventId)
    {
        try
        {
            var eventDetails = await _calendarService.Events.Get("primary", eventId).ExecuteAsync();
            return eventDetails;
        }
        catch (Exception ex)
        {
            return await CreateEventAsync(eventId, "", DateTime.Now, DateTime.Now);
        }
    }


    public async Task<IList<Event>> ListUpcomingEventsAsync(int maxResults = 10)
    {
        var events = await _calendarService.Events.List("primary").ExecuteAsync();
        return events.Items.Take(maxResults).ToList();
    }

    public async Task<object> UpdateEventAsync(string eventId, Event existingEvent)
    {
        await _calendarService.Events.Update(existingEvent, "primary", eventId).ExecuteAsync();
        return existingEvent;
    }
}