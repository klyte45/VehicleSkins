extern alias CD;

using CD::CustomData.Overrides;
using VehicleSkins.ModShared;
using VehicleSkins.Singleton;

namespace K45_VS2CD
{
    public class BridgeCD : IBridgeCD
    {

        public override int Priority => 0;

        public override bool IsBridgeEnabled => true;

        public override string GetPreferredSkin(ushort buildingId) => CDFacade.Instance.GetPreferredVehiclesSkinForBuilding(buildingId);

        public BridgeCD()
        {
            CDFacade.Instance.EventOnBuildingVehicleSkinChanged += (x) => SkinsSingleton.instance.ResetCache();
        }
    }
}
