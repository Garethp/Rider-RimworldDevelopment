using JetBrains.Application.Resources;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;

namespace ReSharperPlugin.RimworldDev.XmlCompletion.Settings;

[SettingsKey(typeof (AutopopupEnabledSettingsKey), typeof (Strings), "XAMLSettingDescription")]
public class XmlAutopopupEnabledSettingsKey
{
    [SettingsEntry(AutopopupType.HardAutopopup, typeof (Strings), "OnSpaceSettingDescription")]
    public AutopopupType OnSpace;
    [SettingsEntry(AutopopupType.HardAutopopup, typeof (Strings), "OnPunctuationSettingDescription")]
    public AutopopupType OnPunctuation;
    [SettingsEntry(AutopopupType.HardAutopopup, typeof (Strings), "OnIdentifiersSettingDescription")]
    public AutopopupType OnIdentifiers;
}