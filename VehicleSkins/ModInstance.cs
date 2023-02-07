using ICities;
using Kwytto.Interfaces;
using Kwytto.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using VehicleSkins.Localization;
using VehicleSkins.Singleton;
using VehicleSkins.UI;

[assembly: AssemblyVersion("1.1.0.7")]
namespace VehicleSkins
{
    public class ModInstance : BasicIUserMod<ModInstance, VSMainController>
    {
        public override string SimpleName => "Vehicle Skins";

        public override string SafeName => "VehicleSkins";

        public override string Acronym => "VS";

        public override Color ModColor => ColorExtensions.FromRGB("4d2f0f");

        public override string Description => Str.root_modDescription;

        protected override void SetLocaleCulture(CultureInfo culture) => Str.Culture = culture;

        private IUUIButtonContainerPlaceholder[] cachedUUI;
        public override IUUIButtonContainerPlaceholder[] UUIButtons => cachedUUI ?? (cachedUUI = new[]
        {
            new UUIWindowButtonContainerPlaceholder(
                buttonName: Instance.SimpleName,
                tooltip: Instance.GeneralName,
                iconPath: "ModIcon_NoBG",
                windowGetter: ()=>VSBaseLiteUI.Instance
             )
        });
        protected override void OnLevelLoadingInternal()
        {
            base.OnLevelLoadingInternal();
            SkinsSingleton.instance.ReloadSkins();
        }
        protected override Dictionary<string, Func<IBridgePrioritizable>> ModBridges { get; } = new Dictionary<string, Func<IBridgePrioritizable>>
        {
            ["Write Everywhere"] = () => controller?.ConnectorWE,
            ["Custom Data Mod"] = () => controller?.ConnectorCD
        };
        protected override void DoOnLevelUnloading()
        {
            base.DoOnLevelUnloading();
            cachedUUI = null;
        }
        protected override bool IsValidLoadMode(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.NewAsset:
                case LoadMode.LoadAsset:
                case LoadMode.NewScenarioFromGame:
                case LoadMode.NewScenarioFromMap:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                case LoadMode.UpdateScenarioFromGame:
                case LoadMode.UpdateScenarioFromMap:
                    return true;
                default:
                    return false;
            }
        }
        protected override bool IsValidLoadMode(ILoading loading) => loading?.currentMode == AppMode.Game || loading?.currentMode == AppMode.AssetEditor;

        public override string[] AssetExtraDirectoryNames => new string[] {
            VSMainController.EXTRA_SPRITES_FILES_FOLDER_ASSETS
        };

    }
}