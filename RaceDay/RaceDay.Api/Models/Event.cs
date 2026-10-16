namespace RaceDay.Api.Models;

//each event is created by one organiser
public class Event
{
    //PK
    public int EventID { get; set; }

    //required
    public string EventName { get; set; } = string.Empty;

    //FK for the EventType table, (run/walk/cycle)
    public int EventTypeID { get; set; }

    //FK for the Users table, the organiser who owns this event
    public int UserID { get; set; }

    //optional so its nullable
    public string? Description { get; set; }

    //dateonly maps to DATE in SQL
    public DateOnly EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    //in km, decimal keeps it exact, the (6,2) size gets set in the dbcontext
    public decimal Distance { get; set; }

    //null until the organiser uploads one
    public string? BannerImageURL { get; set; }

    //false until the organiser publishes the results, participants only see results once its true
    public bool ResultsPublished { get; set; }

    //the type this event is, EF fills this in
    public EventType EventType { get; set; } = null!;

    //the organiser who created the event
    public User User { get; set; } = null!;

    //all the categories that belong to this event
    public ICollection<Category> Categories { get; set; } = new List<Category>();

    //all the enrolments for this event
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}