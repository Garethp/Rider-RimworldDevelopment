using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests
{
    [TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Explicit)]
    public class CompletionTest: CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;

        protected override string RelativeTestDataPath => "F";
    
        [Test, TestSetting(typeof(CSharpCodeStyleSettingsKey), nameof(CSharpCodeStyleSettingsKey.DEFAULT_PRIVATE_MODIFIER), DefaultModifierDefinition.Implicit)]
        public void Test1() { DoNamedTest(); }
    }
}