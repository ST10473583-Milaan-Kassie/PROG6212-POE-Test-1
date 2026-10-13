var builder = WebApplication.CreateBuilder(args);

//registers the controllers so the api can find the endpoints
builder.Services.AddControllers();

//registers swagger so every endpoint can be viewed and tested in the browser
builder.Services.AddSwaggerGen();

var app = builder.Build();

//swagger ui is only switched on while developing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//redirects any http requests to https
app.UseHttpsRedirection();

//maps each controller route to its endpoint
app.MapControllers();

app.Run();