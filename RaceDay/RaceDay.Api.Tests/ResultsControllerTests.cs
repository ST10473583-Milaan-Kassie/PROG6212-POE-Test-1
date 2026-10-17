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

//organisers capturing, checking and publishing results, and participants viewing their own once theyre published
//seed results: alucard 1st (enrolment 1) and carmilla 2nd (enrolment 2) at comrades, captured but not published yet
public class ResultsControllerTests
{
    //the results controller with the test clock and a fake session, nobody logged in yet
    private static ResultsController CreateController(RaceDayDbContext context)
    {
        var controller = new ResultsController(context, TestClock.Default());
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //adds another runner to comrades with no result yet and gives back their enrolment ID
    //confirmed by default, which makes 3 confirmed at comrades so positions 1 to 3 are allowed
    private static int AddComradesRunner(RaceDayDbContext context, string status = EnrolmentStatuses.Confirmed)
    {
        //31 on comrades day, fits senior (20 to 39)
        var userID = TestUsers.AddParticipant(context, new DateOnly(1995, 1, 1));

        var enrolment = new Enrolment
        {
            UserID = userID,
            EventID = SeedIDs.ComradesMarathon,
            CategoryID = SeedIDs.ComradesSenior,
            Status = status
        };
        context.Enrolments.Add(enrolment);
        context.SaveChanges();

        return enrolment.EnrolmentID;
    }

    //a valid result for 3rd place
    private static ResultRequest ThirdPlace()
    {
        return new ResultRequest { FinishTime = new TimeOnly(9, 15, 30), FinishingPosition = 3 };
    }

    //marks comrades as published straight in the database, for the tests that start after publishing
    private static void PublishComrades(RaceDayDbContext context)
    {
        context.Events.Single(e => e.EventID == SeedIDs.ComradesMarathon).ResultsPublished = true;
        context.SaveChanges();
    }

    //capturing

    [Fact]
    public async Task CaptureResult_ConfirmedEnrolment_Returns201AndSavesResult()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CaptureResult(enrolmentID, ThirdPlace());

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<EventResultResponse>(created.Value);
        Assert.Equal(enrolmentID, response.EnrolmentID);
        Assert.Equal("Test Participant", response.ParticipantName);
        Assert.Equal("Senior", response.CategoryName);
        Assert.Equal(3, response.FinishingPosition);

        //reads straight from the database, the result is saved against the right enrolment
        context.ChangeTracker.Clear();
        var saved = context.Results.Single(r => r.EnrolmentID == enrolmentID);
        Assert.Equal(new TimeOnly(9, 15, 30), saved.FinishTime);
        Assert.Equal(3, context.Results.Count());
    }

    [Fact]
    public async Task CaptureResult_SomeoneElsesEvent_Returns403AndSavesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        var controller = CreateController(context);

