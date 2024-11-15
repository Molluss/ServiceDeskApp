using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ServiceDeskApp.Templates
{
    public static class TemplatesManager
    {
        private static string templatesPath = "templates.json";

        public static List<string> LoadTemplates()
        {
            if (!File.Exists(templatesPath))
                return new List<string>();

            var json = File.ReadAllText(templatesPath);
            return JsonConvert.DeserializeObject<List<string>>(json);
        }

        public static void SaveTemplates(List<string> templates)
        {
            var json = JsonConvert.SerializeObject(templates, Formatting.Indented);
            File.WriteAllText(templatesPath, json);
        }
    }
}
