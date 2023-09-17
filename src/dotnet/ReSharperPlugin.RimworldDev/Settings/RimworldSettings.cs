using JetBrains.Application.Communication;
using JetBrains.Application.Settings;

namespace ReSharperPlugin.RimworldDev.Settings;

[SettingsKey(typeof(InternetSettings), "Rimworld Settings")]
public class RimworldSettings
{
    [SettingsEntry("", "Rimworld path")]
    public string RimworldPath { get; set; } = "";
}