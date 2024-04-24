using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MinimalAPIsParaYoutube.Contextos;
using MinimalAPIsParaYoutube.Entidades;

var builder = WebApplication.CreateBuilder(args);

// Servicios

builder.Services.AddDbContext<ApplicationDbContext>(opciones => 
    opciones.UseSqlServer("name=defaultConnection"));

var app = builder.Build();

// Middleware

app.UseHttpsRedirection();

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
});

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

app.MapPost("/personas", async (Persona persona, ApplicationDbContext context) =>
{
    context.Add(persona);
    await context.SaveChangesAsync();
    return TypedResults.CreatedAtRoute(persona, "ObtenerPersona", new { id = persona.Id });
});

app.MapPut("/personas/{id:int}", async Task<Results<BadRequest<string>, NotFound, NoContent>> 
    (int id, Persona persona,  ApplicationDbContext context) =>
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
    return TypedResults.NoContent();
});

app.MapDelete("/personas/{id:int}", async Task<Results<NotFound, NoContent>> 
    (int id, ApplicationDbContext context) =>
{
    var registrosBorrados = await context.Personas.Where(p => p.Id == id).ExecuteDeleteAsync();

    if (registrosBorrados == 0)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.NoContent();

});

var mensaje = builder.Configuration.GetValue<string>("mensaje");
app.MapGet("/mensaje", () => mensaje);

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
