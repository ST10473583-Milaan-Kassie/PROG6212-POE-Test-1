using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

//what a participant sees about one of their enrolments
//includes the event and category details so the front end doesnt have to look them up
public class EnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    //pending or confirmed
    public string Status { get; set; } = string.Empty;

    //lets the front end know if withdrawing is still allowed
    public bool HasTakenPlace { get; set; }

    //builds the response from an enrolment, the event and category have to be loaded first
    public static EnrolmentResponse FromEnrolment(Enrolment enrolment, bool hasTakenPlace)
    {
        return new EnrolmentResponse
        {
            EnrolmentID = enrolment.EnrolmentID,
            EventID = enrolment.EventID,
            EventName = enrolment.Event.EventName,
            EventDate = enrolment.Event.EventDate,
            Location = enrolment.Event.Location,
            CategoryID = enrolment.CategoryID,
            CategoryName = enrolment.Category.CategoryName,
            Status = enrolment.Status,
            HasTakenPlace = hasTakenPlace
        };
    }
}