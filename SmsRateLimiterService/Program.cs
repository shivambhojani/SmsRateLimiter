using SmsRateLimiterService.Config;
using SmsRateLimiterService.Interface;
using SmsRateLimiterService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddControllers(); // <-- Add this line

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Redis Connection
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.Configure<RateLimiterSettings>(builder.Configuration.GetSection("RateLimiterSettings"));

// Register RateLimiterService as IRateLimiter
builder.Services.AddSingleton<IRateLimiter, RateLimiterService>();  

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.UseExceptionHandler("/api/error");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

