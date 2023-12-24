using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace ReSharperPlugin.RimworldDev.DevTools;

public class PropertyUsageItem
{
    public readonly string FullName;

    public PropertyUsageItem(string fullName)
    {
        FullName = fullName;
    }
    
    public static readonly IUnsafeMarshaller<List<PropertyUsageItem>> Marshaller =
        UnsafeMarshallers.GetCollectionMarshaller(new UniversalMarshaller<PropertyUsageItem>(Read, Write), _ => new List<PropertyUsageItem>());
    
    private static PropertyUsageItem Read(UnsafeReader reader)
    {
        var fullName = reader.ReadString();
        
        return new PropertyUsageItem(fullName);
    }

    private static void Write(UnsafeWriter writer, PropertyUsageItem value)
    {
        writer.Write(value.FullName);
    }
}