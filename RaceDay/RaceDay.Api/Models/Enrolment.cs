namespace RaceDay.Api.Models;

//enrolments link a participant to an event and their chosen category
//this is the junction table for the whole entry process
public class Enrolment
{
    //PK
    public int EnrolmentID { get; set; }

    //FK for the users table, the participant enrolling
    public int UserID { get; set; }

    //FK for the events table, the event they're entering
    public int EventID { get; set; }

    //FK for the categories table, the category they picked
    //eventID and categoryID together point at the category, that gets set up in the dbcontext
    public int CategoryID { get; set; }

    //either pending or confirmed, pending by default
    public string Status { get; set; } = "Pending";

    //the participant who enrolled
    public User User { get; set; } = null!;

    //the event they enrolled in
    public Event Event { get; set; } = null!;

    //the category they picked
    public Category Category { get; set; } = null!;

    //null until the organiser captures a result after the event
    public Result? Result { get; set; }
}