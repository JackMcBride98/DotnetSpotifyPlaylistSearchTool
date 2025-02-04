using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();
builder.Services.AddDbContextPool<DataContext>(options => options.UseNpgsql(builder.Configuration.GetSection("Database:ConnectionString").Value));
builder.Services.AddFastEndpoints();
builder.Services.AddScoped<ISyncSpotifyPlaylistService, SyncSpotifyPlaylistService>();

// builder.Services.AddSpaStaticFiles(options => { options.RootPath = "client/dist"; }); do this if not development

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
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
}

app.Run();
