using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace ReSharperPlugin.RimworldDev.Cache;

public class RimworldXMLCacheItem
{
    public static readonly IUnsafeMarshaller<List<RimworldXMLCacheItem>> Marshaller =
        UnsafeMarshallers.GetCollectionMarshaller(new UniversalMarshaller<RimworldXMLCacheItem>(Read, Write), (size) => new List<RimworldXMLCacheItem>());
    
    public string DefName { get; }
    public string DefType { get; }

    public int DocumentOffset { get; }
    
    // public IXmlTag Tag { get; }

    public RimworldXMLCacheItem(IXmlTag tag, string defName, string defType)
    {
        DefName = defName;
        DefType = defType;
        DocumentOffset = tag.GetTreeStartOffset().Offset;
    }
    
    public RimworldXMLCacheItem(int documentOffset, string defName, string defType)
    {
        DefName = defName;
        DefType = defType;
        DocumentOffset = documentOffset;
    }
    
    private static RimworldXMLCacheItem Read(UnsafeReader reader)
    {
        var defType = reader.ReadString();
        var defName = reader.ReadString();
        var documentOffset = reader.ReadInt();
        
        return new RimworldXMLCacheItem(documentOffset, defName, defType);
    }

    private static void Write(UnsafeWriter writer, RimworldXMLCacheItem value)
    {
        writer.Write(value.DefType);
        writer.Write(value.DefName);
        writer.Write(value.DocumentOffset);
    }
}