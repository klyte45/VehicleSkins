using Bridge_VS2WE;

namespace VehicleSkins.ModShared
{
    internal class BridgeWEFallback : IBridge
    {
        public override bool WEAvailable { get; } = false;

        public override int Priority { get; } = 1000;

        public override bool IsWEVehicleEditorOpen { get; } = false;

        public override bool IsAnyEditorOpen { get; } = false;

        public override void ClearWTSLayoutRegisters() { }

        public override bool GetSkinDescriptorByName<T>(VehicleInfo vehicle, string skinName, out T layout)
        {
            layout = null;
            return false;
        }

        public override bool GetSkinDescriptorForVehicle<T>(VehicleInfo vehicle, ushort vehicleId, bool isParked, out T layout)
        {
            layout = null;
            return false;
        }
        public override int RegisterWTSLayout(string fileContent) => -1;
        public override void ApplyLayout(VehicleInfo info, string skin, string contents) { }

    }
}