using Kwytto.Interfaces;

namespace VehicleSkins.ModShared
{
    public abstract class IBridgeCD : IBridgePrioritizable
    {
        public abstract int Priority { get; }
        public abstract bool IsBridgeEnabled { get; }

        public abstract string GetPreferredSkin(ushort buildingId);
    }
}
