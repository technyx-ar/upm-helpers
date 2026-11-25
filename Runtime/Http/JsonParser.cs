using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Http
{
    internal static class JsonParser
    {
        // JToken.Parse turns ISO 8601 strings into DateTime, which ToObject then renders back
        // as culture-dependent text in string fields such as created_at.
        internal static JToken Parse(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
            {
                var token = JToken.Load(reader);

                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.Comment)
                        throw new JsonReaderException("Additional text encountered after finished reading JSON content.");
                }

                return token;
            }
        }
    }
}
