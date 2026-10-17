using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Requests;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Helpers;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//categories defined by an age range, a distance, or both
//the park run is trevors and 5km long, cape town is syphas and 109km long
public class CategoriesControllerTests
{
    //the categories controller with the test clock and a fake session, nobody logged in yet
    private static CategoriesController CreateController(RaceDayDbContext context)
    {
        var controller = new CategoriesController(context, TestClock.Default());
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //viewing

    [Fact]
    public async Task GetCategories_ParkRun_ReturnsAgeAndDistanceCategoriesYoungestFirst()
    {
        using var context = TestDbContextFactory.Create();

        //no LogInAs, viewing categories is public
        var controller = CreateController(context);

        var result = await controller.GetCategories(SeedIDs.DurbanParkRun);

        var ok = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<CategoryResponse>>(ok.Value).ToList();

        //both juniors start at 10, the one with no distance comes first, then open at 18
        Assert.Equal(3, categories.Count);
        Assert.Equal(SeedIDs.ParkRunJunior, categories[0].CategoryID);
        Assert.Equal(SeedIDs.ParkRunJunior2Point5Km, categories[1].CategoryID);
        Assert.Equal(SeedIDs.ParkRunOpen, categories[2].CategoryID);

        //junior 2.5km has both an age range and a distance
        Assert.Equal(10, categories[1].MinAge);
        Assert.Equal(17, categories[1].MaxAge);
        Assert.Equal(2.50m, categories[1].Distance);
    }

    [Fact]
    public async Task GetCategories_CapeTown_DistanceOnlyCategoryComesFirst()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.GetCategories(SeedIDs.CapeTownCycleTour);

        var ok = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<CategoryResponse>>(ok.Value).ToList();

        //the fun ride is open to every age so it has no min age and sorts first
        Assert.Equal(3, categories.Count);
        Assert.Equal(SeedIDs.CapeTownFunRide, categories[0].CategoryID);
        Assert.Null(categories[0].MinAge);
        Assert.Null(categories[0].MaxAge);
        Assert.Equal(47.00m, categories[0].Distance);
    }

    //adding

    [Fact]
    public async Task AddCategory_AgeOnly_Returns201AndSaves()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Veteran", MinAge = 40, MaxAge = 59 };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<CategoryResponse>(created.Value);
        Assert.Equal(40, response.MinAge);
        Assert.Null(response.Distance);

        //reads straight from the database to make sure it was saved
        context.ChangeTracker.Clear();
        var saved = context.Categories.Single(c => c.CategoryID == response.CategoryID);
        Assert.Equal(SeedIDs.DurbanParkRun, saved.EventID);
        Assert.Equal(59, saved.MaxAge);
        Assert.Null(saved.Distance);
    }

    [Fact]
    public async Task AddCategory_DistanceOnly_Returns201AndSaves()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Fun Walk 3km", Distance = 3.00m };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<CategoryResponse>(created.Value);
        Assert.Null(response.MinAge);
        Assert.Null(response.MaxAge);
        Assert.Equal(3.00m, response.Distance);

        context.ChangeTracker.Clear();
        var saved = context.Categories.Single(c => c.CategoryID == response.CategoryID);
        Assert.Null(saved.MinAge);
        Assert.Equal(3.00m, saved.Distance);
    }

    [Fact]
    public async Task AddCategory_AgeAndDistanceMatchingEvent_Returns201()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //5km is the park runs full distance, a category can be exactly as long as its event
        var request = new CategoryRequest { CategoryName = "Senior 5km", MinAge = 20, MaxAge = 39, Distance = 5.00m };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status201Created, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task AddCategory_NoAgeRangeOrDistance_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, new CategoryRequest { CategoryName = "Nothing" });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("A category must have an age range, a distance, or both.", ResultHelper.GetMessage(result));
        Assert.Equal(8, context.Categories.Count());
    }

    [Fact]
    public async Task AddCategory_MaxAgeWithoutMinAge_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Under 20", MaxAge = 19, Distance = 5.00m };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("A maximum age needs a minimum age to go with it.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task AddCategory_MinAgeBelow10_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Kids", MinAge = 9, MaxAge = 12 };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Minimum age must be between 10 and 99.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task AddCategory_DistanceLongerThanEvent_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //just past the park runs 5km
        var request = new CategoryRequest { CategoryName = "Too Far", Distance = 5.01m };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Category distance cannot be longer than the event's distance of 5.00 km.", ResultHelper.GetMessage(result));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(10000.0)]
    public async Task AddCategory_DistanceOutOfRange_Returns400(double distance)
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Sypha, RoleNames.Organiser);

        //attributes cant hold decimals so the values come in as doubles and get converted
        var request = new CategoryRequest { CategoryName = "Bad Distance", Distance = (decimal)distance };

        var result = await controller.AddCategory(SeedIDs.CapeTownCycleTour, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Distance must be between 0.01 and 9999.99 km.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task AddCategory_DistanceWithThreeDecimals_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Odd Distance", Distance = 2.505m };

        var result = await controller.AddCategory(SeedIDs.DurbanParkRun, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Distance can have at most 2 decimal places.", ResultHelper.GetMessage(result));
    }

    //updating

    [Fact]
    public async Task UpdateCategory_RemovingAgeRange_OpensItToEveryone_Returns200()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        //carmilla is enrolled in the park runs open category, dropping the ages cant leave her outside it
        var request = new CategoryRequest { CategoryName = "Open 5km", Distance = 5.00m };

        var result = await controller.UpdateCategory(SeedIDs.ParkRunOpen, request);

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));

        context.ChangeTracker.Clear();
        var saved = context.Categories.Single(c => c.CategoryID == SeedIDs.ParkRunOpen);
        Assert.Null(saved.MinAge);
        Assert.Equal(5.00m, saved.Distance);
    }

    [Fact]
    public async Task UpdateCategory_DistanceLongerThanEvent_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        var request = new CategoryRequest { CategoryName = "Junior 6km", MinAge = 10, MaxAge = 17, Distance = 6.00m };

        var result = await controller.UpdateCategory(SeedIDs.ParkRunJunior2Point5Km, request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Category distance cannot be longer than the event's distance of 5.00 km.", ResultHelper.GetMessage(result));

        //still 2.5km
        context.ChangeTracker.Clear();
        Assert.Equal(2.50m, context.Categories.Single(c => c.CategoryID == SeedIDs.ParkRunJunior2Point5Km).Distance);
    }
}
