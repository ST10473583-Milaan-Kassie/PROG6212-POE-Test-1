namespace RaceDay.Api.Helpers;

//the two enrolment statuses exactly as CK_Enrolments_Status allows them
//kept in one place so the spelling always matches the database
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
}