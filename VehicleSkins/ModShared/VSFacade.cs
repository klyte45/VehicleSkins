using UnityEngine;
using VehicleSkins.Singleton;
namespace VehicleSkins.ModShared
{
    public static class VSFacade
    {
        public static bool GetDescriptorForVehicle<T>(VehicleInfo vehicle, ushort vehicleId, bool isParked, out T layout) where T : class => ModInstance.Controller.ConnectorWE.GetSkinDescriptorForVehicle(vehicle, vehicleId, isParked, out layout);
        public static string[] GetAvailableSkins(VehicleInfo info) => SkinsSingleton.instance.GetAvailableSkins(info);
        public static bool GetSkin<T>(VehicleInfo info, string skinName, out T skin) where T : class => ModInstance.Controller.ConnectorWE.GetSkinDescriptorByName(info, skinName, out skin);
        public static void Apply(VehicleInfo info, string skin, string contents) => ModInstance.Controller.ConnectorWE.ApplyLayout(info, skin, contents);
        public static Material GetSkinMaterial(VehicleInfo info, string skinName) => SkinsSingleton.instance.GetSkin(info, skinName, out var material) ? material.main : null;
    }
}