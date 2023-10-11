using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace ReSharperPlugin.RimworldDev.SymbolScope;

public class RimworldXmlDefSymbol
{
    public static readonly IUnsafeMarshaller<List<RimworldXmlDefSymbol>> Marshaller =
        UnsafeMarshallers.GetCollectionMarshaller(new UniversalMarshaller<RimworldXmlDefSymbol>(Read, Write), (size) => new List<RimworldXmlDefSymbol>());
    
    public string DefName { get; }
    public string DefType { get; }

    public int DocumentOffset { get; }
    
    // public IXmlTag Tag { get; }

    public RimworldXmlDefSymbol(ITreeNode tag, string defName, string defType)
    {
        DefName = defName;
        DefType = defType;
        DocumentOffset = tag.GetTreeStartOffset().Offset;
    }
    
    public RimworldXmlDefSymbol(int documentOffset, string defName, string defType)
    {
        DefName = defName;
        DefType = defType;
        DocumentOffset = documentOffset;
    }
    
    private static RimworldXmlDefSymbol Read(UnsafeReader reader)
    {
        var defType = reader.ReadString();
        var defName = reader.ReadString();
        var documentOffset = reader.ReadInt();
        
        return new RimworldXmlDefSymbol(documentOffset, defName, defType);
    }

    private static void Write(UnsafeWriter writer, RimworldXmlDefSymbol value)
    {
        writer.Write(value.DefType);
        writer.Write(value.DefName);
        writer.Write(value.DocumentOffset);
    }
}