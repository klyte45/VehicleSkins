using Kwytto.LiteUI;
using Kwytto.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleSkins.Localization;
using VehicleSkins.Singleton;
using VehicleSkins.Tools;

namespace VehicleSkins.UI
{
    internal class VSBaseLiteUI : GUIRootWindowBase
    {
        public static VSBaseLiteUI Instance { get; private set; }
        public static ushort GrabbedId { get; private set; }
        public static bool GrabbedIsParked { get; private set; }
        public string GrabbedTargetSkin => m_detailUI.ForcedSkinOnSelected;

        private VSInfoDetailLiteUI m_detailUI;
        public GUIColorPicker m_colorPicker;

        public static Texture2D BgTextureSubgroup;
        public static Texture2D BgTextureNote;

        public static Color bgSubgroup;
        public static Color bgNote;
        public int CurrentTab { get; private set; } = 0;
        public VehicleInfo CurrentInfo
        {
            get => SceneUtils.IsAssetEditor ? ToolsModifierControl.toolController.m_editPrefabInfo as VehicleInfo : m_currentInfo;

            set
            {
                if (!SceneUtils.IsAssetEditor)
                {
                    OnChangeInfo(value);
                }
            }
        }

        protected override bool showOverModals => false;



        public void Update()
        {
            if (Visible && Event.current.button == 1)
            {

            }
        }

        private GUIStyle m_greenButton;
        private GUIStyle m_redButton;
        internal GUIStyle GreenButton
        {
            get
            {
                if (m_greenButton is null)
                {
                    m_greenButton = new GUIStyle(Skin.button)
                    {
                        normal = new GUIStyleState()
                        {
                            background = GUIKwyttoCommons.darkGreenTexture,
                            textColor = Color.white
                        },
                        hover = new GUIStyleState()
                        {
                            background = GUIKwyttoCommons.greenTexture,
                            textColor = Color.black
                        },
                    };
                }
                return m_greenButton;
            }
        }



        internal GUIStyle RedButton
        {
            get
            {
                if (m_redButton is null)
                {
                    m_redButton = new GUIStyle(Skin.button)
                    {
                        normal = new GUIStyleState()
                        {
                            background = GUIKwyttoCommons.darkRedTexture,
                            textColor = Color.white
                        },
                        hover = new GUIStyleState()
                        {
                            background = GUIKwyttoCommons.redTexture,
                            textColor = Color.white
                        },
                    };
                }
                return m_redButton;
            }
        }


        private enum State
        {
            Normal,
            SelectVehicle
        }
        static VSBaseLiteUI()
        {
            bgSubgroup = ModInstance.Instance.ModColor.SetBrightness(.20f);

            BgTextureSubgroup = new Texture2D(1, 1);
            BgTextureSubgroup.SetPixel(0, 0, new Color(bgSubgroup.r, bgSubgroup.g, bgSubgroup.b, 1));
            BgTextureSubgroup.Apply();


            bgNote = ModInstance.Instance.ModColor.SetBrightness(.60f);
            BgTextureNote = new Texture2D(1, 1);
            BgTextureNote.SetPixel(0, 0, new Color(bgNote.r, bgNote.g, bgNote.b, 1));
            BgTextureNote.Apply();

        }


        public void Awake()
        {
            Init($"{ModInstance.Instance.SimpleName} v{ModInstance.FullVersion}", new Rect(128, 128, 680, 420), resizable: true, minSize: new Vector2(340, 260));
            Instance = this;
            m_modelFilter = new GUIFilterItemsScreen<State>(Str.VS_MODELSELECT, ModInstance.Controller, OnFilterParam, OnVehicleSet, GoTo, State.Normal, State.SelectVehicle, otherFilters: DrawExtraFilter, extraButtonsSearch: ExtraButtonsSearch);
            m_colorPicker = GameObjectUtils.CreateElement<GUIColorPicker>(transform).Init();
            m_colorPicker.Visible = false;
            m_detailUI = new VSInfoDetailLiteUI(this);
        }


        private float ExtraButtonsSearch(out bool hasChanged)
        {
            hasChanged = false;
            var currentTool = ToolsModifierControl.toolController.CurrentTool;
            if (GUILayout.Button("Picker", currentTool is VehicleSkinsTool ? GreenButton : GUI.skin.button, GUILayout.Width(100)))
            {
                var tool = ToolsModifierControl.toolController.GetComponent<VehicleSkinsTool>();
                tool.OnVehicleSelect += (x) =>
                {
                    var head = VehicleManager.instance.m_vehicles.m_buffer[x].GetFirstVehicle(x);
                    CurrentInfo = VehicleManager.instance.m_vehicles.m_buffer[head].Info;
                    m_currentState = State.Normal;
                    GrabbedId = x;
                    GrabbedIsParked = false;
                };
                tool.OnParkedVehicleSelect += (x) =>
                {
                    CurrentInfo = VehicleManager.instance.m_parkedVehicles.m_buffer[x].Info;
                    m_currentState = State.Normal;

                    GrabbedId = x;
                    GrabbedIsParked = true;
                };

                GrabbedId = 0;
                GrabbedIsParked = false;
                ToolsModifierControl.SetTool<VehicleSkinsTool>();
            }
            return 0;
        }
        public void Start() => Visible = false;
        internal void Reset()
        {
            m_currentState = State.Normal;
            CurrentTab = 0;
        }

        private State m_currentState = State.Normal;
        private void GoTo(State newState) => m_currentState = newState;
        protected override bool requireModal => false;

