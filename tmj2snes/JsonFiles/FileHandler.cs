using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmj2snes.JsonFiles
{


    public interface iConverter<T>
    {
        public T? Convert(string input);

    }
    public static class FileHandler
    {
        public static T? LoadFile<T>(string path) where T: iConverter<T>
        {
            try
            {
                // Open the text file using a stream reader.
                using (var sr = new StreamReader(path))
                {

                    var inst =   (T?)Activator.CreateInstance(typeof(T), null);
                    if (inst == null)return default;
                    var json = sr.ReadToEnd();
                    return inst.Convert(json);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return default;
        }
    }
}
