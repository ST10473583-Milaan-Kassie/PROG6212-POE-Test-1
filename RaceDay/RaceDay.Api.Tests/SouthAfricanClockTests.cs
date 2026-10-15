using RaceDay.Api.Services;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//tests for the south african clock that every date rule in the API goes through
public class SouthAfricanClockTests
{
    [Fact]
    public void Today_UsesSouthAfricanDate_NotUtcDate()
    {
        //23:30 UTC on 14 october is already 01:30 on 15 october in south africa
        var lateNightUTC = new DateTimeOffset(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);
        var clock = new SouthAfricanClock(new FixedTimeProvider(lateNightUTC));

        Assert.Equal(new DateOnly(2026, 10, 15), clock.Today);
    }

    [Fact]
    public void HasTakenPlace_CountsRaceDay_ButNotTheDayAfter()
    {
        var clock = TestClock.On(new DateOnly(2026, 6, 14));

        //the day before and race day itself have taken place
        Assert.True(clock.HasTakenPlace(new DateOnly(2026, 6, 13)));
        Assert.True(clock.HasTakenPlace(new DateOnly(2026, 6, 14)));

        //the day after is still upcoming
        Assert.False(clock.HasTakenPlace(new DateOnly(2026, 6, 15)));
    }
}