using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Meiyounaise.Modules
{
    public class ArtistResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mbid_id")]
        public Guid MbidId { get; set; }

        [JsonProperty("artistbackground")]
        public List<Artistbackground> Artistbackground { get; set; }

        [JsonProperty("artistthumb")]
        public List<Artistbackground> Artistthumb { get; set; }

        [JsonProperty("musiclogo")]
        public List<Artistbackground> Musiclogo { get; set; }

        [JsonProperty("hdmusiclogo")]
        public List<Artistbackground> Hdmusiclogo { get; set; }

        [JsonProperty("albums")]
        public Albums Albums { get; set; }

        [JsonProperty("musicbanner")]
        public List<Artistbackground> Musicbanner { get; set; }
    }

    public class Albums
    {
        [JsonProperty("2187d248-1a3b-35d0-a4ec-bead586ff547")]
        public The2187D2481A3B35D0A4EcBead586Ff547 The2187D2481A3B35D0A4EcBead586Ff547 { get; set; }

        [JsonProperty("902331d8-67aa-3b3c-bb2a-786d6a66c823")]
        public The2187D2481A3B35D0A4EcBead586Ff547 The902331D867Aa3B3CBb2A786D6A66C823 { get; set; }

        [JsonProperty("ac7dd865-3c32-3bb9-9241-a4c63d940f67")]
        public The8866D30729D332D58B1B16D8C10A85B5 Ac7Dd8653C323Bb99241A4C63D940F67 { get; set; }

        [JsonProperty("eab92048-3e05-3e0f-bc16-0780f5481b83")]
        public The2187D2481A3B35D0A4EcBead586Ff547 Eab920483E053E0FBc160780F5481B83 { get; set; }

        [JsonProperty("a35bcaf6-8e4a-3087-9b3b-d1295a2d4dbb")]
        public The2187D2481A3B35D0A4EcBead586Ff547 A35Bcaf68E4A30879B3Bd1295A2D4Dbb { get; set; }

        [JsonProperty("bf18a287-fdec-3e65-b9f2-460b82790a16")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 Bf18A287Fdec3E65B9F2460B82790A16 { get; set; }

        [JsonProperty("e1c522d0-2746-3845-9b1c-975632f496bc")]
        public The2187D2481A3B35D0A4EcBead586Ff547 E1C522D0274638459B1C975632F496Bc { get; set; }

        [JsonProperty("e7e5eaa6-baf3-3539-b163-759431597f3a")]
        public The2187D2481A3B35D0A4EcBead586Ff547 E7E5Eaa6Baf33539B163759431597F3A { get; set; }

        [JsonProperty("8866d307-29d3-32d5-8b1b-16d8c10a85b5")]
        public The8866D30729D332D58B1B16D8C10A85B5 The8866D30729D332D58B1B16D8C10A85B5 { get; set; }

        [JsonProperty("9ba659df-5814-32f6-b95f-02b738698e7c")]
        public The2187D2481A3B35D0A4EcBead586Ff547 The9Ba659Df581432F6B95F02B738698E7C { get; set; }

        [JsonProperty("a23c8be0-4aa9-3f6c-b1bc-af543a1da73d")]
        public The8866D30729D332D58B1B16D8C10A85B5 A23C8Be04Aa93F6Cb1BcAf543A1Da73D { get; set; }

        [JsonProperty("b1f3d25e-80ba-47f6-b0b8-7310e0dfe3e2")]
        public The8866D30729D332D58B1B16D8C10A85B5 B1F3D25E80Ba47F6B0B87310E0Dfe3E2 { get; set; }

        [JsonProperty("cd557bc4-9978-36ef-8c4c-9cb06099323a")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 Cd557Bc4997836Ef8C4C9Cb06099323A { get; set; }

        [JsonProperty("b4818927-0bdc-46ed-b8fb-99effc41c4d4")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 B48189270Bdc46EdB8Fb99Effc41C4D4 { get; set; }

        [JsonProperty("8d1d5cc0-f46d-473b-b422-1fba523aaf12")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 The8D1D5Cc0F46D473Bb4221Fba523Aaf12 { get; set; }

        [JsonProperty("b3e3be7f-334b-3383-ad9e-aa90f5d9e30c")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 B3E3Be7F334B3383Ad9EAa90F5D9E30C { get; set; }

        [JsonProperty("3babe662-f12a-3023-adcf-2e3afdfd64e9")]
        public The3Babe662F12A3023Adcf2E3Afdfd64E9 The3Babe662F12A3023Adcf2E3Afdfd64E9 { get; set; }
    }

    public class The8866D30729D332D58B1B16D8C10A85B5
    {
        [JsonProperty("cdart")]
        public List<Cdart> Cdart { get; set; }
    }

    public class Cdart
    {
        [JsonProperty("id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Id { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("likes")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Likes { get; set; }

        [JsonProperty("disc")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Disc { get; set; }

        [JsonProperty("size")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Size { get; set; }
    }

    public class The2187D2481A3B35D0A4EcBead586Ff547
    {
        [JsonProperty("albumcover")]
        public List<Artistbackground> Albumcover { get; set; }

        [JsonProperty("cdart")]
        public List<Cdart> Cdart { get; set; }
    }

    public class Artistbackground
    {
        [JsonProperty("id")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Id { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("likes")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Likes { get; set; }
    }

    public class The3Babe662F12A3023Adcf2E3Afdfd64E9
    {
        [JsonProperty("albumcover")]
        public List<Artistbackground> Albumcover { get; set; }
    }
}
