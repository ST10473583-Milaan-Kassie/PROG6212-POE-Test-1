namespace RaceDay.Api.Tests.Fakes;

//a clock thats stuck on whatever time the test gives it, so the date rules give the same answer every run
public class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    //SouthAfricanClock only ever asks for the UTC time, so this is all that needs faking
    public override DateTimeOffset GetUtcNow() => _utcNow;
}