using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using tmj2snes.JsonFiles;
using System.Text.Json.Serialization;
using NLua;
using static System.Net.Mime.MediaTypeNames;

namespace tmj2snes.CustomConverter
{
    public class ConverterExtensions
    {
        public List<string> CustomScripts { get; set; } = [];
        public List<StringBuilder> Output { get; set; } = [];

        public void LoadAll()
        {
            CustomScripts = [.. ExtensionLoader.LoadExtensions().ToList()];
            Output.AddRange(CustomScripts.Select(_ => new StringBuilder())); //adds a new stringbuilder for each script
        }

        public void ExecuteMapScript(string basepath, string mapID, World? world)
        {
            for (int i = 0; i < CustomScripts.Count; i++)
            {
                ExecuteMapScript(CustomScripts[i], basepath, mapID, world, Output[i]);
            }
        }

        public void ExecuteAllBegin()
        {
            for (int i = 0; i < CustomScripts.Count; i++)
            {
                ExecuteBaseScript(CustomScripts[i], Output[i], "begin");
            }
        }

        public void ExecuteAllEnd()
        {
            for (int i = 0; i < CustomScripts.Count; i++)
            {
                ExecuteBaseScript(CustomScripts[i], Output[i], "runEnd");
            }
        }

        private static void ExecuteBaseScript(string script, StringBuilder sbOutput, string function)
        {
            try
            {
                using Lua lua = new Lua();
                lua.DoString(script);
#if DEBUG
                lua["debugLua"] = true;
#endif
                lua["print"] = (Action<object>)((msg) => Console.WriteLine("[Lua] " + msg));
                var test = lua[function];

                if (lua[function] is LuaFunction luaFunction)
                {
                    var result = string.Join("", luaFunction.Call(sbOutput.ToString()));
                    if (result.Length > 0)
                    {
                        sbOutput.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing {ex.Message}");
            }
        }

        private static void ExecuteMapScript(string script, string basepath, string mapname, World? world, StringBuilder sbOutput)
        {
            try
            {
                using Lua lua = new Lua();
                lua.DoString(script);
#if DEBUG
                lua["debugLua"] = true;
#endif
                lua["basepath"] = basepath;
                lua["mapname"] = mapname;
                if (world != null)
                {
                    lua["world"] = world;
                }
                // Redirect Lua print() to C# Console.WriteLine
                lua["print"] = (Action<object>)((msg) => Console.WriteLine("[Lua] " + msg));

                if (lua["runMap"] is LuaFunction luaFunction)
                {
                    object[] result = luaFunction.Call();
                    if (result.Length > 0)
                    {
                        if (result[0] is LuaTable luaTable)
                        {
                            // Convert LuaTable to a C# string array
                            string val = string.Join("\n", (luaTable.Values.Cast<string>()));
                            sbOutput.AppendLine(val);
                            var t = sbOutput.ToString();
                        }
                        else
                        {
                            string val = string.Join("", luaFunction.Call());
                            if (val.Length > 0)
                                sbOutput.AppendLine(val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing {ex.Message}");
            }
        }
    }

    internal static class ExtensionLoader
    {
        internal static IEnumerable<string> LoadExtensions()
        {
            foreach (var file in Directory.GetFiles("extensions", "*.lua"))
            {
                var text = "";
                try
                {
                    if (File.Exists(file))
                    {
                        text = File.ReadAllText(file);
                    }
                    else
                    {
                        Console.WriteLine($"Warning script {file} not exist");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                if (text == "") continue;
                yield return text;
            }
        }
    }
}