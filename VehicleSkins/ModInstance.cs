using Kwytto.Interfaces;
using Kwytto.Utils;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using VehicleSkins.Localization;
using VehicleSkins.Singleton;
using VehicleSkins.UI;

[assembly: AssemblyVersion("1.1.0.10003")]
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
                iconPath: "ModIcon",
                windowGetter: ()=>VSBaseLiteUI.Instance
             )
        });
        protected override void OnLevelLoadingInternal()
        {
            base.OnLevelLoadingInternal();
            SkinsSingleton.instance.ReloadSkins();
        }
    }
}