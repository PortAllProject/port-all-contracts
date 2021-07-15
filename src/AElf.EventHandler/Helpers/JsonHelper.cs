using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public static string ParseJson(string response, List<string> attributes)
        {
            var jsonDoc = JsonDocument.Parse(response);
            var data = string.Empty;

            foreach (var attribute in attributes)
            {
                if (!attribute.Contains('/'))
                {
                    if (jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement))
                    {
                        if (data == string.Empty)
                        {
                            data = targetElement.GetRawText();
                        }
                        else
                        {
                            data += $";{targetElement.GetRawText()}";
                        }
                    }
                    else
                    {
                        return data;
                    }
                }
                else
                {
                    var attrs = attribute.Split('/');
                    var targetElement = jsonDoc.RootElement.GetProperty(attrs[0]);
                    foreach (var attr in attrs.Skip(1))
                    {
                        if (!targetElement.TryGetProperty(attr, out targetElement))
                        {
                            return attr;
                        }
                    }

                    if (data == string.Empty)
                    {
                        data = targetElement.GetRawText();
                    }
                    else
                    {
                        data += $";{targetElement.GetRawText()}";
                    }
                }
            }

            return data;
        }
    }
}