using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using GoogleTranslateFreeApi;
using Meiyounaise.DB;
using WanaKanaNet;

namespace Meiyounaise.Modules
{
    public class TranslationModule : BaseCommandModule
    {
        //TRANSLATE TO DE
        [Command("de")]
        [Description("Translates your message to german.")]
        public async Task German(CommandContext ctx, [RemainingText] string text)
        {
            text = await CheckInput(text, ctx);

            var translation = await GTranslate(text, Language.German.ISO639);
            await ctx.RespondAsync(translation);
        }

        //TRANSLATE TO EN
        [Command("en")]
        [Description("Translates your message to english.")]
        public async Task English(CommandContext ctx, [RemainingText] string text)
        {
            text = await CheckInput(text, ctx);

            var translation = await GTranslate(text, Language.English.ISO639);
            await ctx.RespondAsync(translation);
        }

        //TRANSLATE TO ANY LANGUAGE
        [Command("translate")]
        [Description(
            "Translates your text into the desired language. If you enter \"codes\" instead of a code the bot will dm you all available codes.")]
        public async Task Translate(CommandContext ctx, string langcode, [RemainingText] string text)
        {
            if (langcode == "codes")
            {
                await SendLanguageCodes(ctx);
                return;
            }

            text = await CheckInput(text, ctx);

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

        [Command("romaji")]
        [Description("Converts japanese characters to romaji.")]
        public async Task ToRomanji(CommandContext ctx, [RemainingText] string text = "")
        {
            text = await CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToRomaji(text));
        }

        [Command("hiragana"), Aliases("hira")]
        [Description("Converts *doesn't translate* text to hiragana.")]
        public async Task ToHiragana(CommandContext ctx, [RemainingText] string text)
        {
            text = await CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToHiragana(text));
        }

        [Command("katakana"), Aliases("kata")]
        [Description("Converts *doesn't translate* text to katakana.")]
        public async Task ToKatakana(CommandContext ctx, [RemainingText] string text)
        {
            text = await CheckInput(text, ctx);
            await ctx.RespondAsync(WanaKana.ToKatakana(text));
        }

        private static async Task<string> CheckInput(string input, CommandContext ctx)
        {
            if (!string.IsNullOrEmpty(input)) return input;
            var messages = await ctx.Channel.GetMessagesAsync(2);
            return messages.Last().Content;
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

        public static async Task TranslateWithReaction(MessageReactionAddEventArgs e)
        {
            var name = e.Emoji.GetDiscordName();
            var message = await e.Channel.GetMessageAsync(e.Message.Id);
            if (!name.Contains("flag_") || message.Reactions.Count > 1)
                return;
            try
            {
                var language = GoogleTranslator.GetLanguageByISO(name.Substring(name.LastIndexOf('_') + 1, 2));
                if (!GoogleTranslator.IsLanguageSupported(language))
                    return;
                var client = new GoogleTranslator();
                var result =
                    await client.TranslateLiteAsync(message.Content, Language.Auto, language);
                var eb = new DiscordEmbedBuilder()
                    .WithDescription($"[Original Message]({message.JumpLink})\n{result.MergedTranslation}")
                    .WithTitle("Translation Result")
                    .WithFooter(
                        $"Translated from {result.LanguageDetections.First().Language.FullName} to {result.TargetLanguage.FullName}");
                await e.Channel.SendMessageAsync(e.User.Mention, embed: eb.Build());
            }
            catch (Exception)
            {
                var member = await e.Guild.GetMemberAsync(e.User.Id);
                await member.SendMessageAsync(
                    $"{DiscordEmoji.FromGuildEmote(Bot.Client, 578527109891227649)} Whoops, translating your message via reaction failed");
            }
        }
    }
}