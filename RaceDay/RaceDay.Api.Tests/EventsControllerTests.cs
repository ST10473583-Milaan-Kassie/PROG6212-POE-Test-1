using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Requests;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//organisers managing events, creating, updating, deleting and viewing their own
//on the test date (15 october 2026) comrades has taken place, cape town (syphas) and the park run (trevors) are upcoming
public class EventsControllerTests
{
    //the events controller with the test clock, fake storage and a fake session, nobody logged in yet
    private static EventsController CreateController(RaceDayDbContext context)
    {
        return CreateController(context, new FakeFileStorageService());
    }

    //same thing but the storage gets passed in, so the banner tests can check what was uploaded
    private static EventsController CreateController(RaceDayDbContext context, FakeFileStorageService storage)
    {
        var controller = new EventsController(context, TestClock.Default(), storage);
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //the park runs current details with whatever date the test wants
    private static EventRequest ParkRunRequest(DateOnly eventDate)
    {
        return new EventRequest
        {
            EventName = "Durban Beachfront Park Run",
            Description = "Casual weekly walk along the Durban beachfront.",
            EventDate = eventDate,
            Location = "Durban",
            Distance = 5.00m,
            EventTypeID = 2
        };
    }

    //enrols a 17 year old in the park runs junior category (10 to 17)
    //shes 17 on the park runs date and only turns 18 on 1 december 2027, so moving the date can push her out
    private static void AddJuniorToParkRun(RaceDayDbContext context)
    {
        var participantRole = context.Roles.Single(r => r.RoleName == RoleNames.Participant);

        var junior = new User
        {
            RoleID = participantRole.RoleID,
            FirstName = "Maria",
            LastName = "Renard",
            DateOfBirth = new DateOnly(2009, 12, 1),
            Email = "maria.renard@gmail.com",
            PasswordHash = "not-used-in-these-tests"
        };
        context.Users.Add(junior);
        context.SaveChanges();

        context.Enrolments.Add(new Enrolment
        {
            UserID = junior.UserID,
            EventID = SeedIDs.DurbanParkRun,
            CategoryID = SeedIDs.ParkRunJunior
        });
        context.SaveChanges();
    }

    //create

    [Fact]
    public async Task CreateEvent_ValidDetails_Returns201AndSavesUnderOrganiser()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CreateEvent(TestRequests.ValidEventRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<EventResponse>(created.Value);
        Assert.Equal("Umhlanga Sunset 10K", response.EventName);
        Assert.Equal("Run", response.EventTypeName);
        Assert.Equal("Trevor Belmont", response.OrganiserName);
        Assert.False(response.HasTakenPlace);

        //reads straight from the database, the event is saved and belongs to trevor
        context.ChangeTracker.Clear();
        var savedEvent = context.Events.Single(e => e.EventID == response.EventID);
        Assert.Equal(SeedIDs.Trevor, savedEvent.UserID);
        Assert.Equal(4, context.Events.Count());
    }

    [Fact]
    public async Task CreateEvent_DateTomorrow_Returns201()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);
        var request = TestRequests.ValidEventRequest();

        //tomorrow is the earliest an event can be
        request.EventDate = TestClock.DefaultToday.AddDays(1);

        var result = await controller.CreateEvent(request);

        Assert.Equal(StatusCodes.Status201Created, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task CreateEvent_DateToday_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);
        var request = TestRequests.ValidEventRequest();

        //today counts as race day so it would be locked straight away
        request.EventDate = TestClock.DefaultToday;

