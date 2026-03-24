using BookingService.Api.Middleware;
using BookingService.Application.Abstractions;
using BookingService.Application.Bookings;
using BookingService.Infrastructure.DependencyInjection;
using BookingService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IBookingService, BookingService.Application.Bookings.BookingService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseIdempotencyMiddleware();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
