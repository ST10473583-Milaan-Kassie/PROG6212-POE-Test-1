namespace RaceDay.Api.Tests.Helpers;

//the IDs of the seed data rows in SeedData.cs, so the tests read as names instead of random numbers
public static class SeedIDs
{
    //every seed user has this password
    public const string Password = "Password123!";

    //organisers
    public const int Trevor = 1;
    public const int Sypha = 2;

    //participants
    public const int Alucard = 3;
    public const int Carmilla = 4;

    //events, comrades and the park run are trevors, cape town is syphas
    public const int ComradesMarathon = 1;
    public const int CapeTownCycleTour = 2;
    public const int DurbanParkRun = 3;

    //categories, two per event, plus a distance only one for cape town and an age and distance one for the park run
    public const int ComradesSenior = 1;
    public const int ComradesVeteran = 2;
    public const int CapeTownOpen = 3;
    public const int CapeTownMasters = 4;
    public const int ParkRunJunior = 5;
    public const int ParkRunOpen = 6;
    public const int CapeTownFunRide = 7;
    public const int ParkRunJunior2Point5Km = 8;
}