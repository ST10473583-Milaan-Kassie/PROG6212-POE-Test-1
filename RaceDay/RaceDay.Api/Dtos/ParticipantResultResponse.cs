using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

//what a participant sees about one of their own results
//total finishers lets the front end show e.g. 47th out of 312 finishers
public class ParticipantResultResponse
{
    public int ResultID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly FinishTime { get; set; }
    public int FinishingPosition { get; set; }

    //how many results have been captured for the event
    public int TotalFinishers { get; set; }

    //builds the response from a result, the enrolment with its event and category have to be loaded first
    public static ParticipantResultResponse FromResult(Result result, int totalFinishers)
    {
        return new ParticipantResultResponse
        {
            ResultID = result.ResultID,
            EventID = result.Enrolment.EventID,
            EventName = result.Enrolment.Event.EventName,
            EventDate = result.Enrolment.Event.EventDate,
            CategoryName = result.Enrolment.Category.CategoryName,
            FinishTime = result.FinishTime,
            FinishingPosition = result.FinishingPosition,
            TotalFinishers = totalFinishers
        };
    }
}