using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

//what an organiser sees about each result for their event
public class EventResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly FinishTime { get; set; }
    public int FinishingPosition { get; set; }

    //builds the response from a result, the enrolment with its user and category have to be loaded first
    public static EventResultResponse FromResult(Result result)
    {
        return new EventResultResponse
        {
            ResultID = result.ResultID,
            EnrolmentID = result.EnrolmentID,
            ParticipantID = result.Enrolment.UserID,
            ParticipantName = $"{result.Enrolment.User.FirstName} {result.Enrolment.User.LastName}",
            CategoryName = result.Enrolment.Category.CategoryName,
            FinishTime = result.FinishTime,
            FinishingPosition = result.FinishingPosition
        };
    }
}