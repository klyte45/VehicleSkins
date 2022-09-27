using Bridge_VS2WE;

namespace VehicleSkins.ModShared
{
    internal class BridgeWEFallback : IBridge
    {
        public override bool WEAvailable { get; } = false;

        public override int Priority { get; } = 1000;
        public override bool IsBridgeEnabled { get; } = true;

        public override bool IsWEVehicleEditorOpen { get; } = false;

        public override bool IsAnyEditorOpen { get; } = false;

        public override string CurrentSelectionSkin { get; } = null;

        public override ushort CurrentFocusVehicle { get; } = 0;

        public override void ClearWELayoutRegisters() { }

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
        public override int RegisterWELayout(string fileContent) => -1;
        public override void ApplyLayout(VehicleInfo info, string skin, string contents) { }

    }
}