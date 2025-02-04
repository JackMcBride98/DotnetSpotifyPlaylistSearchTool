﻿// <auto-generated />
using DotnetSpotifyPlaylistSearchTool.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DotnetSpotifyPlaylistSearchTool.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Image", b =>
                {
                    b.Property<int>("ImageId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ImageId"));

                    b.Property<int>("Height")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("Width")
                        .HasColumnType("integer");

                    b.HasKey("ImageId");

                    b.ToTable("Image");
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Playlist", b =>
                {
                    b.Property<string>("PlaylistId")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int?>("ImageId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("OwnerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("SnapshotId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("PlaylistId");

                    b.HasIndex("ImageId");

                    b.ToTable("Playlists");
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Track", b =>
                {
                    b.Property<int>("TrackId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TrackId"));

                    b.Property<string>("ArtistName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("Index")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("PlaylistId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("TrackId");

                    b.HasIndex("PlaylistId");

                    b.ToTable("Tracks");
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.User", b =>
                {
                    b.Property<string>("UserId")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PlaylistUser", b =>
                {
                    b.Property<string>("PlaylistsPlaylistId")
                        .HasColumnType("character varying(100)");

                    b.Property<string>("UsersUserId")
                        .HasColumnType("character varying(100)");

                    b.HasKey("PlaylistsPlaylistId", "UsersUserId");

                    b.HasIndex("UsersUserId");

                    b.ToTable("PlaylistUser");
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Playlist", b =>
                {
                    b.HasOne("DotnetSpotifyPlaylistSearchTool.Database.Image", "Image")
                        .WithMany()
                        .HasForeignKey("ImageId");

                    b.Navigation("Image");
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Track", b =>
                {
                    b.HasOne("DotnetSpotifyPlaylistSearchTool.Database.Playlist", null)
                        .WithMany("Tracks")
                        .HasForeignKey("PlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PlaylistUser", b =>
                {
                    b.HasOne("DotnetSpotifyPlaylistSearchTool.Database.Playlist", null)
                        .WithMany()
                        .HasForeignKey("PlaylistsPlaylistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DotnetSpotifyPlaylistSearchTool.Database.User", null)
                        .WithMany()
                        .HasForeignKey("UsersUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DotnetSpotifyPlaylistSearchTool.Database.Playlist", b =>
                {
                    b.Navigation("Tracks");
                });
#pragma warning restore 612, 618
        }
    }
}
