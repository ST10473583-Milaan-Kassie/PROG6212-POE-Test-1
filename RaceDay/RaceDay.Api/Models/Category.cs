namespace RaceDay.Api.Models;

//categories belong to one event
public class Category
{
    //PK
    public int CategoryID { get; set; }

    //FK for the events table, which event this category belongs to
    public int EventID { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    //not null bcs there is always a min age
    public int MinAge { get; set; }

    //nullable as there may be no max age
    public int? MaxAge { get; set; }

    //the event this category belongs to
    public Event Event { get; set; } = null!;

    //all the enrolments for this category
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}