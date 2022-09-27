extern alias VS;
extern alias WE;
using Bridge_VS2WE;
using Kwytto.Utils;
using System.Collections.Generic;
using System.IO;
using VS::VehicleSkins.Singleton;
using WE::WriteEverywhere.ModShared;
using WE::WriteEverywhere.Xml;

namespace K45_VS2WE
{
    public class BridgeWE : IBridge
    {
        public override bool WEAvailable { get; } = true;

        public override int Priority { get; } = 0;

        public override bool IsBridgeEnabled => PluginUtils.VerifyModsEnabled(new Dictionary<ulong, string> { }, new List<string>
        {
          typeof(WEFacade).Assembly.GetName().Name
        }).Count > 0;

        public override bool IsWEVehicleEditorOpen => WEFacade.IsWEVehicleEditorOpen;
        public override bool IsAnyEditorOpen => WEFacade.IsAnyEditorOpen;

        private readonly List<LayoutDescriptorVehicleXml> m_skinLayouts = new List<LayoutDescriptorVehicleXml>();
        public override string CurrentSelectionSkin => WEFacade.CurrentSelectedSkin;

        public override ushort CurrentFocusVehicle => WEFacade.CurrentGrabbedVehicleId;

        public override void ClearWELayoutRegisters() => m_skinLayouts.Clear();
        public override bool GetSkinDescriptorForVehicle<T>(VehicleInfo vehicle, ushort vehicleId, bool isParked, out T layout)
        {
            if (SkinsSingleton.instance.GetSkin(vehicle, vehicleId, isParked, out var skin))
            {
                layout = skin.wtsLayoutId < 0 ? null : m_skinLayouts[skin.wtsLayoutId] as T;
                return true;
            }
            layout = null;
            return false;
        }

        public override int RegisterWELayout(string fileContent)
        {
            var data = XmlUtils.DefaultXmlDeserialize<LayoutDescriptorVehicleXml>(fileContent);
            if (!data.IsValid())
            {
                return -1;
            }
            m_skinLayouts.Add(data);
            return m_skinLayouts.Count - 1;
        }

        public override void ApplyLayout(VehicleInfo info, string skin, string contents)
        {
            if (SkinsSingleton.instance.GetSkin(info, skin, out var material))
            {
                var newId = RegisterWELayout(contents);
                if (newId != -1)
                {
                    material.wtsLayoutId = newId;
                    File.WriteAllText(Path.Combine(SkinsSingleton.instance.GetDirectoryForAssetShared(info), $"{skin}_{SkinsSingleton.WE_FILESUFFIX}"), contents);
                }

            }
        }

        public override bool GetSkinDescriptorByName<T>(VehicleInfo vehicle, string skinName, out T layout)
        {
            if (SkinsSingleton.instance.GetSkin(vehicle, skinName, out var skin))
            {
                layout = skin.wtsLayoutId < 0 ? null : m_skinLayouts[skin.wtsLayoutId] as T;
                return !(layout is null);
            }
            layout = null;
            return false;
        }
    }
}
