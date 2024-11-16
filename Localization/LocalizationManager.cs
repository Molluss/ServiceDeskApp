using System.Globalization;
using System.Resources;

namespace ServiceDeskApp.Localization
{
    public static class LocalizationManager
    {
        private static ResourceManager resourceManager = new ResourceManager("ServiceDeskApp.Localization.Resources", typeof(LocalizationManager).Assembly);

        public static string GetTranslation(string key, string language)
        {
            var culture = new CultureInfo(language);
            return resourceManager.GetString(key, culture) ?? key;
        }
    }
}
