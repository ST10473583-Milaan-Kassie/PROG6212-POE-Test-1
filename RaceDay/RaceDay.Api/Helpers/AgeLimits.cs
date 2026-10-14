namespace RaceDay.Api.Helpers;

//the age limits checked at registration, kept in one place so theyre easy to find and change
public static class AgeLimits
{
    //matches the youngest category in the seed data (junior, 10 to 17)
    public const int MinimumParticipantAge = 10;

    //organisers run events and handle other peoples details so they have to be adults
    public const int MinimumOrganiserAge = 18;

    //everyone has to be younger than this, catches typos like 1026 instead of 1996
    public const int MaximumAge = 100;
}