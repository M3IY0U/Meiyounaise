using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;

namespace Meiyounaise.Modules
{
    [Group("spotify"), Aliases("sp")]
    public class SpotifyModule : BaseCommandModule
    {
        private SpotifyWebAPI _spotify;

        private SpotifyWebAPI authSpotify()
        {
            var auth = new ClientCredentialsAuth
            {
                ClientId = Utilities.GetKey("spid"),
                ClientSecret = Utilities.GetKey("spsecret"),
                Scope = Scope.UserReadPrivate
            };
            var token = auth.DoAuth();
            var spotifyClient = new SpotifyWebAPI
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true
            };
            return spotifyClient;
        }
        
        [GroupCommand]
        public async Task SpotifyCommand(CommandContext ctx, [RemainingText] string input)
        {
            _spotify = authSpotify();
            var interactivity = ctx.Client.GetInteractivity();
            var result = await _spotify.SearchItemsAsync(input, SearchType.All);
            if (!result.Albums.Items.Any() && !result.Artists.Items.Any() && !result.Tracks.Items.Any())
            {
                await ctx.RespondAsync($"`{input}` was not found on Spotify!");
                return;
            }

            var msg = await ctx.RespondAsync(
                $"Found {result.Albums.Items.Count} albums, {result.Artists.Items.Count} artists and {result.Tracks.Items.Count} tracks!\n" +
                $"Please react with which one you want to see");
            var reactions = new List<string> {"💿", "👤", "🎵"};
            foreach (var r in reactions)
            {
                try
                {
                    await msg.CreateReactionAsync(DiscordEmoji.FromUnicode(r));
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.Message);
                }

                await Task.Delay(100);
            }

            var test = await interactivity.WaitForReactionAsync(x => reactions.Any(x.Emoji.Name.Contains), ctx.User);
            await msg.DeleteAsync();
            switch (test.Result.Emoji.Name)
            {
                case "💿":
                {
                    var pages = new List<Page>();
                    var albums = result.Albums.Items;
                    foreach (var album in albums)
                    {
                        pages.Add(new Page(album.ExternalUrls.First().Value));
                    }
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
                    break;
                }

                case "👤":
                {
                    var pages = new List<Page>();
                    var artists = result.Artists.Items;
                    foreach (var artist in artists)
                    {
                        pages.Add(new Page(artist.ExternalUrls.First().Value));
                    }

                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
                    break;
                }

                case "🎵":
                {
                    var pages = new List<Page>();
                    var tracks = result.Tracks.Items;
                    foreach (var track in tracks)
                    {
                        pages.Add(new Page (track.ExternUrls.First().Value));
                    }

                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
                    break;
                }

                default:
                    await ctx.RespondAsync("No matching reaction found!");
                    break;
            }
        }

        [Command("album")]
        public async Task SearchAlbum(CommandContext ctx, [RemainingText] string input)
        {
            _spotify = authSpotify();
            var result = await _spotify.SearchItemsAsync(input, SearchType.Album, 1);
            var response = result.Albums.Items.Count == 0
                ? $"No Album found with name `{input}`!"
                : result.Albums.Items.First().ExternalUrls.First().Value;
            await ctx.RespondAsync(response);
        }

        [Command("artist")]
        public async Task SearchArtist(CommandContext ctx, [RemainingText] string input)
        {
            _spotify = authSpotify();
            var result = await _spotify.SearchItemsAsync(input, SearchType.Artist, 1);
            var response = result.Artists.Items.Count == 0
                ? $"No Artist found with name `{input}`!"
                : result.Artists.Items.First().ExternalUrls.First().Value;
            await ctx.RespondAsync(response);
        }
    }
}