namespace RaceDay.Api.DTOs.Responses;

//one event type (run/walk/cycle), used by the front end for its dropdowns and filters
public class EventTypeResponse
{
    public int EventTypeID { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
}