namespace Arbeidstilsynet.MeldingerReceiver.App;

/// <summary>
/// Lets container healthchecks probe the app via <c>dotnet MeldingerReceiver.App.dll --healthcheck</c>,
/// since the distroless runtime image has no shell or curl.
/// </summary>
internal static class HealthCheckProbe
{
    public const string Argument = "--healthcheck";

    public static async Task<int> Run()
    {
        var port = Environment
            .GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")
            ?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?? "8080";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync($"http://localhost:{port}/healthz/ready");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
