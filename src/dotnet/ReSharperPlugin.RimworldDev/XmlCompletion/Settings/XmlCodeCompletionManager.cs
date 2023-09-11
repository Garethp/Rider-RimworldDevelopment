using System;
using System.Linq.Expressions;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml;

namespace ReSharperPlugin.RimworldDev.XmlCompletion.Settings;

[ShellComponent]
public class XmlCodeCompletionManager : LanguageSpecificCodeCompletionManager
{
    public XmlCodeCompletionManager(
        CodeCompletionSettingsService codeCompletionSettings)
        : base(codeCompletionSettings)
    {
    }

    public override SettingsScalarEntry GetSettingsEntry(ISettingsSchema settingsSchema) =>
        settingsSchema.GetScalarEntry(
            (Expression<Func<IntellisenseEnabledSettingXml, bool>>)(setting => setting.IntellisenseEnabled));

    public override PsiLanguageType PsiLanguage =>
        XmlLanguage.Instance.NotNull();
}