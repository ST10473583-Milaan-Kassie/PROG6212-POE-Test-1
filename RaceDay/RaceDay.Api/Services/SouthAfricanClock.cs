namespace RaceDay.Api.Services;

//gets todays date in south africa no matter what timezone the server is set to
public class SouthAfricanClock : ISouthAfricanClock
{
    private static readonly TimeZoneInfo SouthAfricaTimeZone = GetSouthAfricaTimeZone();

    //timeprovider is .nets built in clock, used instead of DateTime.Now so tests can fake the time
    private readonly TimeProvider _timeProvider;

    public SouthAfricanClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateOnly Today
    {
        get
        {
            //takes the current UTC time and moves it into south african time before taking the date
            var southAfricanNow = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), SouthAfricaTimeZone);
            return DateOnly.FromDateTime(southAfricanNow.DateTime);
        }
    }

    public bool HasTakenPlace(DateOnly eventDate)
    {
        //on or before today, so race day counts as having taken place
        return eventDate <= Today;
    }

    //the iana ID works on windows and linux, the windows ID is just a backup
    private static TimeZoneInfo GetSouthAfricaTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Africa/Johannesburg", out var timeZone))
        {
            return timeZone;
        }

        return TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
    }
}