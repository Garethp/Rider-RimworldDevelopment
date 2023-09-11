using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Features.Intellisense.Resources;

namespace ReSharperPlugin.RimworldDev.XmlCompletion.Settings;

[SettingsKey(typeof (IntellisenseEnabledSettingsKey), typeof (Strings), "OverrideVSIntelliSenseForXAMLSettingDescription")]
public class IntellisenseEnabledSettingXml
{
    [SettingsEntry(true, typeof (Strings), "XAMLXamlFilesSettingDescription")]
    public bool IntellisenseEnabled;
}