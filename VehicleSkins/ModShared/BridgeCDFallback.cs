
namespace VehicleSkins.ModShared
{
    public class BridgeCDFallback : IBridgeCD
    {
        public override int Priority => 1000;

        public override bool IsBridgeEnabled => false;

        public override string GetPreferredSkin(ushort buildingId) => null;
    }
}