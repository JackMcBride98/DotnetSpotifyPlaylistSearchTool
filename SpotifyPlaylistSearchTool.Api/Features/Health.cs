namespace SpotifyPlaylistSearchTool.Api.Features;

public class Health
{
    public record HealthResponse(string Status, DateTimeOffset Timestamp);

    public class Endpoint : EndpointWithoutRequest<HealthResponse>
    {
        public override void Configure()
        {
            Get("/health");
            AllowAnonymous();
        }

        public override async Task<HealthResponse> ExecuteAsync(CancellationToken ct)
        {
            var response = new HealthResponse("Healthy", DateTimeOffset.UtcNow);

            return response;
        }
    }
}
