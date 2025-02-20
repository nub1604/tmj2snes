namespace tmj2snes.CustomConverter;

internal static class ScriptLoader
{
    internal static IEnumerable<string> LoadScript()
    {
        var folder = Path .Combine(Environment.CurrentDirectory, "extensions");
        foreach (var file in Directory.GetFiles(folder, "*.lua"))
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