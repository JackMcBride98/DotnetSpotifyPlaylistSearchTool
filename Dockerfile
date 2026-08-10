FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY ./publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "SpotifyPlaylistSearchTool.Api.dll"]