using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tmj2snes.JsonFiles
{
   
    public class TiledColor
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("probability")]
        public int Probability { get; set; }

        [JsonPropertyName("tile")]
        public int Tile { get; set; }
    }

    public class Layer
    {
        [JsonPropertyName("data")]
        public List<object> Data { get; set; } = new();

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("opacity")]
        public int Opacity { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("draworder")]
        public string Draworder { get; set; } = "";

        [JsonPropertyName("objects")]
        public List<TiledObject> Objects { get; set; } = new ();

        [JsonPropertyName("tintcolor")]
        public string Tintcolor { get; set; } = "";
    }

    public class TiledObject
    {
        [JsonPropertyName("height")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Height { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("properties")]
        public List<Property> Properties { get; set; } = new();

        [JsonPropertyName("rotation")]
        public int Rotation { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class Property
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    public class TileMap: iConverter<TileMap>
    {

        public static TileMap? GetTileMap(string input) => JsonSerializer.Deserialize<TileMap>(input, new JsonSerializerOptions { Converters = { new AutoNumberToIntConverter() } });


        public TileMap? Convert(string input)
        {
            return GetTileMap(input);
        }

        [JsonPropertyName("compressionlevel")]
        public int Compressionlevel { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("infinite")]
        public bool Infinite { get; set; }

        [JsonPropertyName("layers")]
        public List<Layer> Layers { get; set; } = new();

        [JsonPropertyName("nextlayerid")]
        public int Nextlayerid { get; set; }

        [JsonPropertyName("nextobjectid")]
        public int Nextobjectid { get; set; }

        [JsonPropertyName("orientation")]
        public string Orientation { get; set; } = "";

        [JsonPropertyName("renderorder")]
        public string Renderorder { get; set; } = "";

        [JsonPropertyName("tiledversion")]
        public string Tiledversion { get; set; } = "";

        [JsonPropertyName("tileheight")]
        public int Tileheight { get; set; }

        [JsonPropertyName("tilesets")]
        public List<Tileset> Tilesets { get; set; } = new();

        [JsonPropertyName("tilewidth")]
        public int Tilewidth { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class Tile
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("properties")]
        public List<Property> Properties { get; set; } = new();
    }

    public class Tileset : iConverter<Tileset>
    {
        public static Tileset? GetTileset(string input) => JsonSerializer.Deserialize<Tileset>(input);
        public Tileset? Convert(string input)
        {
            return GetTileset(input);
        }

        [JsonPropertyName("columns")]
        public int Columns { get; set; }

        [JsonPropertyName("firstgid")]
        public int Firstgid { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; } = "";

        [JsonPropertyName("imageheight")]
        public int Imageheight { get; set; }

        [JsonPropertyName("imagewidth")]
        public int Imagewidth { get; set; }

        [JsonPropertyName("margin")]
        public int Margin { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("spacing")]
        public int Spacing { get; set; }

        [JsonPropertyName("tilecount")]
        public int Tilecount { get; set; }

        [JsonPropertyName("tileheight")]
        public int Tileheight { get; set; }

        [JsonPropertyName("tiles")]
        public List<Tile> Tiles { get; set; } = new();

        [JsonPropertyName("tilewidth")]
        public int Tilewidth { get; set; }

        [JsonPropertyName("wangsets")]
        public List<Wangset> Wangsets { get; set; } = new();

        [JsonPropertyName("source")]
        public string Source { get; set; } = "";
    }

    public class Wangset
    {
        [JsonPropertyName("colors")]
        public List<TiledColor> Colors { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("tile")]
        public int Tile { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("wangtiles")]
        public List<Wangtile> Wangtiles { get; set; } = new();
    }

    public class Wangtile
    {
        [JsonPropertyName("tileid")]
        public int Tileid { get; set; }

        [JsonPropertyName("wangid")]
        public List<int> Wangid { get; set; }=new ();
    }

}
