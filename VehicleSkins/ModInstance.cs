using Kwytto.Interfaces;
using Kwytto.Utils;
using System.Globalization;
using UnityEngine;
using VehicleSkins.Localization;

namespace VehicleSkins
{
    public class ModInstance : BasicIUserMod<ModInstance, MainController>
    {
        public override string SimpleName => "Vehicle Skins";

        public override string SafeName => "VehicleSkins";

        public override string Acronym => "VS";

        public override Color ModColor => ColorExtensions.FromRGB("4d2f0f");

        public override string Description => Str.root_modDescription;

        protected override void SetLocaleCulture(CultureInfo culture) => Str.Culture = culture;
    }
}