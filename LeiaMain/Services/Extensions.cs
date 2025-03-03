using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using DataObjects;

namespace Services
{
    public static class Extensions
    {
        public static void UpdatePropertiesFrom<T>(this T destination, T source, params string[] propertiesToExclude)
        {
            if (source == null || destination == null)
            {
                Trace.WriteLine($"In Extensions.UpdatePropertiesFrom, source: {source}, or destination: {destination}, were null");
                throw new ArgumentNullException("Source or destination are null.");
            }

            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                // a way to exclude updating particular properties by name 
                if (propertiesToExclude.Contains(prop.Name) || !prop.CanWrite)
                    continue;

                // Skip complex types and collections if necessary
                // if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) continue;

                var newValue = prop.GetValue(source, null);
                prop.SetValue(destination, newValue, null);
            }
        }

    }
}
