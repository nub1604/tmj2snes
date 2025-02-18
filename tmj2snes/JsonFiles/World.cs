using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tmj2snes.JsonFiles
{
    // World myDeserializedClass = JsonSerializer.Deserialize<World>(myJsonResponse);
    public class Map
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class World : iConverter<World>
    {

        public static World? GetWorld(string input) => JsonSerializer.Deserialize<World>(input);

        public World? Convert(string input)
        {
            return GetWorld(input);
        }

        [JsonPropertyName("maps")]
        public Map[] Maps { get; set; } = [];

        [JsonPropertyName("onlyShowAdjacentMaps")]
        public bool OnlyShowAdjacentMaps { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }


}
