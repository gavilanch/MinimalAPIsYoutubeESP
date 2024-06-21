using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MinimalAPIsParaYoutube.Contextos;
using MinimalAPIsParaYoutube.Entidades;

var builder = WebApplication.CreateBuilder(args);

// Servicios

builder.Services.AddDbContext<ApplicationDbContext>(opciones => 
    opciones.UseSqlServer("name=defaultConnection"));

//builder.Services.AddOutputCache();

builder.Services.AddStackExchangeRedisOutputCache(opciones =>
{
    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
});

var app = builder.Build();

// Middleware

app.UseHttpsRedirection();

app.UseOutputCache();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 10).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapGet("/personas", async (ApplicationDbContext context) =>
{
    var personas = await context.Personas.ToListAsync();
    return personas;
}).CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).Tag("personas-get"));

app.MapGet("/personas/{id:int}", async Task<Results<NotFound, Ok<Persona>>>
    (int id, ApplicationDbContext context) =>
{
    var persona = await context.Personas.FirstOrDefaultAsync(p => p.Id == id);

    if (persona is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(persona);
}).WithName("ObtenerPersona");

app.MapPost("/personas", async (Persona persona, ApplicationDbContext context, 
    IOutputCacheStore outputCacheStore) =>
{
    context.Add(persona);
    await context.SaveChangesAsync();
    await outputCacheStore.EvictByTagAsync("personas-get", default);
    return TypedResults.CreatedAtRoute(persona, "ObtenerPersona", new { id = persona.Id });
});

app.MapPut("/personas/{id:int}", async Task<Results<BadRequest<string>, NotFound, NoContent>> 
    (int id, Persona persona,  ApplicationDbContext context, 
    IOutputCacheStore outputCacheStore) =>
{
    if (id != persona.Id)
    {
        return TypedResults.BadRequest("Los Ids no coinciden");
    }

    var existe = await context.Personas.AnyAsync(p => p.Id == id);

    if (!existe)
    {
        return TypedResults.NotFound();
    }

    context.Update(persona);
    await context.SaveChangesAsync();
    await outputCacheStore.EvictByTagAsync("personas-get", default);
    return TypedResults.NoContent();
});

app.MapDelete("/personas/{id:int}", async Task<Results<NotFound, NoContent>> 
    (int id, ApplicationDbContext context, IOutputCacheStore outputCacheStore) =>
{
    var registrosBorrados = await context.Personas.Where(p => p.Id == id).ExecuteDeleteAsync();

    if (registrosBorrados == 0)
    {
        return TypedResults.NotFound();
    }

    await outputCacheStore.EvictByTagAsync("personas-get", default);
    return TypedResults.NoContent();

});

var mensaje = builder.Configuration.GetValue<string>("mensaje");
app.MapGet("/mensaje", () => mensaje);

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