        var result = await controller.CreateEvent(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event date must be after today.", ResultHelper.GetMessage(result));
        Assert.Equal(3, context.Events.Count());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(10000.0)]
    public async Task CreateEvent_DistanceOutOfRange_Returns400(double distance)
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);
        var request = TestRequests.ValidEventRequest();

        //attributes cant hold decimals so the values come in as doubles and get converted
        request.Distance = (decimal)distance;

        var result = await controller.CreateEvent(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Distance must be between 0.01 and 9999.99 km.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CreateEvent_DistanceWithThreeDecimals_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);
        var request = TestRequests.ValidEventRequest();
        request.Distance = 10.005m;

        var result = await controller.CreateEvent(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Distance can have at most 2 decimal places.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CreateEvent_UnknownEventType_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);
        var request = TestRequests.ValidEventRequest();
        request.EventTypeID = 99;

        var result = await controller.CreateEvent(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event type does not exist.", ResultHelper.GetMessage(result));
    }

    //update

    [Fact]
    public async Task UpdateEvent_OwnUpcomingEvent_Returns200AndSavesChanges()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //same date, new name and location
        var request = ParkRunRequest(new DateOnly(2027, 1, 16));
        request.EventName = "Durban North Beach Park Run";
        request.Location = "North Beach, Durban";

        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponse>(ok.Value);
        Assert.Equal("Durban North Beach Park Run", response.EventName);

        //reads straight from the database to make sure the change was actually saved
        context.ChangeTracker.Clear();
        var savedEvent = context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun);
        Assert.Equal("Durban North Beach Park Run", savedEvent.EventName);
        Assert.Equal("North Beach, Durban", savedEvent.Location);
    }

    [Fact]
    public async Task UpdateEvent_SomeoneElsesEvent_Returns403AndChangesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        //sypha trying to change trevors park run
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);
        var request = ParkRunRequest(new DateOnly(2027, 1, 16));
        request.EventName = "Not Trevors Anymore";

        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only manage your own events.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.Equal("Durban Beachfront Park Run", context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun).EventName);
    }

    [Fact]
    public async Task UpdateEvent_EventAlreadyTakenPlace_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //comrades is trevors but its already happened
        var result = await controller.UpdateEvent(SeedIDs.ComradesMarathon, TestRequests.ValidEventRequest());

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This event has already taken place and can no longer be changed.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task UpdateEvent_EventDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.UpdateEvent(999, TestRequests.ValidEventRequest());

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event not found.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task UpdateEvent_NewDateKeepsEnrolmentsInRange_Returns200()
    {
        using var context = TestDbContextFactory.Create();
        AddJuniorToParkRun(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //the day before marias 18th birthday, shes still 17 so still fits junior
        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, ParkRunRequest(new DateOnly(2027, 11, 30)));

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task UpdateEvent_NewDatePushesEnrolmentOutOfRange_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        AddJuniorToParkRun(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //marias 18th birthday, she'd be too old for junior on race day
        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, ParkRunRequest(new DateOnly(2027, 12, 1)));

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This change would leave 1 enrolled participant outside their category's age range.", ResultHelper.GetMessage(result));

        //the date didnt move
        context.ChangeTracker.Clear();
        Assert.Equal(new DateOnly(2027, 1, 16), context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun).EventDate);
    }

    [Fact]
    public async Task UpdateEvent_ShortenedToLongestCategory_Returns200()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //the park runs longest category is junior 2.5km, so 2.5km is as short as the event can go
        var request = ParkRunRequest(new DateOnly(2027, 1, 16));
        request.Distance = 2.50m;

        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task UpdateEvent_ShorterThanACategory_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //2km would make the event shorter than its junior 2.5km category
        var request = ParkRunRequest(new DateOnly(2027, 1, 16));
        request.Distance = 2.00m;

        var result = await controller.UpdateEvent(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This change would leave 1 category longer than the event's distance.", ResultHelper.GetMessage(result));

        //the distance didnt change
        context.ChangeTracker.Clear();
        Assert.Equal(5.00m, context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun).Distance);
    }

    //delete

    [Fact]
    public async Task DeleteEvent_OwnUpcomingEvent_Returns204AndRemovesCategoriesAndEnrolments()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.DeleteEvent(SeedIDs.DurbanParkRun);

        Assert.IsType<NoContentResult>(result);

        //the park run, its 3 categories and carmillas enrolment are all gone
        context.ChangeTracker.Clear();
        Assert.DoesNotContain(context.Events, e => e.EventID == SeedIDs.DurbanParkRun);
        Assert.DoesNotContain(context.Categories, c => c.EventID == SeedIDs.DurbanParkRun);
        Assert.DoesNotContain(context.Enrolments, en => en.EventID == SeedIDs.DurbanParkRun);

        //the other events werent touched
        Assert.Equal(2, context.Events.Count());
        Assert.Equal(5, context.Categories.Count());
        Assert.Equal(3, context.Enrolments.Count());
    }

    [Fact]
    public async Task DeleteEvent_SomeoneElsesEvent_Returns403AndDeletesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        //trevor trying to delete syphas cape town cycle tour
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.DeleteEvent(SeedIDs.CapeTownCycleTour);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only manage your own events.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.Contains(context.Events, e => e.EventID == SeedIDs.CapeTownCycleTour);
    }

    [Fact]
    public async Task DeleteEvent_EventAlreadyTakenPlace_Returns409AndKeepsHistory()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.DeleteEvent(SeedIDs.ComradesMarathon);

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This event has already taken place and can no longer be deleted.", ResultHelper.GetMessage(result));

        //comrades and both its results are still there
        context.ChangeTracker.Clear();
        Assert.Contains(context.Events, e => e.EventID == SeedIDs.ComradesMarathon);
        Assert.Equal(2, context.Results.Count());
    }

    [Fact]
    public async Task DeleteEvent_EventDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.DeleteEvent(999);

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event not found.", ResultHelper.GetMessage(result));
    }

    //viewing

    [Fact]
    public async Task GetMyEvents_ReturnsOnlyOwnEventsWithEnrolmentTotals()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.GetMyEvents();

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();

        //trevors two events soonest first, syphas cape town isnt in here
        Assert.Equal(2, events.Count);
        Assert.Equal(SeedIDs.ComradesMarathon, events[0].EventID);
        Assert.Equal(SeedIDs.DurbanParkRun, events[1].EventID);

        //comrades has alucard and carmilla, the park run has carmilla
        Assert.Equal(2, events[0].TotalEnrolments);
        Assert.Equal(1, events[1].TotalEnrolments);

        //all three of those enrolments are confirmed
        Assert.Equal(2, events[0].ConfirmedEnrolments);
        Assert.Equal(1, events[1].ConfirmedEnrolments);
    }

    [Fact]
    public async Task GetMyEvents_PendingEnrolment_CountsInTotalButNotConfirmed()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.GetMyEvents();

        var ok = Assert.IsType<OkObjectResult>(result);
        var capeTown = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value));

        //alucards cape town enrolment is still pending
        Assert.Equal(SeedIDs.CapeTownCycleTour, capeTown.EventID);
        Assert.Equal(1, capeTown.TotalEnrolments);
        Assert.Equal(0, capeTown.ConfirmedEnrolments);
    }

    [Fact]
    public async Task GetEvents_NotLoggedIn_ReturnsAllEventsSoonestFirst()
    {
        using var context = TestDbContextFactory.Create();

        //no LogInAs, viewing events is public
        var controller = CreateController(context);

        var result = await controller.GetEvents(new EventFilterQuery());

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();

        Assert.Equal(3, events.Count);
        Assert.Equal(SeedIDs.ComradesMarathon, events[0].EventID);
        Assert.Equal(SeedIDs.DurbanParkRun, events[1].EventID);
        Assert.Equal(SeedIDs.CapeTownCycleTour, events[2].EventID);

        //comrades is the only one thats taken place
        Assert.True(events[0].HasTakenPlace);
        Assert.False(events[1].HasTakenPlace);
    }

    [Fact]
    public async Task GetEvents_UpcomingOnly_LeavesOutPastEvents()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.GetEvents(new EventFilterQuery { UpcomingOnly = true });

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();

        Assert.Equal(2, events.Count);
        Assert.DoesNotContain(events, e => e.EventID == SeedIDs.ComradesMarathon);
    }

    //banner

    [Fact]
    public async Task UploadBanner_OwnEvent_Returns200AndSavesURL()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.UploadBanner(SeedIDs.DurbanParkRun, new ImageUploadRequest { File = TestFiles.Png("banner.png") });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponse>(ok.Value);

        //the URL storage gave back is the one sent back and saved
        var uploadedURL = Assert.Single(storage.UploadedURLs);
        Assert.Equal(uploadedURL, response.BannerImageURL);
        Assert.StartsWith("https://storage.test/raceday-images/event-banners/", uploadedURL);

        context.ChangeTracker.Clear();
        Assert.Equal(uploadedURL, context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun).BannerImageURL);
    }

    [Fact]
    public async Task UploadBanner_SomeoneElsesEvent_Returns403AndUploadsNothing()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);

        //sypha trying to put a banner on trevors park run
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.UploadBanner(SeedIDs.DurbanParkRun, new ImageUploadRequest { File = TestFiles.Png("banner.png") });

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only manage your own events.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);

        context.ChangeTracker.Clear();
        Assert.Null(context.Events.Single(e => e.EventID == SeedIDs.DurbanParkRun).BannerImageURL);
    }

    [Fact]
    public async Task UploadBanner_NotAnImage_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.UploadBanner(SeedIDs.DurbanParkRun, new ImageUploadRequest { File = TestFiles.Text() });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Only JPG, PNG, or WEBP images are allowed.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);
    }
}