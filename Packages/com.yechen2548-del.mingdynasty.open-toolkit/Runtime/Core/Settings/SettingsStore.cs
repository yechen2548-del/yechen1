using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface ISettingsStore
    {
        void SetString(string key, string value);
        string GetString(string key, string fallbackValue);
        bool Contains(string key);
    }

    public sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A setting key cannot be empty.", nameof(key));
            }

            values[key] = value ?? string.Empty;
        }

        public string GetString(string key, string fallbackValue)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : fallbackValue;
        }

        public bool Contains(string key)
        {
            return values.ContainsKey(key);
        }
    }
}
