using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace tmj2snes.JsonFiles
{
    public class AutoNumberToIntConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            
            var res = typeof(Int32) == typeToConvert;
            return res;
        }
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {

                return Convert.ToInt32(reader.GetDouble());
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return Convert.ToInt32( reader.GetString());
            }
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                return document.RootElement.Clone().ToString();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }
}

