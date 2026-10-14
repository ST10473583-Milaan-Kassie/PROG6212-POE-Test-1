namespace RaceDay.Api.Models;

//lookup table for the event type (run/walk/cycle)
public class EventType
{
    //PK
    public int EventTypeID { get; set; }

    //called EventTypeName bcs a property cant have the same name as its class
    public string EventTypeName { get; set; } = string.Empty;

    //all the events of this type
    public ICollection<Event> Events { get; set; } = new List<Event>();
}