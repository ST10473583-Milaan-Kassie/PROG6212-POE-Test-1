namespace RaceDay.Api.Models;

//categories belong to one event
public class Category
{
    //PK
    public int CategoryID { get; set; }

    //FK for the events table, which event this category belongs to
    public int EventID { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    //a category has an age range, a distance, or both, so all three are nullable
    //null min age means the category is open to every age
    public int? MinAge { get; set; }

    //nullable as there may be no max age, needs a min age to go with it
    public int? MaxAge { get; set; }

    //in km, null means its an age only category
    public decimal? Distance { get; set; }

    //the event this category belongs to
    public Event Event { get; set; } = null!;

    //all the enrolments for this category
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}