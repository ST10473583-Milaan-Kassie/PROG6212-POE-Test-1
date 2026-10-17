using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Requests;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.Controllers;

//participants enrolling and withdrawing, and organisers viewing and confirming enrolments
//seed enrolments: 1 alucard comrades, 2 carmilla comrades (both with results), 3 alucard cape town (pending), 4 carmilla park run
public class EnrolmentsControllerTests
{
    //the enrolments controller with the test clock and a fake session, nobody logged in yet
    private static EnrolmentsController CreateController(RaceDayDbContext context)
    {
        var controller = new EnrolmentsController(context, TestClock.Default());
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //enrol

    [Fact]
    public async Task Enrol_ValidCategory_Returns201AndRecordsEnrolment()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //alucard is 28 on the park runs date, so open (18 and up) fits
        var result = await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunOpen });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<EnrolmentResponse>(created.Value);
        Assert.Equal(SeedIDs.DurbanParkRun, response.EventID);
        Assert.Equal(SeedIDs.ParkRunOpen, response.CategoryID);
        Assert.Equal("Open", response.CategoryName);
        Assert.Equal(EnrolmentStatuses.Pending, response.Status);

        //reads straight from the database, the participant, event and category are all linked properly
        context.ChangeTracker.Clear();
        var saved = context.Enrolments.Single(en => en.EnrolmentID == response.EnrolmentID);
        Assert.Equal(SeedIDs.Alucard, saved.UserID);
        Assert.Equal(SeedIDs.DurbanParkRun, saved.EventID);
        Assert.Equal(SeedIDs.ParkRunOpen, saved.CategoryID);
        Assert.Equal(EnrolmentStatuses.Pending, saved.Status);
        Assert.Equal(5, context.Enrolments.Count());
    }

    [Fact]
    public async Task Enrol_NewEnrolment_ShowsInMyEnrolments()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunOpen });
        var result = await controller.GetMyEnrolments();

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value).ToList();

        //comrades, then the new park run, then cape town, soonest first
        Assert.Equal(3, enrolments.Count);
        Assert.Equal(SeedIDs.ComradesMarathon, enrolments[0].EventID);
        Assert.Equal(SeedIDs.DurbanParkRun, enrolments[1].EventID);
        Assert.Equal(SeedIDs.CapeTownCycleTour, enrolments[2].EventID);

        //comrades is done so it cant be withdrawn from anymore, the park run still can
        Assert.True(enrolments[0].HasTakenPlace);
        Assert.False(enrolments[1].HasTakenPlace);
    }

    [Fact]
    public async Task Enrol_EventDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.Enrol(999, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunOpen });

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Event not found.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Enrol_EventAlreadyTakenPlace_Returns400()
    {
        using var context = TestDbContextFactory.Create();

        //someone new, bcs alucard and carmilla are both already in comrades
        var participantID = TestUsers.AddParticipant(context, new DateOnly(1995, 1, 1));
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, participantID, RoleNames.Participant);

        var result = await controller.Enrol(SeedIDs.ComradesMarathon, new EnrolmentRequest { CategoryID = SeedIDs.ComradesSenior });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("This event has already taken place, so it can no longer be entered.", ResultHelper.GetMessage(result));
        Assert.Equal(4, context.Enrolments.Count());
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //cape towns open category sent for the park run
        var result = await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.CapeTownOpen });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("The selected category is not part of this event.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Enrol_AlreadyEnrolled_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //alucard is already in cape town (enrolment 3)
        var result = await controller.Enrol(SeedIDs.CapeTownCycleTour, new EnrolmentRequest { CategoryID = SeedIDs.CapeTownOpen });

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("You are already enrolled in this event.", ResultHelper.GetMessage(result));
        Assert.Equal(4, context.Enrolments.Count());
    }

    [Fact]
    public async Task Enrol_OutsideCategoryAgeRange_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //alucard is 28 on race day, way too old for junior (10 to 17)
        var result = await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunJunior });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("You will be 28 on the event date, which is outside this category's age range.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Enrol_YoungEnoughTodayButTooOldOnRaceDay_Returns400()
    {
        using var context = TestDbContextFactory.Create();

        //17 on the test date, turns 18 on 10 january 2027, before the park run on the 16th
        var participantID = TestUsers.AddParticipant(context, new DateOnly(2009, 1, 10));
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, participantID, RoleNames.Participant);

        var result = await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunJunior });

        //junior is 10 to 17, they'll be 18 on race day
        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("You will be 18 on the event date, which is outside this category's age range.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Enrol_TooYoungTodayButOldEnoughOnRaceDay_Returns201()
    {
        using var context = TestDbContextFactory.Create();

        //same person as above, 17 today but 18 on race day
        var participantID = TestUsers.AddParticipant(context, new DateOnly(2009, 1, 10));
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, participantID, RoleNames.Participant);

        //open is 18 and up, they fit on race day even though theyre not 18 yet
        var result = await controller.Enrol(SeedIDs.DurbanParkRun, new EnrolmentRequest { CategoryID = SeedIDs.ParkRunOpen });

        Assert.Equal(StatusCodes.Status201Created, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task Enrol_DistanceOnlyCategory_AnyAgeCanEnter()
    {
        using var context = TestDbContextFactory.Create();

        //12 on the day of cape town, too young for open (18 to 49) but the fun ride has no age range
        var participantID = TestUsers.AddParticipant(context, new DateOnly(2014, 6, 1));
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, participantID, RoleNames.Participant);

        var result = await controller.Enrol(SeedIDs.CapeTownCycleTour, new EnrolmentRequest { CategoryID = SeedIDs.CapeTownFunRide });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<EnrolmentResponse>(created.Value);
        Assert.Equal("Fun Ride", response.CategoryName);
    }

    //withdraw

    [Fact]
    public async Task Withdraw_OwnUpcomingEnrolment_Returns204AndRemovesIt()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //enrolment 3 is alucards cape town entry
        var result = await controller.Withdraw(3);

        Assert.IsType<NoContentResult>(result);

        context.ChangeTracker.Clear();
        Assert.DoesNotContain(context.Enrolments, en => en.EnrolmentID == 3);
        Assert.Equal(3, context.Enrolments.Count());
    }

    [Fact]
    public async Task Withdraw_SomeoneElsesEnrolment_Returns403AndKeepsIt()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //enrolment 4 is carmillas park run entry
        var result = await controller.Withdraw(4);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only withdraw your own enrolments.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.Contains(context.Enrolments, en => en.EnrolmentID == 4);
    }

    [Fact]
    public async Task Withdraw_EventAlreadyTakenPlace_Returns409AndKeepsHistory()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //enrolment 1 is alucards comrades entry, already run with a result
        var result = await controller.Withdraw(1);

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This event has already taken place, so the enrolment is kept as a historical record.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.Contains(context.Enrolments, en => en.EnrolmentID == 1);
    }

    [Fact]
    public async Task Withdraw_EnrolmentDoesNotExist_Returns404()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.Withdraw(999);

        Assert.Equal(StatusCodes.Status404NotFound, ResultHelper.GetStatusCode(result));
        Assert.Equal("Enrolment not found.", ResultHelper.GetMessage(result));
    }

    //organisers viewing and confirming

    [Fact]
    public async Task GetEventEnrolments_OwnEvent_ReturnsParticipantsInCategoryOrder()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.GetEventEnrolments(SeedIDs.ComradesMarathon, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<IEnumerable<EventEnrolmentResponse>>(ok.Value).ToList();

        //senior (20 and up) comes before veteran (40 and up)
        Assert.Equal(2, enrolments.Count);
        Assert.Equal("Alucard Tepes", enrolments[0].ParticipantName);
        Assert.Equal("Senior", enrolments[0].CategoryName);
        Assert.Equal("Carmilla Karnstein", enrolments[1].ParticipantName);
        Assert.Equal("Veteran", enrolments[1].CategoryName);

        //both already have results captured
        Assert.All(enrolments, en => Assert.True(en.HasResult));
    }

    [Fact]
    public async Task GetEventEnrolments_SomeoneElsesEvent_Returns403()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        //trevor trying to see who entered syphas cape town cycle tour
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.GetEventEnrolments(SeedIDs.CapeTownCycleTour, null);

        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal("You can only view enrolments for your own events.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_PendingToConfirmed_Returns200AndSaves()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        //sypha confirming alucards pending cape town entry
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        var result = await controller.UpdateEnrolmentStatus(3, new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventEnrolmentResponse>(ok.Value);
        Assert.Equal(EnrolmentStatuses.Confirmed, response.Status);

        context.ChangeTracker.Clear();
        Assert.Equal(EnrolmentStatuses.Confirmed, context.Enrolments.Single(en => en.EnrolmentID == 3).Status);
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_ResultAlreadyCaptured_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //enrolment 1 already has alucards comrades result
        var result = await controller.UpdateEnrolmentStatus(1, new UpdateEnrolmentStatusRequest { Status = "Pending" });

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("A result has already been captured for this enrolment, so its status can no longer be changed.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_InvalidStatus_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.UpdateEnrolmentStatus(4, new UpdateEnrolmentStatusRequest { Status = "Cancelled" });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Status must be either Pending or Confirmed.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_WouldDropBelowCapturedPosition_Returns409()
    {
        using var context = TestDbContextFactory.Create();

        //two more confirmed comrades runners, one already placed 4th and one still waiting on their result
        var waitingID = TestUsers.AddParticipant(context, new DateOnly(1990, 1, 1));
        var fourthID = TestUsers.AddParticipant(context, new DateOnly(1990, 1, 1));

        var waiting = new Enrolment
        {
            UserID = waitingID,
            EventID = SeedIDs.ComradesMarathon,
            CategoryID = SeedIDs.ComradesSenior,
            Status = EnrolmentStatuses.Confirmed
        };

        var fourth = new Enrolment
        {
            UserID = fourthID,
            EventID = SeedIDs.ComradesMarathon,
            CategoryID = SeedIDs.ComradesSenior,
            Status = EnrolmentStatuses.Confirmed,
            Result = new Result { FinishTime = new TimeOnly(8, 30, 0), FinishingPosition = 4 }
        };

        context.Enrolments.AddRange(waiting, fourth);
        context.SaveChanges();

        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //4 confirmed now, taking one away leaves 3, but 4th place has already been captured
        var result = await controller.UpdateEnrolmentStatus(waiting.EnrolmentID, new UpdateEnrolmentStatusRequest { Status = "Pending" });

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("This enrolment cannot be set back to Pending, a finishing position of 4 has already been captured for this event.", ResultHelper.GetMessage(result));

        context.ChangeTracker.Clear();
        Assert.Equal(EnrolmentStatuses.Confirmed, context.Enrolments.Single(en => en.EnrolmentID == waiting.EnrolmentID).Status);
    }
}