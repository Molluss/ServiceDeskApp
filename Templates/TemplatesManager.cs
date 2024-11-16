using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ServiceDeskApp.Templates
{
    public class Template
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    public static class TemplatesManager
    {
        private static string templatesPath = "templates.json";

        public static List<Template> LoadTemplates()
        {
            if (!File.Exists(templatesPath))
                return new List<Template>();

            var json = File.ReadAllText(templatesPath);

            try
            {
                // Essayer de désérialiser en tant que liste de Template
                return JsonConvert.DeserializeObject<List<Template>>(json);
            }
            catch (JsonSerializationException)
            {
                // Si l'ancien format (liste de chaînes) est détecté, le convertir
                var oldFormat = JsonConvert.DeserializeObject<List<string>>(json);
                var templates = new List<Template>();
                foreach (var name in oldFormat)
                {
                    templates.Add(new Template { Name = name, Text = "" }); // Texte vide par défaut
                }
                SaveTemplates(templates); // Sauvegarder au nouveau format
                return templates;
            }
        }


        public static void SaveTemplates(List<Template> templates)
        {
            var json = JsonConvert.SerializeObject(templates, Formatting.Indented);
            File.WriteAllText(templatesPath, json);
        }

        public static void AddTemplate(string name, string text)
        {
            var templates = LoadTemplates();
            templates.Add(new Template { Name = name, Text = text });
            SaveTemplates(templates);
        }

        public static void EditTemplate(string oldName, string newName, string newText)
        {
            var templates = LoadTemplates();
            var template = templates.Find(t => t.Name == oldName);
            if (template != null)
            {
                template.Name = newName;
                template.Text = newText;
                SaveTemplates(templates);
            }
        }

        public static void DeleteTemplate(string name)
        {
            var templates = LoadTemplates();
            templates.RemoveAll(t => t.Name == name);
            SaveTemplates(templates);
        }
    }
}
