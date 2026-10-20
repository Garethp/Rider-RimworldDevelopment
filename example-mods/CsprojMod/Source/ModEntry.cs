using Verse;

namespace CsprojModFixture;

/// <summary>Only here so the fixture is a real C# project rather than an empty one.</summary>
public class ModEntry : Mod
{
    public ModEntry(ModContentPack content) : base(content)
    {
    }
}