        private void OnChangeInfo(VehicleInfo value)
        {
            if (m_currentInfo != value)
            {
                m_currentInfo = value;
                if (!(value is null))
                {
                    m_currentInfoList = new List<VehicleInfo>()
                        {
                            m_currentInfo
                        };
                    if (!(m_currentInfo.m_trailers is null))
                    {
                        m_currentInfoList.AddRange(m_currentInfo.m_trailers.Select(x => x.m_info).Distinct().Where(x => x != m_currentInfo));
                    }
                    CurrentTab = 0;
                    if (SceneUtils.IsAssetEditor)
                    {
                        m_isPackageLoaded = SkinsSingleton.instance.GetDirectoryForAssetOwn(m_currentInfo) != null;
                    }

                }
            }
        }


        private List<VehicleInfo> m_currentInfoList;
        private VehicleInfo m_currentInfo;
        private bool m_isPackageLoaded;
        private Vector2 m_horizontalScroll;

        protected override void DrawWindow()
        {
            var area = new Rect(5, 25, WindowRect.width - 10, WindowRect.height - 25);
            using (new GUILayout.AreaScope(area))
            {
                switch (m_currentState)
                {
                    case State.Normal:
                        DrawNormal(area.size);
                        break;
                    case State.SelectVehicle:
                        m_modelFilter.DrawSelectorView(area.height);
                        break;
                }
            }
        }

        protected void DrawNormal(Vector2 size)
        {
            var inf = CurrentInfo;
            var isAssetEditor = SceneUtils.IsAssetEditor;
            if (isAssetEditor)
            {
                OnChangeInfo(inf);
                GUILayout.Label(inf?.GetUncheckedLocalizedTitle() is string vehicleName
                    ? m_isPackageLoaded
                        ? $"Saving all content under name: <color=yellow>{inf.name}</color>. Save the asset and reload editor to change it."
                        : $"<color=yellow>{vehicleName}</color> is a new asset not loaded in the editor. Reload editor to edit their skins!"
                    : "<Editing asset is not a vehicle!>");
                if (!m_isPackageLoaded)
                {
                    inf = null;
                }
            }
            else
            {
                m_modelFilter.DrawButton(size.x, inf?.GetUncheckedLocalizedTitle());
            }

            if (inf)
            {
                var headerArea = new Rect(0, 25, size.x, 50); ;
                var bodyArea = new Rect(0, 75, size.x, size.y - 75);
                using (new GUILayout.AreaScope(headerArea))
                {
                    if (!isAssetEditor && GUILayout.Button(Str.VS_RELOADALLSKINS))
                    {
                        SkinsSingleton.instance.ReloadSkins();
                    }
                    using (var scope = new GUILayout.ScrollViewScope(m_horizontalScroll))
                    {
                        CurrentTab = GUILayout.SelectionGrid(CurrentTab, m_currentInfoList.Select((_, i) => i == 0 ? "Head" : $"Trailer {i}").ToArray(), m_currentInfoList.Count, GUILayout.MinWidth(40));
                        m_horizontalScroll = scope.scrollPosition;
                    }
                }
                using (new GUILayout.AreaScope(bodyArea, BgTextureSubgroup, GUI.skin.box))
                {
                    m_detailUI.DoDraw(new Rect(default, bodyArea.size), m_currentInfoList[CurrentTab], m_currentInfoList[0], CurrentTab, m_currentInfoList.Count);
                }
            }
        }
        protected override void OnWindowDestroyed() => Destroy(m_colorPicker);

        public string GetSelectedSkinFor(VehicleInfo info) => m_currentInfoList?.IndexOf(info) is int idx && idx >= 0 ? m_detailUI.GetCurentSelectedSkinAtIndex(idx) : null;

        #region Model Select


        private GUIFilterItemsScreen<State> m_modelFilter;


        private readonly Wrapper<IIndexedPrefabData[]> m_searchResultWrapper = new Wrapper<IIndexedPrefabData[]>();
        private readonly List<IIndexedPrefabData> m_cachedResultList = new List<IIndexedPrefabData>();
        private bool m_searchOnlyWithActiveRules;
        private IEnumerator OnFilterParam(string searchText, Action<string[]> setResult)
        {
            yield return VehiclesIndexes.instance.BasicInputFiltering(searchText, m_searchResultWrapper);
            m_cachedResultList.Clear();
            m_cachedResultList.AddRange(m_searchResultWrapper.Value.Cast<IndexedPrefabData<VehicleInfo>>()
                .Where(x => x.Prefab.m_placementStyle == ItemClass.Placement.Automatic &&
                (!m_searchOnlyWithActiveRules || SkinsSingleton.instance.HasSkins(x.Prefab))
                )
                .OrderBy(x => x.Info.GetUncheckedLocalizedTitle()).Take(500).Cast<IIndexedPrefabData>()); ;
            setResult(m_cachedResultList.Select(x => x.Info.GetUncheckedLocalizedTitle()).ToArray());
        }
        private float DrawExtraFilter(out bool hasChanged)
        {
            hasChanged = false;
            if (m_searchOnlyWithActiveRules != GUILayout.Toggle(m_searchOnlyWithActiveRules, Str.VS_ACTIVERULESONLY))
            {
                m_searchOnlyWithActiveRules = !m_searchOnlyWithActiveRules;
                hasChanged = true;
            }
            return 12;
        }

        private void OnVehicleSet(int selectLayout, string _ = null) => CurrentInfo = (m_cachedResultList[selectLayout].Info as VehicleInfo);
        #endregion



    }
}
