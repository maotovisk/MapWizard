using System.Diagnostics.CodeAnalysis;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Generic helper methods.
/// </summary>
public partial class Helper
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
    private static int CountNonNullableProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => typeof(T).GetProperties().Count(p => !IsNullable(p.PropertyType));


    /// <summary>
    /// Counts the number of properties in a class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static int CountProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => typeof(T).GetProperties().Length;

    /// <summary>
    /// Gets the missing properties from a list of properties.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetMissingPropertiesNames<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(IEnumerable<string> properties)
    {
        List<string> missingProperties = [];
        foreach (var property in typeof(T).GetProperties())
        {
            if (!properties.Contains(property.Name) && !IsNullable(property.PropertyType))
            {
                missingProperties.Add(property.Name);
            }
        }
        return missingProperties;
    }

    /// <summary>
    /// Determines whether the number of properties is within the expected range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <returns></returns>
    public static bool IsWithinPropertyQuantity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(int count) => count > CountProperties<T>() || count < CountNonNullableProperties<T>();

}