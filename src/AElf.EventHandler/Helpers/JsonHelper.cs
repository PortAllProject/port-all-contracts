using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.EventHandler
{
    public class JsonHelper
    {
        public static string ReadJson(string jsonfile, string key)
        {
            using var file = System.IO.File.OpenText(jsonfile);
            using var reader = new JsonTextReader(file);
            var o = (JObject) JToken.ReadFrom(reader);
            var value = o[key]?.ToString();
            return value;
        }
    }
}