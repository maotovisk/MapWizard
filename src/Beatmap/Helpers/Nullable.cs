
using System.Reflection;

namespace BeatmapParser;

/// <summary>
/// Generic helper methods.
/// </summary>
public partial class Helpers
{
    /// <summary>
    /// Determines whether the specified type is nullable.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType)
        {
            return true; // Reference type
        }
        else if (Nullable.GetUnderlyingType(type) != null)
        {
            return true; // Nullable<T>
        }
        else
        {
            return false; // Value type
        }
    }

    /// <summary>
    /// Counts the number of properties in a class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int CountNonNullableProperties<T>()
    {
        // Not considering nullable properties
        return typeof(T).GetProperties().Count(p => !IsNullable(p.PropertyType));
    }

    /// <summary>
    /// Counts the number of properties in a class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int CountProperties<T>() => typeof(T).GetProperties().Length;

    /// <summary>
    /// Gets the missing properties from a list of properties.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static List<string> GetMissingPropertiesNames<T>(IEnumerable<string> properties)
    {
        List<string> missingProperties = [];
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            if (!properties.Contains(property.Name) && !IsNullable(property.PropertyType))
            {
                missingProperties.Add(property.Name);
            }
        }
        return missingProperties;
    }

    // static T? GetValueOrNull<FN>(Dictionary<string, T> dictionary, string key, Func<string, FN> parser) where T : struct
    // {
    //     if (dictionary.TryGetValue(key, out string value))
    //     {
    //         return parser(value);
    //     }
    //     else
    //     {
    //         return null;
    //     }
    // }

}