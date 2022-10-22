using Kwytto.Utils;
using System.Reflection;
using UnityEngine;
using VehicleSkins.Singleton;
using VehicleSkins.UI;

namespace VehicleSkins.Overrides
{
    public class VehicleOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        #region Events

        public static void PreRender(ref VehicleInfo info, ref InstanceID id, ref bool __state) => __state = SkinsSingleton.instance.SetSkin(info, id.Vehicle, ref VehicleManager.instance.m_vehicles.m_buffer[id.Vehicle]);
        public static void PreRenderParked(ref VehicleParked __instance, ref ushort parkedVehicleID, ref bool __state) => __state = SkinsSingleton.instance.SetSkin(__instance.Info, parkedVehicleID);
        public static void PostRenderParked(ref VehicleParked __instance, ref ushort parkedVehicleID, ref bool __state)
        {
            if (__state)
            {
                SkinsSingleton.instance.SetSkin(__instance.Info, "");
            }
            if (VSBaseLiteUI.Instance.Visible
                //&& VSBaseLiteUI.LockSelection 
                && VSBaseLiteUI.GrabbedIsParked
                && VSBaseLiteUI.GrabbedId == parkedVehicleID)
            {
                ToolsModifierControl.cameraController.m_targetPosition.x = __instance.m_position.x;
                ToolsModifierControl.cameraController.m_targetPosition.z = __instance.m_position.z;
                targetHeight = __instance.m_position.y + __instance.Info.m_mesh.bounds.center.y;
                lastFrameOverride = SimulationManager.instance.m_currentTickIndex;
            }
        }

        public static void PostRender(ref VehicleInfo info, ref Vector3 position, ref InstanceID id, ref bool __state)
        {
            if (__state)
            {
                SkinsSingleton.instance.SetSkin(info, "");
            }
            if (VSBaseLiteUI.Instance.Visible
                //&& VSBaseLiteUI.LockSelection 
                && !VSBaseLiteUI.GrabbedIsParked
                && VSBaseLiteUI.GrabbedId == id.Vehicle)
            {
                ToolsModifierControl.cameraController.m_targetPosition.x = position.x;
                ToolsModifierControl.cameraController.m_targetPosition.z = position.z;
                targetHeight = position.y + info.m_mesh.bounds.center.y;
                lastFrameOverride = SimulationManager.instance.m_currentTickIndex;
            }
        }

        #endregion

        #region Camera

        private static uint lastFrameOverride;
        private static float targetHeight;
        public static void AfterUpdateTransformOverride(CameraController __instance)
        {
            if (ModInstance.Controller.ConnectorWE.IsAnyEditorOpen || (LoadingManager.instance.m_loadingComplete && SimulationManager.instance.m_currentTickIndex - lastFrameOverride > 24))
            {
                return;
            }
            __instance.m_minDistance = 1;

            Vector3 vector = __instance.transform.position;
            vector.y = targetHeight + (Mathf.Sin(__instance.m_currentAngle.y * Mathf.Deg2Rad) * __instance.m_targetSize);
            __instance.transform.position = vector;
        }

        #endregion

        #region Hooking

        public void Awake()
        {
            RedirectorInstance = GameObjectUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Vehicle Overrides");
            #region Loading Vehicle Overrides
            MethodInfo preRender = typeof(VehicleOverrides).GetMethod("PreRender", RedirectorUtils.allFlags);
            MethodInfo postRender = typeof(VehicleOverrides).GetMethod("PostRender", RedirectorUtils.allFlags);
            MethodInfo preRenderPkd = typeof(VehicleOverrides).GetMethod("PreRenderParked", RedirectorUtils.allFlags);
            MethodInfo postRenderPkd = typeof(VehicleOverrides).GetMethod("PostRenderParked", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(Vehicle).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~BindingFlags.Instance), preRender, postRender);
            RedirectorInstance.AddRedirect(typeof(VehicleParked).GetMethod("RenderInstance", RedirectorUtils.allFlags), preRenderPkd, postRenderPkd);
            #endregion

        }
        #endregion
    }
}