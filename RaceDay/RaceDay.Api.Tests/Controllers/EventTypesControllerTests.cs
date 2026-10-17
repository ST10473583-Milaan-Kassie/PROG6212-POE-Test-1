using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.Controllers;

//event types is a read only lookup with one public endpoint, so theres nothing more to test than these two
public class EventTypesControllerTests
{
    [Fact]
    public async Task GetEventTypes_ReturnsRunWalkAndCycleInOrder()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new EventTypesController(context);

        var result = await controller.GetEventTypes();

        var ok = Assert.IsType<OkObjectResult>(result);
        var eventTypes = Assert.IsAssignableFrom<IEnumerable<EventTypeResponse>>(ok.Value).ToList();

        Assert.Equal(3, eventTypes.Count);
        Assert.Equal("Run", eventTypes[0].EventTypeName);
        Assert.Equal("Walk", eventTypes[1].EventTypeName);
        Assert.Equal("Cycle", eventTypes[2].EventTypeName);
    }

    [Fact]
    public async Task GetEventTypes_NewTypeInDatabase_ShowsUpLast()
    {
        using var context = TestDbContextFactory.Create();

        //makes sure the list comes from the table and isnt just written into the controller
        context.EventTypes.Add(new EventType { EventTypeName = "Swim" });
        context.SaveChanges();

        var controller = new EventTypesController(context);

        var result = await controller.GetEventTypes();

        var ok = Assert.IsType<OkObjectResult>(result);
        var eventTypes = Assert.IsAssignableFrom<IEnumerable<EventTypeResponse>>(ok.Value).ToList();

        Assert.Equal(4, eventTypes.Count);
        Assert.Equal("Swim", eventTypes[3].EventTypeName);
    }
}