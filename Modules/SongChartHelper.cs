using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Meiyounaise.Modules
{
    public class SongResponse
    {
        [JsonProperty("toptracks")]
        public Toptracks Toptracks { get; set; }
    }

    public class Toptracks
    {
        [JsonProperty("@attr")]
        public ToptracksAttr Attr { get; set; }

        [JsonProperty("track")]
        public List<Track> Track { get; set; }
    }

    public class ToptracksAttr
    {
        [JsonProperty("page")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Page { get; set; }

        [JsonProperty("perPage")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PerPage { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("total")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Total { get; set; }

        [JsonProperty("totalPages")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TotalPages { get; set; }
    }

    public class Track
    {
        [JsonProperty("@attr")]
        public TrackAttr Attr { get; set; }

        [JsonProperty("duration")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Duration { get; set; }

        [JsonProperty("playcount")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long PlayCount { get; set; }

        [JsonProperty("artist")]
        public Artist Artist { get; set; }

        [JsonProperty("image")]
        public List<Image> Image { get; set; }

        [JsonProperty("streamable")]
        public Streamable Streamable { get; set; }

        [JsonProperty("mbid")]
        public string Mbid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public class Artist
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mbid")]
        public string Mbid { get; set; }
    }

    public class TrackAttr
    {
        [JsonProperty("rank")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Rank { get; set; }
    }

    public class Image
    {
        [JsonProperty("size")]
        public Size Size { get; set; }

        [JsonProperty("#text")]
        public Uri Text { get; set; }
    }

    public class Streamable
    {
        [JsonProperty("fulltrack")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Fulltrack { get; set; }

        [JsonProperty("#text")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Text { get; set; }
    }

    public enum Size { Extralarge, Large, Medium, Small }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                SizeConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    internal class SizeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Size) || t == typeof(Size?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "extralarge":
                    return Size.Extralarge;
                case "large":
                    return Size.Large;
                case "medium":
                    return Size.Medium;
                case "small":
                    return Size.Small;
            }
            throw new Exception("Cannot unmarshal type Size");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Size)untypedValue;
            switch (value)
            {
                case Size.Extralarge:
                    serializer.Serialize(writer, "extralarge");
                    return;
                case Size.Large:
                    serializer.Serialize(writer, "large");
                    return;
                case Size.Medium:
                    serializer.Serialize(writer, "medium");
                    return;
                case Size.Small:
                    serializer.Serialize(writer, "small");
                    return;
            }
            throw new Exception("Cannot marshal type Size");
        }

        public static readonly SizeConverter Singleton = new SizeConverter();
    }
}
