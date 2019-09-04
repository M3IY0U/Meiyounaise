using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Meiyounaise.DB;

namespace Meiyounaise.Modules
{
    public class TranslationModule : BaseCommandModule
    {
        private static readonly GoogleCredential Credential =
            GoogleCredential.FromJson(Utilities.GetKey("googleTranslate"));

        //TRANSLATE TO DE
        [Command("de")]
        [Description("Translates your message to german.")]
        public async Task German(CommandContext ctx, [RemainingText] string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                await LastGerman(ctx);
                return;
            }

            await ctx.RespondAsync(GTranslate(text, LanguageCodes.German));
        }

        private static async Task LastGerman(CommandContext ctx)
        {
            var message = await ctx.Channel.GetMessagesAsync(2);
            await ctx.RespondAsync(GTranslate(message.Last().Content, LanguageCodes.German));
        }

        //TRANSLATE TO EN
        [Command("en")]
        [Description("Translates your message to english.")]
        public async Task English(CommandContext ctx, [RemainingText] string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                await LastEnglish(ctx);
                return;
            }

            await ctx.RespondAsync(GTranslate(text, LanguageCodes.English));
        }

        private static async Task LastEnglish(CommandContext ctx)
        {
            var message = await ctx.Channel.GetMessagesAsync(2);
            await ctx.RespondAsync(GTranslate(message.Last().Content, LanguageCodes.English));
        }

        //TRANSLATE TO ANY LANGUAGE
        [Command("translate")]
        [Description("Translates your text into the desired language. If you enter \"codes\" instead of a code the bot will dm you all available codes.")]
        public async Task Translate(CommandContext ctx, string langcode, [RemainingText] string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                await LastTranslate(ctx, langcode);
                return;
            }

            try
            {
                await ctx.RespondAsync(GTranslate(text, langcode));
            }
            catch (Exception)
            {
                throw new Exception("Couldn't translate, probably because you used the wrong language code.\n" +
                                    $"You can make the bot send you all codes by using `{Guilds.GetGuild(ctx.Guild).Prefix}translate codes`");
            }
        }

        private static async Task LastTranslate(CommandContext ctx, string langcode)
        {
            if (langcode == "codes")
            {
                var embed = new DiscordEmbedBuilder();
                var embed2 = new DiscordEmbedBuilder();
                var embed3 = new DiscordEmbedBuilder();
                var embed4 = new DiscordEmbedBuilder();
                var embed5 = new DiscordEmbedBuilder();
                var client = TranslationClient.Create(Credential);
                int i = 0;
                foreach (var language in client.ListLanguages(LanguageCodes.English))
                {
                    if (i < 25)
                    {
                        embed.AddField(language.Name, language.Code, true);
                        i++;
                    }
                    else if (i < 50)
                    {
                        embed2.AddField(language.Name, language.Code, true);
                        i++;
                    }
                    else if (i < 75)
                    {
                        embed3.AddField(language.Name, language.Code, true);
                        i++;
                    }
                    else if (i < 100)
                    {
                        embed4.AddField(language.Name, language.Code, true);
                        i++;
                    }
                    else if (i < 125)
                    {
                        embed5.AddField(language.Name, language.Code, true);
                        i++;
                    }
                }
                try
                {
                    var dm = await ctx.Member.CreateDmChannelAsync();
                    await dm.SendMessageAsync("", false, embed.Build());
                    await dm.SendMessageAsync("", false, embed2.Build());
                    await dm.SendMessageAsync("", false, embed3.Build());
                    await dm.SendMessageAsync("", false, embed4.Build());
                    await dm.SendMessageAsync("", false, embed5.Build());
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.Message);
                }
                return;
            }
            var message = await ctx.Channel.GetMessagesAsync(2);
            try
            {
                await ctx.RespondAsync(GTranslate(message.Last().Content, langcode));
            }
            catch (Exception)
            {
                throw new Exception("Couldn't translate, probably because you used the wrong language code.\n" +
                                    $"You can make the bot send you all codes by using `{Guilds.GetGuild(ctx.Guild).Prefix}translate codes`");
            }
        }
        
        private static string GTranslate(string text, string lang)
        {
            var client = TranslationClient.Create(Credential);
            return client.TranslateText(text, lang).TranslatedText;
        }
    }
}