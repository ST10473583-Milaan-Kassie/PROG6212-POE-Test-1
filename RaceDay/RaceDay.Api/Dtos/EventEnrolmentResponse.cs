using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

//what an organiser sees about each enrolment for their event
//only the participants name goes out, their email, phone and dob arent needed here so theyre left out
public class EventEnrolmentResponse
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    //pending or confirmed
    public string Status { get; set; } = string.Empty;

    //once a result is captured the status is locked, lets the front end know
    public bool HasResult { get; set; }

    //builds the response from an enrolment, the user, category and result have to be loaded first
    public static EventEnrolmentResponse FromEnrolment(Enrolment enrolment)
    {
        return new EventEnrolmentResponse
        {
            EnrolmentID = enrolment.EnrolmentID,
            ParticipantID = enrolment.UserID,
            ParticipantName = $"{enrolment.User.FirstName} {enrolment.User.LastName}",
            CategoryID = enrolment.CategoryID,
            CategoryName = enrolment.Category.CategoryName,
            Status = enrolment.Status,
            HasResult = enrolment.Result != null
        };
    }
}