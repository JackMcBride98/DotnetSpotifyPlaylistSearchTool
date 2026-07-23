using System.Reflection;
using FastEndpoints.OpenApi;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

var builder = WebApplication.CreateBuilder();

var dbSection = builder.Configuration.GetRequiredSection("Database");
var connectionString = dbSection["ConnectionString"];

var isDocumentGeneration =
    Assembly
        .GetEntryAssembly()
        ?.GetName()
        .Name?.Contains("GetDocument", StringComparison.OrdinalIgnoreCase)
    ?? false;

if (!isDocumentGeneration && string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("Database connection string is missing");
}

builder.Services.AddDbContextPool<DataContext>(options =>
{
    options.UseNpgsql(connectionString, x => x.UseNodaTime());
});
builder
    .Services.AddFastEndpoints()
    .OpenApiDocument(options =>
    {
        options.DocumentName = "v1";
        options.Title = "GetDocument.Insider API";
        options.Version = "v1.0.0";
        options.ShortSchemaNames = true;
    });

var optionsBuilder = builder
    .Services.AddOptions<SpotifyOptions>()
    .Bind(builder.Configuration.GetSection(SpotifyOptions.Position))
    .ValidateDataAnnotations();

if (!isDocumentGeneration)
{
    optionsBuilder.ValidateOnStart();
}

builder.Services.AddScoped<ISyncSpotifyPlaylistService, SyncSpotifyPlaylistService>();
builder.Services.AddScoped<ISpotifyAuthService, SpotifyAuthService>();
builder.Services.AddSingleton<ISpotifyClientFactory, SpotifyClientFactory>();

// builder.Services.AddSpaStaticFiles(options => { options.RootPath = "client/dist"; }); do this if not development

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseDefaultExceptionHandler();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Endpoints.NameGenerator = ctx =>
    {
        var declaringType = ctx.EndpointType.DeclaringType;

        var name = declaringType != null ? declaringType.Name : ctx.EndpointType.Name;

        return name.EndsWith("Endpoint") ? name[..^8] : name;
    };
    c.Errors.UseProblemDetails(x =>
    {
        x.AllowDuplicateErrors = true; //allows duplicate errors for the same error name
        x.IndicateErrorCode = true; //serializes the fluentvalidation error code
        x.IndicateErrorSeverity = true; //serializes the fluentvalidation error severity
        x.TypeValue = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";
        x.TitleValue = "One or more validation errors occurred.";
        x.TitleTransformer = pd =>
            pd.Status switch
            {
                400 => "Validation Error",
                404 => "Not Found",
                _ => "One or more errors occurred!",
            };
    });
});

// `UseEndpoints` terminates the request pipeline if a match was found. It's usually added implicitly by .NET but we
// need to add it explicitly because otherwise it would wrap everything, including the logic below to proxy to the
// Vite dev server in development. If we don't put it here, every request will fall through to the proxying logic,
// so in particular API calls etc. will not get handled correctly.
// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-6.0
app.UseEndpoints(_ => { });

if (builder.Environment.IsDevelopment())
{
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("http://localhost:3000"));
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.AddDocuments("v1");
        o.OperationTitleSource = OperationTitleSource.Path;
    });
}

app.Run();
