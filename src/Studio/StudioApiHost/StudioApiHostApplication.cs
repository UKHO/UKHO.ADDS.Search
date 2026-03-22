using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio;
using UKHO.Search.Studio.Providers.FileShare.Injection;

namespace StudioApiHost
{
    public static class StudioApiHostApplication
    {
        public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
        {
            var builder = WebApplication.CreateBuilder(args);
            var studioShellOrigin = "http://localhost:3000";

            configureBuilder?.Invoke(builder);

            builder.Services.AddAuthorization();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("StudioShell", policy =>
                {
                    policy.WithOrigins(studioShellOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddOpenApi();
            builder.Services.AddFileShareProviderMetadata();
            builder.Services.AddFileShareStudioProvider();

            var app = builder.Build();

            app.Services.GetRequiredService<IStudioProviderRegistrationValidator>()
               .Validate();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("StudioShell");
            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/providers", (IProviderCatalog providerCatalog) =>
            {
                return TypedResults.Ok(providerCatalog.GetAllProviders());
            })
            .WithName("GetProviders");

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

            app.MapGet("/echo", () => TypedResults.Text("Hello from StudioApiHost echo."))
               .WithName("GetEcho");

            return app;
        }
    }
}
