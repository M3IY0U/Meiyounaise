using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using GoogleTranslateFreeApi;
using Meiyounaise.DB;
using WanaKanaNet;

namespace Meiyounaise.Modules
{
    public class TranslationModule : BaseCommandModule
    {
        private async Task Trogi(CommandContext ctx)
            => await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                .WithAuthor("So you just used the translate command")
                .WithTitle("Let me tell you why it is currently disabled")
                .WithDescription("**1.** I do not know what broke and how to fix it\n" +
                                 "**2.** I currently do not have time to fix it\n" +
                                 "**3.** I currently do not feel like fixing it\n" +
                                 "**4.** Just fucking use NotSoBot")
                .WithFooter("Contrary to popular belief, pinging me about this issue does not accelerate the process")
                .Build());

        //TRANSLATE TO DE
        [Command("de")]
        [Description("Translates your message to german.")]
        public async Task German(CommandContext ctx, [RemainingText] string text)
        {
            await Trogi(ctx);
            return;
            text = await Utilities.CheckInput(text, ctx);

            var translation = await GTranslate(text, Language.German.ISO639);
            await ctx.RespondAsync(translation);
        }

        //TRANSLATE TO EN
        [Command("en")]
        [Description("Translates your message to english.")]
        public async Task English(CommandContext ctx, [RemainingText] string text)
        {
            await Trogi(ctx);
            return;
            text = await Utilities.CheckInput(text, ctx);

            var translation = await GTranslate(text, Language.English.ISO639);
            await ctx.RespondAsync(translation);
        }

        //TRANSLATE TO ANY LANGUAGE
        [Command("translate")]
        [Description(
            "Translates your text into the desired language. If you enter \"codes\" instead of a code the bot will dm you all available codes.")]
        public async Task Translate(CommandContext ctx, string langcode, [RemainingText] string text)
        {
            await Trogi(ctx);
            return;
            if (langcode == "codes")
            {
                await SendLanguageCodes(ctx);
                return;
            }

            text = await Utilities.CheckInput(text, ctx);

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

        #region Kana

        [Command("romaji")]
        [Description("Converts japanese characters to romaji.")]
        public async Task ToRomanji(CommandContext ctx, [RemainingText] string text = "")
        {
            text = await Utilities.CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToRomaji(text));
        }

        [Command("hiragana"), Aliases("hira")]
        [Description("Converts *doesn't translate* text to hiragana.")]
        public async Task ToHiragana(CommandContext ctx, [RemainingText] string text)
        {
            text = await Utilities.CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToHiragana(text));
        }

        [Command("katakana"), Aliases("kata")]
        [Description("Converts *doesn't translate* text to katakana.")]
        public async Task ToKatakana(CommandContext ctx, [RemainingText] string text)
        {
            text = await Utilities.CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToKatakana(text));
        }

        #endregion

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