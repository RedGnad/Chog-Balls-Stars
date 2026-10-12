using System;
using System.Globalization;

namespace Chog.Diagnostics
{
    /// <summary>
    /// Engine-independent helpers of the performance overlay, testable outside Unity.
    /// </summary>
    public static class PerformanceSampling
    {
        /// <summary>True when the URL query contains "name", "name=1" or "name=true".</summary>
        public static bool HasQueryFlag(string url, string name)
        {
            if (!TryGetQueryValue(url, name, out var value))
                return false;

            return value == null || value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Reads an integer query parameter such as "hz=30" or "fps=-1".</summary>
        public static bool TryGetQueryInt(string url, string name, out int value)
        {
            value = 0;
            return TryGetQueryValue(url, name, out var text) && text != null &&
                   int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Finds the first query parameter with that name, ignoring case. The value is null
        /// when the parameter has no "=" part. The URL fragment is never read.
        /// </summary>
        public static bool TryGetQueryValue(string url, string name, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(name))
                return false;

            var queryStart = url.IndexOf('?');
            if (queryStart < 0)
                return false;

            var query = url.Substring(queryStart + 1);
            var fragmentStart = query.IndexOf('#');
            if (fragmentStart >= 0)
                query = query.Substring(0, fragmentStart);

            foreach (var pair in query.Split('&'))
            {
                var separator = pair.IndexOf('=');
                var key = separator < 0 ? pair : pair.Substring(0, separator);
                if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = separator < 0 ? null : pair.Substring(separator + 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Nearest-rank percentile of the first "count" values, percent in (0, 100].
        /// Sorts that range of the array in place; returns 0 when there is no value.
        /// </summary>
        public static float Percentile(float[] values, int count, float percent)
        {
            if (values == null || count <= 0)
                return 0f;
            if (count > values.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            Array.Sort(values, 0, count);
            return PercentileOfSorted(values, count, percent);
        }

        /// <summary>Nearest-rank percentile of the first "count" values, already sorted ascending.</summary>
        public static float PercentileOfSorted(float[] sortedValues, int count, float percent)
        {
            if (sortedValues == null || count <= 0)
                return 0f;
            if (count > sortedValues.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            var rank = (int)Math.Ceiling(percent / 100.0 * count);
            return sortedValues[Math.Min(Math.Max(rank, 1), count) - 1];
        }
    }
}
