using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Meiyounaise.Modules
{
    public class ImageModule : BaseCommandModule
    {
        [Command("ir"), Aliases("imagerecognition", "whatsthis")]
        [Description(
            "Returns information about either an image attachment or an url to an image, also works for the previous message")]
        public async Task ImageRecognition(CommandContext ctx, string imageUrl = "")
        {
            if (imageUrl == "")
            {
                if (ctx.Message.Attachments.Count != 0)
                {
                    imageUrl = ctx.Message.Attachments.First().Url;
                }
                else
                {
                    var messages = await ctx.Channel.GetMessagesAsync(2);
                    imageUrl = messages.Last().Attachments.Count != 0
                        ? messages.Last().Attachments.First().Url
                        : messages.Last().Content;
                }
            }

            await Utilities.DownloadAsync(new Uri(imageUrl), "image.png");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Utilities.GetKey("recognition"));

            const string uri =
                "https://westeurope.api.cognitive.microsoft.com/vision/v2.0/analyze?visualFeatures=Categories,Description,Color";
            HttpResponseMessage response;

            var byteData = GetImageAsByteArray("image.png");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);
            }

            var contentString = await response.Content.ReadAsStringAsync();

            var responseObject = JsonConvert.DeserializeObject<RecognitionResponse>(contentString);

            var tags = responseObject.Description.Tags.Count == 0
                ? "None"
                : string.Join(", ", responseObject.Description.Tags);
            var categories = responseObject.Categories.Count == 0
                ? "None"
                : responseObject.Categories.Aggregate("",
                    (current, category) => current + $"\n{category.Name}; Score {Math.Round(category.Score, 2)}");
            var captions = responseObject.Description.Captions.Count == 0
                ? "None"
                : responseObject.Description.Captions.Aggregate("",
                    (current, desc) => current + $"\n{desc.Text}; Confidence {Math.Round(desc.Confidence, 2)}");

            var eb = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(responseObject.Color.AccentColor))
                .WithTitle("Image Analysis")
                .WithThumbnailUrl(imageUrl)
                .WithDescription(captions)
                .AddField("Categories",categories,true)
                .AddField("Tags", tags, true)
                .AddField("Colors",$"Foreground Color: {responseObject.Color.DominantColorForeground}\nBackground Color: {responseObject.Color.DominantColorBackground}\nAccent Color: {responseObject.Color.AccentColor}",
                    true)
                .AddField("Dimensions",
                    $"{responseObject.Metadata.Width} x {responseObject.Metadata.Height}, Format: {responseObject.Metadata.Format}",
                    true);

            await ctx.RespondAsync(embed: eb.Build());
        }
        
        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (var fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                var binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int) fileStream.Length);
            }
        }
    }
}