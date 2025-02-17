using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using tmj2snes.JsonFiles;
using System.Text.Json.Serialization;
using NLua;

namespace tmj2snes.CustomConverter
{
    public class ConverterExtensions
    {

        public List<JsonExtension> Converters { get; set; } = [];

        public void LoadAll()
        {
            Converters = [.. ExtensionLoader.LoadExtensions()];
        }

        public void Run(string basepath, string mapID, TileMap map, World world)
        {
            foreach (var c in Converters)
            {

                ExecuteLuaScript(c,basepath, mapID, map,world);
                //ConvertLayer(basepath, mapID, map, world, c);
            }
        }

        private static void ConvertLayer(string basepath, string mapID, TileMap map, World? world, JsonExtension c)
        {
            var layer = map.Layers.FirstOrDefault(x => x.Type == c.LayerType && x.Name.StartsWith(c.LayerName));
            if (layer == null) return;
            var triggerObjects = layer.Objects.Where(x => x.Properties.Any(y => y.Name == c.TriggerName)).ToArray();
            foreach (var to in triggerObjects)
            {
                var property = to.Properties.FirstOrDefault(y => y.Name == c.TriggerName);
                var value = property!.Value as string;
                var pl = value.Split(',');
                var il = c.InputLayout.Split(',');

                if (pl.Length != il.Length)
                {
                    Console.WriteLine($"Warning InputLayout in {to.Name},  error source: map {mapID}, {c.TriggerName}");
                }
                HandleInputPattern(basepath, mapID, world, c, to, pl, il);
            }
        }

        private static void HandleInputPattern(string basepath, string mapID, World? world, JsonExtension c, TiledObject to, string[] pl, string[] il)
        {
            TileMap? TargetMap = null;
            TiledObject? TargetObject = null;
            for (int i = 0; i < il.Length; i++)
            {
                switch (il[i])
                {
                    case "tm":
                        LoadTargetMap(basepath, mapID, world, c, pl, ref TargetMap, i);

                        break;

                    case "d":
                        if (TargetMap == null)
                        {
                            Console.WriteLine($"Warning parameter \"d\" needs a valid tilemap loaded in {to.Name},  error source: map {mapID}, {c.TriggerName}");
                            return;
                        }
                        if (TargetObject == null)
                        {
                            Console.WriteLine($"Warning parameter \"d\" needs a valid targetobject,  error source: map {mapID}, {c.TriggerName}");
                            return;
                        }

                        switch (pl[i])
                        {
                           

                            case "r":

                                break;

                            case "u":
                                break;

                            case "d":
                                break;

                            case "l":
                                break;

                            case "tr":
                                break;
                        }
                        break;
                    case "tr":
                        if (TargetMap == null)
                        {
                            Console.WriteLine($"Warning \"tr\" needs a valid tilemap loaded in {to.Name},  error source: map {mapID}, {c.TriggerName}");
                            return;
                        }
                        var layer = TargetMap.Layers.FirstOrDefault(x => x.Type == c.LayerType && x.Name.StartsWith(c.LayerName));
                        if (layer == null)
                        {
                            Console.WriteLine($"Warning \"tr\" layer {c.LayerName} not found, error source: map {mapID}, {c.TriggerName}");
                            return;
                        }
                        TargetObject = layer.Objects.FirstOrDefault(x => x.Type == pl[i]);
                        if (TargetObject == null)
                        {
                            Console.WriteLine($"Warning \"tr\" object {pl[i]} not found, error source: map {mapID}, {c.TriggerName}");
                            return;
                        }


                      
                        break;
                    default:
                        break;
                }
            }
            //run script here
        }
        static void ExecuteLuaScript(JsonExtension extension, string basepath, string mapId,  TileMap map, World? world)
        {
            try
            {
                using (Lua lua = new Lua())
                {
#if DEBUG
                    lua["debugLua"] = true;
#endif
                    lua["basepath"] = basepath;
                    lua["mapId"] = mapId;
                    lua["map"] = map;
                    if (world != null)
                    {
                        lua["world"] = world;
                    }
                    // Redirect Lua print() to C# Console.WriteLine
                    lua["print"] = (Action<object>)((msg) => Console.WriteLine("[Lua] " + msg));
                    object[] results = lua.DoString(extension.LuaScript);
                    var res = results.Length > 0 ? results[0]?.ToString() ?? string.Empty : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing {ex.Message}");

            }
        }
        private static void LoadTargetMap(string basepath, string mapID, World? world, JsonExtension c, string[] pl, ref TileMap? TargetMap, int i)
        {
            var m = world?.Maps.FirstOrDefault(x => x.FileName.StartsWith(pl[i]));
            if (m == null)
            {
                Console.WriteLine($"Warning parameter  \"tf\" needs a valid map path {pl[i]},  error source: map {mapID}, {c.TriggerName}");
                return;
            }
            var fullpath = Path.Combine(basepath, m.FileName);
            TargetMap = FileHandler.LoadFile<TileMap>(fullpath);
        }
    }

    public class JsonExtension
    {
        public string LayerName { get; set; } = "";
        public string LayerType { get; set; } = "";
        public string TriggerName { get; set; } = "";
        public string InputLayout { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string Script { get; set; } = "";




        [JsonIgnore]
        public StringBuilder Output { get; set; } = new(); 
        [JsonIgnore]
        public string LuaScript { get; set; } = "";

       

    }

    internal static class ExtensionLoader
    {
        internal static IEnumerable<JsonExtension> LoadExtensions()
        {
            foreach (var file in Directory.GetFiles("extensions", "*.json"))
            {
                var converter = JsonSerializer.Deserialize<JsonExtension>(File.ReadAllText(file));
                if (converter == null)
                {
                    Console.WriteLine("Failed to load extension: " + file);
                    continue;
                }
               var script = Path.Combine("extensions", converter.Script);
                if (File.Exists(script))
                {
                    converter.LuaScript = File.ReadAllText(script);

                }
                else
                {
                    Console.WriteLine($"Warning script {script} not exist");
                }
                    yield return converter;
            }
        }

    }
        
    }