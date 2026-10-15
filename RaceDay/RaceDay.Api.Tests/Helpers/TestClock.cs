using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Helpers;

//builds the real south african clock stuck on a set date
//so the tests still go through the actual clock code, just not the real date
public static class TestClock
{
    //the date the tests pretend it is
    //comrades (14 june 2026) has taken place, cape town and the park run are still upcoming
    public static readonly DateOnly DefaultToday = new(2026, 10, 15);

    //midday in south africa on the given date, nowhere near midnight so the date cant slip a day
    public static ISouthAfricanClock On(DateOnly southAfricanDate)
    {
        var middayInSouthAfrica = new DateTimeOffset(southAfricanDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(2));
        return new SouthAfricanClock(new FixedTimeProvider(middayInSouthAfrica));
    }

    //the clock most tests use
    public static ISouthAfricanClock Default() => On(DefaultToday);
}