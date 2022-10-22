using Kwytto.Utils;
using System;
using UnityEngine;

namespace VehicleSkins.Overrides
{
    public class CameraOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; set; }

        private void Awake()
        {
            RedirectorInstance = GameObjectUtils.CreateElement<Redirector>(transform);
            var overrideVeh = typeof(VehicleOverrides).GetMethod("AfterUpdateTransformOverride", ReflectionUtils.allFlags);
            var src = typeof(CameraController).GetMethod("UpdateTransform", ReflectionUtils.allFlags);
            var src2 = Type.GetType("CameraPositions.Detours.CameraControllerDetour, CameraPositions.dll")?.GetMethod("UpdateTransform", ReflectionUtils.allFlags);
            foreach (var m in new[] { overrideVeh })
            {
                RedirectorInstance.AddRedirect(src, null, m);
                if (src2 != null)
                {
                    LogUtils.DoLog("CameraPositions was found!");
                    RedirectorInstance.AddRedirect(src2, null, m);
                }
            }

        }
    }
}
