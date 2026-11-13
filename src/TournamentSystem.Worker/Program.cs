using TournamentSystem.Worker;
using TournamentSystem.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";

builder.Services.AddHttpClient<TournamentApiClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();