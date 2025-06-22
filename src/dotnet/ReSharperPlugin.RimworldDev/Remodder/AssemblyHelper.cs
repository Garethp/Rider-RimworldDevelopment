using System.IO;
using AsmResolver;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;
using AsmResolver.PE.DotNet.Metadata.Strings;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace ReSharperPlugin.RimworldDev.Remodder;

public static class AssemblyHelper
{
    public static byte[] RemoveReferenceAssemblyAttribute(byte[] input)
    {
        var peImage = PEImage.FromBytes(input);
        var tables = peImage.DotNetDirectory.Metadata.GetStream<TablesStream>();
        var customAttrsTable = tables.GetTable<CustomAttributeRow>();
        var memberRefTable = tables.GetTable<MemberReferenceRow>();
        var typeRefTable = tables.GetTable<TypeReferenceRow>();
        var stringsStream = peImage.DotNetDirectory.Metadata.GetStream<StringsStream>();
    
        for (int i = customAttrsTable.Count - 1; i >= 0; i--)
        {
            var r = customAttrsTable[i];
            if ((r.Type & 0b111) == 3)
            {
                var ctor = memberRefTable.GetByRid(r.Type >> 3);
                if ((ctor.Parent & 0b111) == 1)
                {
                    var type = typeRefTable.GetByRid(ctor.Parent >> 3);
                    if (stringsStream.GetStringByIndex(type.Name) == "ReferenceAssemblyAttribute")
                    {
                        customAttrsTable.Remove(r);
                    }
                }
            }
        }
    
        var fileBuilder = new ManagedPEFileBuilder(EmptyErrorListener.Instance);
        var file = fileBuilder.CreateFile(peImage);
    
        var stream = new MemoryStream();
        file.Write(stream);
    
        return stream.ToArray();
    }
}