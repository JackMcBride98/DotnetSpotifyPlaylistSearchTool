using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;
using Void = FastEndpoints.Void;

namespace SpotifyPlaylistSearchTool.Api.Features.Auth;

public static class Callback
{
    public record Request(string Code);

    public class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Authorization code is required.");
        }
    }

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Get("/callback");
            AllowAnonymous();
        }

        public override async Task<Void> HandleAsync(Request req, CancellationToken ct)
        {
            try
            {
                await spotifyAuthService.HandleCallbackAndUpsertUserAsync(
                    req.Code,
                    HttpContext,
                    ct
                );

                return await Send.ResultAsync(Results.Redirect("/profile"));
            }
            catch (APIException ex)
            {
                AddError(r => r.Code, $"Spotify authentication failed: {ex.Message}");
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return new Void();
            }
        }
    }
}
