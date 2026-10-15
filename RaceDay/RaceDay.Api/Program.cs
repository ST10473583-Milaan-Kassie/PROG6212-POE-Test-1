using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

//registers the controllers so the API can find the endpoints
//validation errors come back in the same { message } shape as every other error, with each field's errors listed underneath
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                message = "The request contains invalid details.",
                errors
            });
        };
    });

//registers swagger so every endpoint can be viewed and tested in the browser
builder.Services.AddSwaggerGen(options =>
{
    //the title and description at the top of the swagger page
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "REST API for the RaceDay race event management system. " +
            "Log in with POST /api/auth/login first. The browser keeps the session cookie, " +
            "so endpoints that require a login can then be tried directly on this page. " +
            "Log out with POST /api/auth/logout."
    });

    //pulls the /// comments into swagger, true includes the ones above each controller as section descriptions
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);
});

//registers the dbcontext, the connection string comes from appsettings.json
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayConnection")));

//blob storage for uploaded images, azurite locally, only the connection string changes for real azure in part 3
//the client is a singleton bcs its safe to share and reuses its connections
builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));
builder.Services.AddScoped<IFileStorageService, BlobStorageService>();

//the real system clock, and the south african clock that uses it
//singletons bcs one copy is shared by the whole app
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISouthAfricanClock, SouthAfricanClock>();

//session data gets kept in the server's memory, the browser only gets a cookie with the session ID
builder.Services.AddDistributedMemoryCache();

//server side sessions, keeps the logged in user's ID and role between requests
builder.Services.AddSession(options =>
{
    options.Cookie.Name = SessionKeys.CookieName;

    //javascript cant read the cookie
    options.Cookie.HttpOnly = true;

    //cookie only gets sent over https
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    //the session cookie is needed for the API to work so its always allowed
    options.Cookie.IsEssential = true;

    //logs the user out after 30 minutes of doing nothing
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

//swagger ui is only switched on while developing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//redirects any http requests to https
app.UseHttpsRedirection();

//loads the session for each request, has to come before the controllers are mapped
app.UseSession();

//maps each controller route to its endpoint
app.MapControllers();

app.Run();