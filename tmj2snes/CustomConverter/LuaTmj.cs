
using System.Text;
using tmj2snes.JsonFiles;
using NLua;


namespace tmj2snes.CustomConverter;

public class LuaTmj
{

#pragma warning disable IDE1006 
    private List<string> _scripts { get; set; } = [];
    private List<StringBuilder> _output { get; set; } = [];
#pragma warning restore IDE1006 

    public void LoadAll()
    {
        _scripts = [.. ScriptLoader.LoadScript().ToList()];
        _output.AddRange(_scripts.Select(_ => new StringBuilder())); //adds a new stringbuilder for each script
    }

    public void ExecuteMapScript(string basepath, string mapID, World? world)
    {
        for (int i = 0; i < _scripts.Count; i++)
            ExecuteMapScript(_scripts[i], basepath, mapID, world, _output[i]);
    }

    public void ExecuteAllBegin()
    {
        for (int i = 0; i < _scripts.Count; i++)
            ExecuteBaseScript(_scripts[i], _output[i], "runBegin");
    }

    public void ExecuteAllEnd()
    {
        for (int i = 0; i < _scripts.Count; i++)
            ExecuteBaseScript(_scripts[i], _output[i], "runEnd");
    }

    private static void ExecuteBaseScript(string script, StringBuilder sbOutput, string function)
    {
        try
        {
            using Lua lua = new();
            lua.DoString(script);
#if DEBUG
            lua["debugLua"] = true;
#endif
            lua["print"] = (Action<object>)((msg) => Console.WriteLine("[Lua] " + msg));
            var test = lua[function];

            if (lua[function] is LuaFunction luaFunction)
            {
                CallLuaScript(sbOutput, luaFunction);
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
            using Lua lua = new();
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
                CallLuaScript(sbOutput, luaFunction);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing {ex.Message}");
        }
    }

    private static void CallLuaScript(StringBuilder sbOutput, LuaFunction luaFunction)
    {
        object[] result = luaFunction.Call(sbOutput.ToString());
        if (result.Length == 0) return;

        if (result[0] is LuaTable luaTable)
        {
            // Convert LuaTable to a C# string array
            string val = string.Join("\n", (luaTable.Values.Cast<string>()));
            sbOutput.AppendLine(val);
            var t = sbOutput.ToString();
        }
        else
        {
            string val = string.Join("", result[0]);
            if (val.Length > 0)
                sbOutput.AppendLine(val);
        }
    }
}
