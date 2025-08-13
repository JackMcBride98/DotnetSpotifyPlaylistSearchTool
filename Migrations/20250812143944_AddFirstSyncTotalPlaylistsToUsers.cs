using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotnetSpotifyPlaylistSearchTool.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstSyncTotalPlaylistsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirstSyncTotalPlaylists",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSyncTotalPlaylists",
                table: "Users");
        }
    }
}
