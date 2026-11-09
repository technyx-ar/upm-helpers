using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Technyx.Sdk.Models
{
    // PHP encodes an empty associative array as [] instead of {}.
    public sealed class EmptyArrayAsObjectConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var token = JToken.Load(reader);

            if (token is JArray array)
            {
                if (array.Count == 0)
                    return Activator.CreateInstance(objectType);

                throw new JsonSerializationException("Expected an object but found a non-empty array.");
            }

            var result = existingValue ?? Activator.CreateInstance(objectType);
            using (var tokenReader = token.CreateReader())
                serializer.Populate(tokenReader, result);

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
