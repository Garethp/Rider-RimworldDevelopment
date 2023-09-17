using JetBrains.Application.UI.Controls.FileSystem;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionPages;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.DataFlow;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.PathActions;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.Settings;

[OptionsPage(Pid, "Rimworld", typeof(FeaturesEnvironmentOptionsThemedIcons.CodeInspections),
    ParentId = ToolsPage.PID)]
public class SettingsPage : BeSimpleOptionsPage
{
    private const string Pid = "RimworldOptiosnPage";

    public SettingsPage(
        Lifetime lifetime,
        OptionsPageContext optionsPageContext,
        OptionsSettingsSmartContext optionsSettingsSmartContext,
        IconHost iconHost,
        ICommonFileDialogs commonFileDialogs,
        bool wrapInScrollablePanel = false
    ) : base(lifetime,
        optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)
    {
        var pathEntry = optionsSettingsSmartContext.Schema.GetScalarEntry((RimworldSettings key) => key.RimworldPath);

        var pathProperty = new Property<string>("RimworldOptiosnPage::RimworldPath");
        pathProperty.SetValue(
            optionsSettingsSmartContext
                .StoreOptionsTransactionContext
                .GetValueProperty<string>(lifetime, pathEntry, null)
                .GetValue()
        );

        pathProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
        {
            optionsSettingsSmartContext.StoreOptionsTransactionContext.SetValue(
                pathEntry, args.New, null
            );
        });

        var defaultLocation = ScopeHelper.FindRimworldDirectory(null) ?? FileSystemPath.Empty;

        AddFolderChooserOption(
            pathProperty,
            defaultLocation,
            defaultLocation,
            iconHost,
            commonFileDialogs,
            null,
            "Rimworld Path:",
            new[] { (BeSimplePathValidationRules.SHOULD_EXIST, ValidationStates.validationWarning) }
        );

        AddComment("If the your project is loading in the Core mod correctly, you don't need to set this. This is an override for the default auto-detection of Rimworld. Leaving this blank, or entering an incorrect path, will still allow the Rimworld plugin to attempt auto-detecting your Rimworld installation");
        AddComment("After changing this value, you will need to restart Rider for the change to take effect");
    }

    private void AddComment(string comment)
    {
        using (Indent())
        {
            var control = CreateCommentText(comment).WithCustomTextSize(BeFontSize.SMALLER);
            AddControl(control);
        }
    }
}