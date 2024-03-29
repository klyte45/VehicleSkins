﻿using Kwytto.Tools;
using System;
using UnityEngine;

namespace VehicleSkins.Tools
{

    public class VehicleSkinsTool : KwyttoVehicleToolBase
    {
        public event Action<ushort> OnVehicleSelect;
        public event Action<ushort> OnParkedVehicleSelect;

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (m_hoverVehicle != 0)
            {
                m_trailersAlso = false;
                Color toolColor = m_hoverColor;
                RenderOverlay(cameraInfo, toolColor, m_hoverVehicle, false);
                return;
            }
            if (m_hoverParkedVehicle != 0)
            {
                m_trailersAlso = false;
                Color toolColor = m_hoverColor;
                RenderOverlay(cameraInfo, toolColor, m_hoverParkedVehicle, true);
                return;
            }

        }

        protected override void OnLeftClick()
        {
            if (m_hoverVehicle != 0 && !(OnVehicleSelect is null))
            {
                OnVehicleSelect.Invoke(m_hoverVehicle);
                ToolsModifierControl.SetTool<DefaultTool>();
            }
            else if (m_hoverParkedVehicle != 0 && !(OnParkedVehicleSelect is null))
            {
                OnParkedVehicleSelect.Invoke(m_hoverParkedVehicle);
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        protected override void OnDisable()
        {
            OnVehicleSelect = null;
            base.OnDisable();
        }

    }

}
