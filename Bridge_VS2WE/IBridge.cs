﻿using Kwytto.Interfaces;

namespace Bridge_VS2WE
{
    public abstract class IBridge : IBridgePrioritizable
    {
        public abstract bool WEAvailable { get; }
        public abstract int Priority { get; }

        public abstract int RegisterWTSLayout(string fileContent);
        public abstract void ClearWTSLayoutRegisters();
        public abstract bool GetSkinDescriptorForVehicle<T>(VehicleInfo vehicle, ushort vehicleId, bool isParked, out T layout) where T : class;
        public abstract bool GetSkinDescriptorByName<T>(VehicleInfo vehicle, string skinName, out T layout) where T : class;
        public abstract void ApplyLayout(VehicleInfo info, string skin, string contents);

        public abstract bool IsWEVehicleEditorOpen { get; }
        public abstract bool IsAnyEditorOpen { get; }
    }
}
