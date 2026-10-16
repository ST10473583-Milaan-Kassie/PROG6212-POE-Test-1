using System.Text.Json.Serialization;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

//what gets sent back about an event
//includes the type name and organiser name so the front end doesnt have to look them up
public class EventResponse
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int EventTypeID { get; set; }
    public string EventTypeName { get; set; } = string.Empty;

    //the organiser who created the event, only their name gets sent, none of their other details
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string? BannerImageURL { get; set; }

    //worked out with the south african clock, race day counts as taken place
    public bool HasTakenPlace { get; set; }

    //lets the front end know if the organiser can still publish the results
    public bool ResultsPublished { get; set; }

    //only filled in for GET /api/events/mine, left out of the JSON everywhere else
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalEnrolments { get; set; }

    //also only for GET /api/events/mine, just the confirmed ones out of the total
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ConfirmedEnrolments { get; set; }

    //builds the response from an event, the eventtype and user have to be loaded first
    public static EventResponse FromEvent(Event ev, bool hasTakenPlace)
    {
        return new EventResponse
        {
            EventID = ev.EventID,
            EventName = ev.EventName,
            EventTypeID = ev.EventTypeID,
            EventTypeName = ev.EventType.EventTypeName,
            OrganiserID = ev.UserID,
            OrganiserName = $"{ev.User.FirstName} {ev.User.LastName}",
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            BannerImageURL = ev.BannerImageURL,
            HasTakenPlace = hasTakenPlace,
            ResultsPublished = ev.ResultsPublished
        };
    }
}