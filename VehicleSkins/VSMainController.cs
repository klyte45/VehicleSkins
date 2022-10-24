using ColossalFramework.UI;
using Kwytto.Interfaces;
using Kwytto.Utils;
using System.IO;
using VehicleSkins.ModShared;
using VehicleSkins.Singleton;
using VehicleSkins.Tools;
using VehicleSkins.UI;

namespace VehicleSkins
{
    public class VSMainController : BaseController<ModInstance, VSMainController>
    {

        public const string EXTRA_SPRITES_FILES_FOLDER_ASSETS = "K45_VS_Skins";
        public static readonly string FOLDER_NAME = Path.Combine(KFileUtils.BASE_FOLDER_PATH, "VehicleSkins");
        public static readonly string SKINS_FOLDER = Path.Combine(FOLDER_NAME, "Skins");

        public static SkinsSingleton Skins => SkinsSingleton.instance;
        public IBridgeWE ConnectorWE { get; private set; }
        public IBridgeCD ConnectorCD { get; private set; }

        public void Awake()
        {
            ConnectorWE = BridgeUtils.GetMostPrioritaryImplementation<IBridgeWE>();
            ConnectorCD = BridgeUtils.GetMostPrioritaryImplementation<IBridgeCD>();
            ToolsModifierControl.toolController.AddExtraToolToController<VehicleSkinsTool>();
        }
        protected override void StartActions()
        {
            base.StartActions();

            GameObjectUtils.CreateElement<VSBaseLiteUI>(UIView.GetAView().gameObject.transform, "VSBaseLiteUI");
        }
    }
}