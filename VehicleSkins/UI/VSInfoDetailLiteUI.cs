using ColossalFramework;
using ColossalFramework.Globalization;
using Kwytto.LiteUI;
using Kwytto.Utils;
using System.IO;
using System.Linq;
using UnityEngine;
using VehicleSkins.Data;
using VehicleSkins.Localization;
using VehicleSkins.Singleton;

namespace VehicleSkins.UI
{
    internal class VSInfoDetailLiteUI
    {
        public VSInfoDetailLiteUI(GUIRootWindowBase root) => m_root = root;

        private GUIRootWindowBase m_root;
        private VehicleInfo m_currentInfo;
        private VehicleInfo m_currentParent;
        private int m_trailersCount;
        private bool m_isVanillaAsset;
        private SkinsSingleton.MaterialContainer[] m_currentSkinsMaterials;
        private string[] m_currentSkinsMaterialsNames;
        private Vector2 m_scrollSkins;
        private Coroutine m_currentReloadCoroutine;
        private string[] m_currentSelectedSkin;

        public string GetCurentSelectedSkinAtIndex(int idx) => m_currentSelectedSkin?[idx];

        internal void DoDraw(Rect rect, VehicleInfo currentInfo, VehicleInfo parentInfo, int currentInfoIndex, int infoTrailersSize)
        {
            if (m_currentInfo != currentInfo)
            {
                OnChangeInfo(currentInfo, parentInfo, infoTrailersSize);
                if (SceneUtils.IsAssetEditor)
                {
                    ReloadSkinsForCurrentInfo();
                }
            }
            if (m_currentInfo is null)
            {
                return;
            }
            if (m_currentSkinsMaterials.Length == 0)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(Str.VS_THEREARENOSKINS);
                    if (GUILayout.Button(Str.VS_CREATENEWSKIN))
                    {
                        //CallCreateSkinPrompt();
                    }
                }
                if (GUILayout.Button(Str.VS_RELOADASSETSKINS))
                {
                    ReloadSkinsForCurrentInfo();
                }
            }
            else
            {
                bool isLocalConfig = VSSkinSelectionData.Instance.GeneralExcludedSkins.ContainsKey(currentInfo.name);
                bool isAssetEditor = SceneUtils.IsAssetEditor;
                if (!isAssetEditor)
                {
                    GUILayout.Label(Locale.Get(isLocalConfig ? "K45_VS_USINGSAVEGAMECONFIG" : "K45_VS_USINGSHAREDCONFIG"));
                }
                else
                {
                    GUILayout.Label(Locale.Get(isLocalConfig ? "K45_VS_DEFAULTSNOTSAVED" : "K45_VS_DEFAULTSSAVED"));
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Str.VS_CREATENEWSKIN))
                    {
                        //CallCreateSkinPrompt();
                    }
                    if (GUILayout.Button(Str.VS_RELOADASSETSKINS))
                    {
                        ReloadSkinsForCurrentInfo();
                    }
                }
                if (!isAssetEditor)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (isLocalConfig && GUILayout.Button(Locale.Get(isAssetEditor ? "K45_VS_SAVESKINENABLESELECTION" : "K45_VS_EXPORTASSHARED")))
                        {
                            DoExportShared();
                        }
                        if (isLocalConfig && GUILayout.Button(Locale.Get(isAssetEditor ? "K45_VS_DISCARDCHANGES" : "K45_VS_DISCARDSAVEGAME")))
                        {
                            DiscardLocal();
                        }
                    }
                }
                if (!m_isVanillaAsset && !isAssetEditor)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(Str.VS_EXPORTASSET))
                        {
                            DoExportAsset();
                        }
                    }
                }

                using (var scroll = new GUILayout.ScrollViewScope(m_scrollSkins))
                {
                    if (!isAssetEditor)
                    {
                        GUILayout.Label(Str.VS_SOURCECOLOR_LEGEND);
                    }
                    foreach (var skin in m_currentSkinsMaterials)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            var currentActive = skin.Active;
                            if (currentActive != GUILayout.Toggle(currentActive, $"<color={GetColornameFromSource(skin.source)}>{skin.skinName.TrimToNull() ?? "<DEFAULT>"}</color>"))
                            {
                                skin.Active = !currentActive;
                            }
                            if (isAssetEditor && GUILayout.Button("PREVIEW", skin.skinName == m_currentSelectedSkin[currentInfoIndex] ? VSBaseLiteUI.Instance.GreenButton : GUI.skin.button, GUILayout.Width(60)))
                            {
                                m_currentSelectedSkin[currentInfoIndex] = skin.skinName;
                            }
                        }
                    }
                    m_scrollSkins = scroll.scrollPosition;
                }
            }
        }

        private string GetColornameFromSource(SkinsSingleton.MaterialContainer.Source source)
        {
            switch (source)
            {
                case SkinsSingleton.MaterialContainer.Source.ORIGINAL: return "gray";
                case SkinsSingleton.MaterialContainer.Source.WORKSHOP: return "green";
                case SkinsSingleton.MaterialContainer.Source.SHARED: return "yellow";
                default: return "purple";
            }
        }

        private void DiscardLocal()
        {
            SkinsSingleton.instance.DiscardLocal(m_currentInfo);
            OnChangeInfo(m_currentInfo, m_currentParent);
        }

        private void DoExportShared() => SkinsSingleton.instance.ExportCurrentAsShared(m_currentInfo, SceneUtils.IsAssetEditor);
        private void DoExportAsset()
        {
            if (Directory.Exists(SkinsSingleton.instance.GetDirectoryForAssetOwn(m_currentInfo)))
            {
                KwyttoDialog.ShowModal(new KwyttoDialog.BindProperties
                {
                    title = Str.VS_EXPORTTOASSET_TITLE,
                    message = Str.VS_EXPORTTOASSET_MSG,
                    buttons = new[]
                    {
                        KwyttoDialog.SpaceBtn,
                        new KwyttoDialog.ButtonDefinition
                        {
                            onClick =()=> { SkinsSingleton.instance.ExportSharedsAsOwn(m_currentInfo, true); return true; },
                            title = Locale.Get("YES"),
                        },
                        new KwyttoDialog.ButtonDefinition
                        {
                            onClick =()=> { SkinsSingleton.instance.ExportSharedsAsOwn(m_currentInfo, false); return true; },
                            title = Locale.Get("NO"),
                        },
                        new KwyttoDialog.ButtonDefinition
                        {
                            onClick =()=>  true,
                            title = Locale.Get("CANCEL"),
                        },

                    }
                });
            }
            else
            {
                SkinsSingleton.instance.ExportSharedsAsOwn(m_currentInfo, false);
            }
            KwyttoDialog.ShowModal(new KwyttoDialog.BindProperties
            {
                title = Str.VS_EXPORTTOASSET_TITLE,
                message = Str.VS_EXPORTTOASSET_MSGSUC,
                buttons = new[]
                {
                    KwyttoDialog.SpaceBtn,
                    new KwyttoDialog.ButtonDefinition
                    {
                        onClick = () => { Utils.OpenInFileBrowser(SkinsSingleton.instance.GetDirectoryForAssetOwn(m_currentInfo)); return true; },
                        title = Str.VS_VIEWFILES,
                    },
                    new KwyttoDialog.ButtonDefinition
                    {
                        onClick = () => { SkinsSingleton.instance.ExportSharedsAsOwn(m_currentInfo, false); return true; },
                        title = Locale.Get("EXCEPTION_OK"),
                        style = KwyttoDialog.ButtonStyle.White
                    },
                }
            });
        }

        //private void CallCreateSkinPrompt(string errorMsg = "")
        //    => KwyttoDialog.ShowModalPromptText(new KwyttoDialog.BindProperties
        //    {
        //        showButton1 = true,
        //        showButton2 = true,
        //        textButton1 = Locale.Get("CREATE"),
        //        textButton2 = Locale.Get("CANCEL"),
        //        message = (errorMsg + "\n" + Str.VS_CREATESKIN_MESSAGE).Trim()
        //    }, OnCreateModalEnded);

        //private bool OnCreateModalEnded(int exitCode, string newName)
        //{
        //    if (exitCode == 1)
        //    {
        //        newName = newName.TrimToNull();
        //        if (newName.IsNullOrWhiteSpace() || m_currentSkinsMaterials.Any(x => x.skinName == newName))
        //        {
        //            CallCreateSkinPrompt(Str.VS_CREATESKIN_ERROR_ALREADYEXISTS);
        //            return true;
        //        }
        //        var dir = SkinsSingleton.instance.CreateSkin(m_currentInfo, newName, () => OnChangeInfo(m_currentInfo, m_currentParent), !SceneUtils.IsAssetEditor);

        //        KwyttoDialog.ShowModal(new KwyttoDialog.BindProperties
        //        {
        //            message = Str.VS_CREATESKIN_MSGSUC,
        //            textButton1 = Locale.Get("EXCEPTION_OK"),
        //            textButton2 = Str.VS_VIEWFILES,
        //            showButton1 = true,
        //            showButton2 = true
        //        }, (x) =>
        //        {
        //            switch (x)
        //            {
        //                case 2:
        //                    Utils.OpenInFileBrowser(dir);
        //                    return false;
        //            }
        //            return true;
        //        });
        //    }
        //    return true;
        //}

        private void OnChangeInfo(VehicleInfo currentInfo, VehicleInfo parentInfo) => OnChangeInfo(currentInfo, parentInfo, m_trailersCount);

        private void OnChangeInfo(VehicleInfo currentInfo, VehicleInfo parentInfo, int sizeTrailers)
        {
            m_currentInfo = currentInfo;
            if (m_currentParent != parentInfo)
            {
                m_trailersCount = sizeTrailers;
                m_currentSelectedSkin = new string[sizeTrailers];
            }

            m_currentParent = parentInfo;
            m_isVanillaAsset = KFileUtils.GetRootFolderForK45(parentInfo) is null;
            m_currentSkinsMaterials = SkinsSingleton.instance.GetAvailableSkinsMaterial(currentInfo).OrderBy(x => x.skinName).ToArray();
            m_currentSkinsMaterialsNames = m_currentSkinsMaterials.Select(x => x.skinName.TrimToNull()).ToArray();
            m_currentReloadCoroutine = null;
        }

        private void ReloadSkinsForCurrentInfo()
        {
            if (!(m_currentReloadCoroutine is null))
            {
                SkinsSingleton.instance.StopCoroutine(m_currentReloadCoroutine);
            }
            m_currentReloadCoroutine = SkinsSingleton.instance.StartCoroutine(SkinsSingleton.instance.RealoadSkinsOfInfo(m_currentInfo, () => OnChangeInfo(m_currentInfo, m_currentParent)));
        }
    }
}