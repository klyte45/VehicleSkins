using Kwytto.Interfaces;

namespace VehicleSkins.ModShared
{
    public abstract class IBridgeWE : IBridgePrioritizable
    {
        public abstract bool WEAvailable { get; }
        public abstract int Priority { get; }

        public abstract int RegisterWELayout(string fileContent);
        public abstract void ClearWELayoutRegisters();
        public abstract bool GetSkinDescriptorForVehicle<T>(VehicleInfo vehicle, ushort vehicleId, bool isParked, out T layout, ushort buildingId) where T : class;
        public abstract bool GetSkinDescriptorByName<T>(VehicleInfo vehicle, string skinName, out T layout) where T : class;
        public abstract void ApplyLayout(VehicleInfo info, string skin, string contents);

        public abstract bool IsWEVehicleEditorOpen { get; }
        public abstract bool IsAnyEditorOpen { get; }
        public abstract string CurrentSelectionSkin { get; }
        public abstract ushort CurrentFocusVehicle { get; }
        public abstract bool IsBridgeEnabled { get; }
    }
}
