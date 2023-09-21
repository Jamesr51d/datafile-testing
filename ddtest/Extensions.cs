using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FiftyOne.Pipeline.Engines.Data;

namespace LocalDetection
{
    public static class Extensions
    {
        public static string GetHumanReadable(this IAspectPropertyValue<string> PropertyValue)
        {
            return PropertyValue.HasValue ? PropertyValue.Value : $"Unknown ({PropertyValue.NoValueMessage})";
        }

        public static string GetHumanReadable(this IAspectPropertyValue<bool> PropertyValue)
        {
            return PropertyValue.HasValue ? PropertyValue.Value.ToString() : $"Unknown ({PropertyValue.NoValueMessage})";
        }

        public static string GetHumanReadable(this IAspectPropertyValue<IReadOnlyList<string>> PropertyValue)
        {
            return PropertyValue.HasValue ? string.Join("| ", PropertyValue.Value) : $"Unknown ({PropertyValue.NoValueMessage})";
        }

        public static string GetHumanReadable(this IAspectPropertyValue<int> PropertyValue)
        {
            return PropertyValue.HasValue ? PropertyValue.Value.ToString() : $"Unknown ({PropertyValue.NoValueMessage})";
        }
    }
}
