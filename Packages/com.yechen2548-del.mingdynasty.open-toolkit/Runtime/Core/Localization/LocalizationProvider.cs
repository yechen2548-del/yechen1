using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface ILocalizationProvider
    {
        void Set(string key, string localizedText);
        bool TryGet(string key, out string localizedText);
        string Get(string key, string fallbackText);
    }

    public sealed class DictionaryLocalizationProvider : ILocalizationProvider
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

        public void Set(string key, string localizedText)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A localization key cannot be empty.", nameof(key));
            }

            values[key] = localizedText ?? string.Empty;
        }

        public bool TryGet(string key, out string localizedText)
        {
            return values.TryGetValue(key, out localizedText);
        }

        public string Get(string key, string fallbackText)
        {
            string localizedText;
            return TryGet(key, out localizedText) ? localizedText : fallbackText;
        }
    }
}
