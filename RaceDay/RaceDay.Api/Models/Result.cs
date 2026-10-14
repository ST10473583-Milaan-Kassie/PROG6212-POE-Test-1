namespace RaceDay.Api.Models;

//results, attached to an enrolment, are captured by the organiser once the event is done
public class Result
{
    //PK
    public int ResultID { get; set; }

    //FK for the enrolments table, which enrolment the result belongs to
    //unique so an enrolment cant have more than one result
    public int EnrolmentID { get; set; }

    //timeonly maps to TIME in sql
    public TimeOnly FinishTime { get; set; }

    public int FinishingPosition { get; set; }

    //the enrolment this result belongs to
    public Enrolment Enrolment { get; set; } = null!;
}