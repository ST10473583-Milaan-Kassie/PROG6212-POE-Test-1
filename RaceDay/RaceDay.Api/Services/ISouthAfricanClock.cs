namespace RaceDay.Api.Services;

//one clock for south african time, every date rule in the API goes through this
//its an interface so the tests can swap in a fake date
public interface ISouthAfricanClock
{
    //todays date in south africa
    DateOnly Today { get; }

    //an event has taken place from its event date onwards, race day itself counts
    bool HasTakenPlace(DateOnly eventDate);
}