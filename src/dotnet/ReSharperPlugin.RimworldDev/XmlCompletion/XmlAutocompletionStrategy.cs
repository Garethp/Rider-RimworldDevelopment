using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.TextControl;

namespace ReSharperPlugin.RimworldDev.XmlCompletion;

[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class XmlAutocompletionStrategy: IAutomaticCodeCompletionStrategy
{
    public AutopopupType IsEnabledInSettings(IContextBoundSettingsStore settingsStore, ITextControl textControl)
    {
        return AutopopupType.SoftAutopopup;
    }

    public bool AcceptTyping(char c, ITextControl textControl, IContextBoundSettingsStore settingsStore)
    {
        return true;
    }

    public bool ProcessSubsequentTyping(char c, ITextControl textControl)
    {
        return false;
    }

    public bool AcceptsFile(IFile file, ITextControl textControl)
    {
        return true;
    }

    public PsiLanguageType Language => XmlLanguage.Instance.NotNull();
    public bool ForceHideCompletion => false;
}