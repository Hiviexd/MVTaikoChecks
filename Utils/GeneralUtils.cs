using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MVTaikoChecks.Utils
{
    public static class GeneralUtils
    {
        // comparison

        public static bool IsWithin(this double num, double range, double of)
            => num <= of + range && num >= of - range;

        // dictionary

        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, TValue value)
        {
            foreach (var key in keys)
                dictionary.Add(key, value);
        }

        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, Func<TValue> valueFactory)
        {
            foreach (var key in keys)
                dictionary.Add(key, valueFactory());
        }
    }
}