        //sypha trying to capture a result for trevors comrades
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.CaptureResult(enrolmentID, ThirdPlace());

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only capture results for your own events.", ResultHelper.GetMessage(result));
        Assert.Equal(2, context.Results.Count());
    }

    [Fact]
    public async Task CaptureResult_EnrolmentDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CaptureResult(999, ThirdPlace());

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Enrolment not found.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_AlreadyCaptured_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //alucard already has his 1st place at comrades
        var result = await controller.CaptureResult(1, ThirdPlace());

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("A result has already been captured for this enrolment.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_EventNotTakenPlace_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //carmillas park run enrolment is confirmed but the park run is still upcoming
        var result = await controller.CaptureResult(4, new ResultRequest { FinishTime = new TimeOnly(0, 28, 10), FinishingPosition = 1 });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Results can only be captured once the event has taken place.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_PendingEnrolment_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context, EnrolmentStatuses.Pending);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CaptureResult(enrolmentID, new ResultRequest { FinishTime = new TimeOnly(9, 15, 30), FinishingPosition = 2 });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Results can only be captured for confirmed enrolments.", ResultHelper.GetMessage(result));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CaptureResult_PositionBelowOne_Returns400(int position)
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CaptureResult(enrolmentID, new ResultRequest { FinishTime = new TimeOnly(9, 15, 30), FinishingPosition = position });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Finishing position must be 1 or higher.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_PositionAboveConfirmedCount_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //only 3 confirmed runners at comrades so nobody can come 4th
        var result = await controller.CaptureResult(enrolmentID, new ResultRequest { FinishTime = new TimeOnly(9, 15, 30), FinishingPosition = 4 });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Finishing position cannot be higher than the number of confirmed participants (3).", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_ZeroFinishTime_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.CaptureResult(enrolmentID, new ResultRequest { FinishTime = TimeOnly.MinValue, FinishingPosition = 3 });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Finish time must be more than 00:00:00.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task CaptureResult_ResultsAlreadyPublished_Returns409AndSavesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var enrolmentID = AddComradesRunner(context);
        PublishComrades(context);
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //published results are final, so the new runner cant be added anymore
        var result = await controller.CaptureResult(enrolmentID, ThirdPlace());

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("Results for this event have already been published.", ResultHelper.GetMessage(result));
        Assert.Equal(2, context.Results.Count());
    }

    //organiser viewing

    [Fact]
    public async Task GetEventResults_OwnEvent_ShowsUnpublishedResultsInOrder()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.GetEventResults(SeedIDs.ComradesMarathon);

        var ok = Assert.IsType<OkObjectResult>(result);
        var results = Assert.IsAssignableFrom<IEnumerable<EventResultResponse>>(ok.Value).ToList();

        //comrades isnt published yet but the organiser still sees both results so they can be checked first
        Assert.Equal(2, results.Count);
        Assert.Equal("Alucard Tepes", results[0].ParticipantName);
        Assert.Equal(1, results[0].FinishingPosition);
        Assert.Equal("Carmilla Karnstein", results[1].ParticipantName);
        Assert.Equal(2, results[1].FinishingPosition);
    }

    [Fact]
    public async Task GetEventResults_SomeoneElsesEvent_Returns403()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.GetEventResults(SeedIDs.ComradesMarathon);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only view results for your own events.", ResultHelper.GetMessage(result));
    }

    //publishing

    [Fact]
    public async Task PublishResults_OwnEventWithResults_Returns200AndSavesFlag()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.PublishResults(SeedIDs.ComradesMarathon);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponse>(ok.Value);
        Assert.Equal(SeedIDs.ComradesMarathon, response.EventID);
        Assert.True(response.ResultsPublished);

        //reads straight from the database to make sure it was actually saved
        context.ChangeTracker.Clear();
        Assert.True(context.Events.Single(e => e.EventID == SeedIDs.ComradesMarathon).ResultsPublished);
    }

    [Fact]
    public async Task PublishResults_SomeoneElsesEvent_Returns403AndPublishesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        //sypha trying to publish trevors comrades results
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.PublishResults(SeedIDs.ComradesMarathon);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only publish results for your own events.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.False(context.Events.Single(e => e.EventID == SeedIDs.ComradesMarathon).ResultsPublished);
    }

    [Fact]
    public async Task PublishResults_EventDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.PublishResults(999);

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event not found.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task PublishResults_EventNotTakenPlace_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //the park run is trevors but its still upcoming
        var result = await controller.PublishResults(SeedIDs.DurbanParkRun);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Results can only be published once the event has taken place.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task PublishResults_NoResultsCaptured_Returns400()
    {
        using var context = TestDbContextFactory.Create();

        //an event of trevors that has taken place but has no results yet
        var pastEvent = new Event
        {
            EventName = "Durban Spring 10K",
            EventTypeID = 1,
            UserID = SeedIDs.Trevor,
            EventDate = new DateOnly(2026, 9, 1),
            Location = "Durban",
            Distance = 10.00m
        };
        context.Events.Add(pastEvent);
        context.SaveChanges();

        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.PublishResults(pastEvent.EventID);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("There are no results to publish for this event yet.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task PublishResults_AlreadyPublished_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //the first publish works, the second one gets turned away
        await controller.PublishResults(SeedIDs.ComradesMarathon);
        var result = await controller.PublishResults(SeedIDs.ComradesMarathon);

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("Results for this event have already been published.", ResultHelper.GetMessage(result));
    }

    //participant viewing

    [Fact]
    public async Task GetMyResults_NotPublishedYet_ReturnsNothing()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.GetMyResults();

        //alucard has a comrades result but trevor hasnt published it yet
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ParticipantResultResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetMyResults_AfterPublishing_ShowsOwnResultWithTotalFinishers()
    {
        using var context = TestDbContextFactory.Create();

        //trevor publishes comrades through the actual endpoint first
        var organiserController = CreateController(context);
        ControllerTestHelper.LogInAs(organiserController, SeedIDs.Trevor, RoleNames.Organiser);
        await organiserController.PublishResults(SeedIDs.ComradesMarathon);

        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.GetMyResults();

        var ok = Assert.IsType<OkObjectResult>(result);
        var myResult = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ParticipantResultResponse>>(ok.Value));

        //only alucards own result, 1st out of the 2 finishers
        Assert.Equal(SeedIDs.ComradesMarathon, myResult.EventID);
        Assert.Equal(1, myResult.FinishingPosition);
        Assert.Equal(2, myResult.TotalFinishers);
        Assert.Equal(new TimeOnly(7, 45, 12), myResult.FinishTime);
    }
}