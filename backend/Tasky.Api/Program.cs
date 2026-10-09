using Tasky.Api.Data;
using Tasky.Api.Extensions;
using Tasky.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTaskyDatabase(builder.Configuration);
builder.Services.AddTaskyServices(builder.Configuration);
builder.Services.AddTaskyCors(builder.Configuration);

var app = builder.Build();

await DbInitializer.InitializeAsync(
    app.Services,
    app.Configuration,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer"));

// Order matters: the exception middleware must come first so it can catch everything below it.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(TaskyServiceExtensions.CorsPolicyName);
app.MapControllers();

app.Run();
