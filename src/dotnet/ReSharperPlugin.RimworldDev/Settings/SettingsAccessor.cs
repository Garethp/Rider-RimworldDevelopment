using JetBrains.Application;
using JetBrains.Application.Settings;

namespace ReSharperPlugin.RimworldDev.Settings;

[ShellComponent]
public class SettingsAccessor
{
    private static SettingsAccessor _instance;
    private ISettingsStore _settingsStore;

    public SettingsAccessor(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _instance = this;
    }

    public RimworldSettings GetSettings()
    {
        var bound = _settingsStore.BindToContextTransient(ContextRange.Smart((lt, _) => _.Empty));
        
        return bound.GetKey<RimworldSettings>(SettingsOptimization.OptimizeDefault);
    }

    public static SettingsAccessor Instance => _instance;
}