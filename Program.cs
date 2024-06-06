using HybridCacheDemo;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<WeatherService>();

//Caches
//In memory
builder.Services.AddMemoryCache();
//Distributed (i.e. Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
//HybridCache Distributed : you can disable InMemory or StackExchange service following what you want to use (code is compatible for both)
builder.Services.AddHybridCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherMemoryCached/{city}", async (string city, WeatherService weatherService) =>
    await weatherService.GetInMemoryCurentWeatherAsync(city) is WeatherResponse weatherResponse ? Results.Ok(weatherResponse) : Results.NotFound()
);

app.MapGet("/weatherIDistributedCached/{city}", async (string city, WeatherService weatherService) =>
    await weatherService.GetIDistributedCachedCurentWeatherAsync(city) is WeatherResponse weatherResponse ? Results.Ok(weatherResponse) : Results.NotFound()
);

app.MapGet("/weatherHybridCached/{city}", async (string city, WeatherService weatherService) =>
    await weatherService.GetHybridCachedCurentWeatherAsync(city) is WeatherResponse weatherResponse ? Results.Ok(weatherResponse) : Results.NotFound()
);

app.Run();
