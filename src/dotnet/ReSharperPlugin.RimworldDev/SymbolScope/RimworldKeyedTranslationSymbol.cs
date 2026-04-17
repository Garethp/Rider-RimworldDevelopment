using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util.PersistentMap;

namespace ReSharperPlugin.RimworldDev.SymbolScope;

public class RimworldKeyedTranslationSymbol
{
    public static readonly IUnsafeMarshaller<List<RimworldKeyedTranslationSymbol>> Marshaller =
        UnsafeMarshallers.GetCollectionMarshaller(new UniversalMarshaller<RimworldKeyedTranslationSymbol>(Read, Write), (size) => new List<RimworldKeyedTranslationSymbol>());
    
    public string KeyName { get; }
    public string Langauge { get; }

    public int DocumentOffset { get; }
    
    public RimworldKeyedTranslationSymbol(ITreeNode tag, string keyName, string language)
    {
        KeyName = keyName;
        Langauge = language;
        DocumentOffset = tag.GetTreeStartOffset().Offset;
    }
    
    public RimworldKeyedTranslationSymbol(int documentOffset, string keyName, string language)
    {
        KeyName = keyName;
        Langauge = language;
        DocumentOffset = documentOffset;
    }
    
    private static RimworldKeyedTranslationSymbol Read(UnsafeReader reader)
    {
        var keyName = reader.ReadString();
        var langauge = reader.ReadString();
        var documentOffset = reader.ReadInt();
        
        return new RimworldKeyedTranslationSymbol(documentOffset, langauge, keyName);
    }

    private static void Write(UnsafeWriter writer, RimworldKeyedTranslationSymbol value)
    {
        writer.Write(value.KeyName);
        writer.Write(value.Langauge);
        writer.Write(value.DocumentOffset);
    }
}