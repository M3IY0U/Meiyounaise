using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using GoogleTranslateFreeApi;
using Meiyounaise.DB;

namespace Meiyounaise.Modules
{
    public class TranslationModule : BaseCommandModule
    {
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

            var translation = await GTranslate(text, Language.German.ISO639);
            await ctx.RespondAsync(translation);
        }

        private static async Task LastGerman(CommandContext ctx)
        {
            var message = await ctx.Channel.GetMessagesAsync(2);
            var translation = await GTranslate(message.Last().Content, Language.German.ISO639);
            await ctx.RespondAsync(translation);
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

            var translation = await GTranslate(text, Language.English.ISO639);
            await ctx.RespondAsync(translation);
        }

        private static async Task LastEnglish(CommandContext ctx)
        {
            var message = await ctx.Channel.GetMessagesAsync(2);
            var translation = await GTranslate(message.Last().Content, Language.English.ISO639);
            await ctx.RespondAsync(translation);
        }

        //TRANSLATE TO ANY LANGUAGE
        [Command("translate")]
        [Description(
            "Translates your text into the desired language. If you enter \"codes\" instead of a code the bot will dm you all available codes.")]
        public async Task Translate(CommandContext ctx, string langcode, [RemainingText] string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                await LastTranslate(ctx, langcode);
                return;
            }

            try
            {
                var translation = await GTranslate(text, langcode);
                await ctx.RespondAsync(translation);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Translating failed!"))
                    throw;
                throw new Exception("Couldn't translate, probably because you used the wrong language code.\n" +
                                    $"You can make the bot send you all codes by using `{Guilds.GetGuild(ctx.Guild).Prefix}translate codes`");
            }
        }

        private static async Task LastTranslate(CommandContext ctx, string langcode)
        {
            if (langcode == "codes")
            {
                await SendLanguageCodes(ctx);
                return;
            }

            var message = await ctx.Channel.GetMessagesAsync(2);
            try
            {
                var translation = await GTranslate(message.Last().Content, langcode);
                await ctx.RespondAsync(translation);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Translating failed!"))
                    throw;
                throw new Exception("Couldn't translate, probably because you used the wrong language code.\n" +
                                    $"You can make the bot send you all codes by using `{Guilds.GetGuild(ctx.Guild).Prefix}translate codes`");
            }
        }

        private static async Task SendLanguageCodes(CommandContext ctx)
        {
            var embeds = new List<DiscordEmbed>();
            var embed = new DiscordEmbedBuilder();
            var i = 0;

            foreach (var language in GoogleTranslator.LanguagesSupported)
            {
                if (++i >= 25)
                {
                    embeds.Add(embed.Build());
                    embed = new DiscordEmbedBuilder();
                    i = 0;
                }

                i++;
                embed.AddField(language.FullName, language.ISO639, true);
            }

            try
            {
                var dm = await ctx.Member.CreateDmChannelAsync();
                    
                foreach (var embedToSend in embeds)
                    await dm.SendMessageAsync("", false, embedToSend);
                    
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
            }
        }
        
        private static async Task<string> GTranslate(string text, string lang)
        {
            var client = new GoogleTranslator();
            try
            {
                var result =
                    await client.TranslateLiteAsync(text, Language.Auto, GoogleTranslator.GetLanguageByISO(lang));
                return result.MergedTranslation;
            }
            catch (Exception)
            {
                throw new Exception("Translating failed!\n[» View on Google Translate]" +
                                    $"(https://translate.google.com/#view=home&op=translate&sl=auto&tl={lang}&text={text})"
                                        .Replace(" ", "%20"));
            }
        }
    }
}