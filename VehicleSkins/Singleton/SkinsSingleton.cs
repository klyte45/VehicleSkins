
using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using Kwytto.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using VehicleSkins.Data;
using VehicleSkins.UI;
using static VehicleSkins.Singleton.SkinsSingleton.MaterialContainer;

namespace VehicleSkins.Singleton
{
    public class SkinsSingleton : Singleton<SkinsSingleton>
    {
        public static string LastStatusReloading { get; private set; }
        public class MaterialContainer : IDisposable
        {
            public enum Source
            {
                ORIGINAL,
                WORKSHOP,
                SHARED
            }

            public readonly string assetName;
            public readonly string skinName;
            public readonly Material main;
            public readonly Material lod;
            public bool defaultActive;
            public readonly Source source;
            public int wtsLayoutId;
            public bool Active
            {
                get => VSSkinSelectionData.Instance.GeneralExcludedSkins.TryGetValue(assetName, out var list) ? !list.Contains(skinName.TrimToNull()) : defaultActive;
                set
                {
                    if (value)
                    {
                        if (!VSSkinSelectionData.Instance.GeneralExcludedSkins.TryGetValue(assetName, out var list))
                        {
                            VSSkinSelectionData.Instance.GeneralExcludedSkins[assetName] = new HashSet<string>(instance.m_skins[assetName].Where(x => !x.Value.defaultActive && x.Key != skinName).Select(x => x.Key));
                            instance.m_cachedSkins.Clear();
                        }
                        else if (list.Contains(skinName))
                        {
                            list.Remove(skinName);
                            instance.m_cachedSkins.Clear();
                        }
                    }
                    else
                    {
                        if (!VSSkinSelectionData.Instance.GeneralExcludedSkins.TryGetValue(assetName, out var list))
                        {
                            VSSkinSelectionData.Instance.GeneralExcludedSkins[assetName] = new HashSet<string>(new[]
                            {
                                skinName
                            }.Union(instance.m_skins[assetName].Where(x => !x.Value.defaultActive).Select(x => x.Key)));
                            instance.m_cachedSkins.Clear();
                        }
                        else if (!list.Contains(skinName))
                        {
                            list.Add(skinName);
                            instance.m_cachedSkins.Clear();
                        }
                    }
                }
            }

            public MaterialContainer(string assetName, string skinName, Material main, Material lod, int wtsLayoutId, Source source, bool defaultActive = true)
            {
                this.assetName = assetName;
                this.skinName = skinName;
                this.main = main;
                this.lod = lod;
                this.wtsLayoutId = wtsLayoutId;
                this.defaultActive = defaultActive;
                this.source = source;
            }

            public void Dispose()
            {
                Destroy(main);
                Destroy(lod);
            }
        }

        public const string XYS_MAP = "_XYSMap";
        public const string ACI_MAP = "_ACIMap";
        public const string MAIN_FILESUFFIX = "d.png";
        public const string XYS_FILESUFFIX = "xys.png";
        public const string ACI_FILESUFFIX = "aci.png";
        //public const string MAIN_LOD_FILESUFFIX = "dLod.png";
        //public const string XYS_LOD_FILESUFFIX = "xysLod.png";
        //public const string ACI_LOD_FILESUFFIX = "aciLod.png";
        public const string WE_FILESUFFIX = "WELayout.xml";

        public const string EXCLUDED_SKINS_FILENAME = "_ExcludedSkins.txt";
        public const string EXCLUDED_DEFAULT_SKIN_STRING = "<\\DEFAULT/>";

        public bool IsLoading => m_reloadCoroutine != null;

        private Coroutine m_reloadCoroutine;
        private static ActionThread m_reloadThread;
        public event Action OnReloadCoroutineDone;

        internal void DiscardLocal(VehicleInfo info)
        {
            VSSkinSelectionData.Instance.GeneralExcludedSkins.Remove(info.name);
            m_cachedSkins.Clear();
        }

        internal void ExportCurrentAsShared(VehicleInfo info)
        {
            if (VSSkinSelectionData.Instance.GeneralExcludedSkins.TryGetValue(info.name, out var list))
            {
                var folder = GetDirectoryForAssetShared(info);
                File.WriteAllLines(Path.Combine(folder, EXCLUDED_SKINS_FILENAME), list.Select(x => x ?? EXCLUDED_DEFAULT_SKIN_STRING).ToArray());
                StartCoroutine(RealoadSkinsOfInfo(info));
                foreach (var entry in m_skins[info.name])
                {
                    entry.Value.Active = !list.Any(x => x != "" && (x ?? "") == entry.Key);
                }
            }
        }
        internal void ExportSharedsAsOwn(VehicleInfo info, bool deleteExisting)
        {
            if (GetDirectoryForAssetOwn(info) is string targFolder)
            {
                if (deleteExisting && Directory.Exists(targFolder))
                {
                    Directory.Delete(targFolder, true);
                }
                KFileUtils.EnsureFolderCreation(targFolder);

                if (GetDirectoryForAssetShared(info) is string sharedSrc && Directory.Exists(sharedSrc))
                {
                    foreach (var suffix in ALLOWED_FILES)
                    {
                        foreach (var subfile in Directory.GetFiles(sharedSrc, $"*{suffix}"))
                        {
                            var targFile = Path.Combine(targFolder, Path.GetFileName(subfile));
                            File.Delete(targFile);
                            File.Copy(subfile, targFile);
                        }
                    }
                    if (VSSkinSelectionData.Instance.GeneralExcludedSkins.TryGetValue(info.name, out var list))
                    {
                        File.WriteAllLines(Path.Combine(targFolder, EXCLUDED_SKINS_FILENAME), list.Select(x => x ?? EXCLUDED_DEFAULT_SKIN_STRING).ToArray());
                        StartCoroutine(RealoadSkinsOfInfo(info));
                    }
                }
            }
        }

        private Dictionary<string, Dictionary<string, MaterialContainer>> m_skins = new Dictionary<string, Dictionary<string, MaterialContainer>>();
        internal string[] GetAvailableSkins(VehicleInfo info) => m_skins.TryGetValue(info.name, out var skins) ? skins.Keys.OrderBy(x => x).ToArray() : (new string[0]);
        internal MaterialContainer[] GetAvailableSkinsMaterial(VehicleInfo info) => GetAvailableSkinsMaterial(info.name);
        internal MaterialContainer[] GetAvailableSkinsMaterial(string assetName) => m_skins.TryGetValue(assetName, out var skins) ? skins.Values.ToArray() : (new MaterialContainer[0]);

        internal bool HasSkins(VehicleInfo info) => m_skins.TryGetValue(info.name, out var skins) && skins.Count > 1;

        public string GetDirectoryForAssetShared(PrefabInfo info) => Path.Combine(VSMainController.SKINS_FOLDER, info.name);
        public static string GetDirectoryForAssetOwn(PrefabInfo info) => KFileUtils.GetRootFolderForK45(info) is string str ? Path.Combine(Path.Combine(str, VSMainController.EXTRA_SPRITES_FILES_FOLDER_ASSETS), PrefabUtils.GetAssetFromPrefab(info).name.Split('.').Last()) : null;

        public void ReloadSkins()
        {
            if (!(m_reloadCoroutine is null))
            {
                return;
            }
            m_reloadCoroutine = StartCoroutine(ReloadSkins_Coroutine());
        }

        private IEnumerator ReloadSkins_Coroutine()
        {
            if (!(m_reloadThread is null))
            {
                yield break;
            }
            m_skins.Values.ForEach(x => x.Where(y => y.Key != "").ForEach(y => y.Value.Dispose()));
            m_cachedSkins.Clear();
            m_skins = new Dictionary<string, Dictionary<string, MaterialContainer>>();
            try
            {
                ModInstance.Controller.ConnectorWE.ClearWELayoutRegisters();
                var prefabs = VehiclesIndexes.instance.PrefabsData;
                m_reloadThread = ThreadHelper.CreateThread(() => ReloadSkins_Thread(prefabs, m_skins));
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog("Error reloading skins! (top Block)\n" + e);

                OnReloadCoroutineDone = null;
                m_reloadCoroutine = null;
                yield break;
            }
            yield return new WaitWhile(() => m_reloadThread != null);
            try
            {
                m_cachedSkins.Clear();
                OnReloadCoroutineDone?.Invoke();
                VSBaseLiteUI.Instance.Reset();
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog("Error reloading skins! (bottom Block)\n" + e);
            }
            OnReloadCoroutineDone = null;
            m_reloadCoroutine = null;
        }

        private static void ReloadSkins_Thread(Dictionary<string, IIndexedPrefabData> prefabsData, Dictionary<string, Dictionary<string, MaterialContainer>> m_skins)
        {
            try
            {
                var counter = 0;
                foreach (var vehicleInfo in prefabsData.Values)
                {
                    counter++;
                    if (vehicleInfo.Info is VehicleInfo vi)
                    {
                        LoadFromWorkshopAssetFolder(vi, m_skins, counter, prefabsData.Values.Count);
                        if (vi.m_trailers != null)
                        {
                            foreach (var trailer in vi.m_trailers)
                            {
                                LoadFromWorkshopAssetFolder(trailer.m_info, m_skins, counter, prefabsData.Values.Count);
                            }
                        }
                    }
                }
                KFileUtils.EnsureFolderCreation(VSMainController.SKINS_FOLDER);
                var models = Directory.GetDirectories(VSMainController.SKINS_FOLDER);
                Dispatcher.main.Dispatch(() => LogAndUpdateStatusInfo($"Found {models.Length} folders for skins"));
                counter = 0;
                foreach (string folderFull in models)
                {
                    counter++;
                    var vehicleName = Path.GetFileName(folderFull);
                    try
                    {
                        Dispatcher.main.Dispatch(() => LogAndUpdateStatusInfo($"[{counter}/{models.Length}] Trying load folder {vehicleName}"));
                        if (prefabsData.TryGetValue(vehicleName, out var data))
                        {
                            LoadFromFolder(folderFull, data.Info as VehicleInfo, Source.SHARED, m_skins, counter, models.Length);
                            Dispatcher.main.Dispatch(() => LogAndUpdateStatusInfo($"[{counter}/{models.Length}] Folder {vehicleName} loaded"));
                        }
                        else
                        {
                            Dispatcher.main.Dispatch(() => LogErrorAndUpdateStatusInfo($"[{counter}/{models.Length}] Folder {vehicleName} failed! Info not found!"));
                        }
                    }
                    catch (Exception e)
                    {
                        Dispatcher.main.Dispatch(() => LogErrorAndUpdateStatusInfo($"[{counter}/{models.Length}] Error while reading folder {vehicleName}: {e.Message}", e));
                    }
                }
                Dispatcher.main.Dispatch(() =>
                {
                    LogAndUpdateStatusInfo($"Scan thread ended!");
                    LogUtils.FlushBuffer();
                });
            }
            catch (Exception e)
            {
                LogErrorAndUpdateStatusInfo($"Error executing loading thread!", e);
                Thread.Sleep(5000);
            }
            finally
            {
                m_reloadThread = null;
            }

        }

        private static void LogAndUpdateStatusInfo(string status)
        {
            LogUtils.DoInfoLog(status);
            LastStatusReloading = status;
        }

        private static void LogErrorAndUpdateStatusInfo(string status, Exception e = null)
        {
            LogUtils.DoErrorLog(status + "\n" + e);
            LastStatusReloading = $"<color=#FF0000>{status}</color>";
        }

        private static void LoadFromWorkshopAssetFolder(VehicleInfo vehicleInfo, Dictionary<string, Dictionary<string, MaterialContainer>> m_skins, int currentIdx, int total)
        {
            Dispatcher.main.Dispatch(() => LogAndUpdateStatusInfo($"[{currentIdx}/{total}] Scanning {vehicleInfo} asset files!"));
            var assetFolder = GetDirectoryForAssetOwn(vehicleInfo);
            if (assetFolder.TrimToNull() != null)
            {
                LoadFromFolder(assetFolder, vehicleInfo, Source.WORKSHOP, m_skins, currentIdx, total);
            }
        }

        private static void LoadFromFolder(string folder, VehicleInfo info, Source source, Dictionary<string, Dictionary<string, MaterialContainer>> m_skins, int currentIdx, int total)
        {
            if (info is null || !Directory.Exists(folder))
            {
                if (ModInstance.DebugMode)
                    Dispatcher.main.Dispatch(() => LogUtils.DoLog($"[{currentIdx}/{total}] Folder doesn't exists for asset '{info}': {folder}"));
                return;
            }
            var assetName = info.name;
            if (ModInstance.DebugMode)
                Dispatcher.main.Dispatch(() => LogUtils.DoLog($"[{currentIdx}/{total}] Processing {assetName} @ {folder}"));
            if (source == Source.WORKSHOP || !m_skins.ContainsKey(assetName))
            {
                m_skins[assetName] = new Dictionary<string, MaterialContainer>();
            }
            var baseMaterial = info.m_material;
            var baseLodMaterial = info.m_lodMaterial;
            var subFiles = Directory.GetFiles(folder);
            var disabledSkins = subFiles.Where(x => Path.GetFileName(x) == EXCLUDED_SKINS_FILENAME).SelectMany(x => File.ReadAllLines(x)).Where(x => !(x.TrimToNull() is null));

            m_skins[assetName][""] = new MaterialContainer
            (
                assetName: assetName,
                skinName: null,
                main: baseMaterial,
                lod: baseLodMaterial,
                wtsLayoutId: -1,
                source: Source.ORIGINAL,
                defaultActive: !disabledSkins.Contains(EXCLUDED_DEFAULT_SKIN_STRING)
            );


            var fileGroups = subFiles.Select(x =>
            {
                var arrName = x.Split('_');
                return Tuple.New(x, Path.GetFileName(string.Join("_", arrName.Take(arrName.Length - 1).ToArray())), arrName.Last());
            }).Where(x => x.Second.TrimToNull() != null && ALLOWED_FILES.Contains(x.Third)).GroupBy(x => x.Second);
            foreach (var group in fileGroups)
            {
                bool hasLodFiles = group.Any(x => x.Third.EndsWith("Lod.png"));
                Material normalMaterial = null, lodMaterial = null;
                Dispatcher.main.Dispatch(() =>
                {
                    normalMaterial = new Material(baseMaterial);
                    lodMaterial = new Material(baseLodMaterial);
                });
                bool hasAnyValidFile = false;
                var wtsLayoutId = -1;
                foreach (var subFile in group)
                {
                    hasAnyValidFile = true;
                    var filePath = Path.Combine(folder, subFile.First);
                    if (subFile.First.EndsWith(".png"))
                    {
                        Texture2D tex = Dispatcher.main.Dispatch(() => TextureAtlasUtils.LoadTextureFromFile(filePath, linear: false)).result;
                        if (tex is null)
                        {
                            continue;
                        }
                        switch (subFile.Third)
                        {
                            case MAIN_FILESUFFIX:
                                normalMaterial.mainTexture = tex;
                                //if (!hasLodFiles)
                                //{
                                //    lodMaterial.mainTexture = tex;
                                //}
                                break;
                            case XYS_FILESUFFIX:
                                normalMaterial.SetTexture(XYS_MAP, tex);
                                //if (!hasLodFiles)
                                //{
                                //    lodMaterial.SetTexture(XYS_MAP, tex);
                                //}
                                break;
                            case ACI_FILESUFFIX:
                                normalMaterial.SetTexture(ACI_MAP, tex);
                                //if (!hasLodFiles)
                                //{
                                //    lodMaterial.SetTexture(ACI_MAP, tex);
                                //}
                                break;
                                //case MAIN_LOD_FILESUFFIX:
                                //    lodMaterial.mainTexture = tex;
                                //    break;
                                //case XYS_LOD_FILESUFFIX:
                                //    lodMaterial.SetTexture(XYS_MAP, tex);
                                //    break;
                                //case ACI_LOD_FILESUFFIX:
                                //    lodMaterial.SetTexture(ACI_MAP, tex);
                                //    break;
                        }
                    }
                    else
                    {
                        switch (subFile.Third)
                        {
                            case WE_FILESUFFIX:
                                wtsLayoutId = ReadWEFile(wtsLayoutId, filePath);
                                break;
                        }
                    }
                }
                if (hasAnyValidFile)
                {
                    normalMaterial.name = $"{assetName}/{group.Key}";
                    lodMaterial.name = $"{assetName}/{group.Key}_lod";
                    m_skins[assetName][group.Key] = new MaterialContainer
                    (
                        source: source,
                        assetName: assetName,
                        skinName: group.Key,
                        main: normalMaterial,
                        lod: lodMaterial,
                        wtsLayoutId: wtsLayoutId,
                        defaultActive: !disabledSkins.Contains(group.Key)
                    );
                    Dispatcher.main.Dispatch(() =>
                    {
                        if (SceneUtils.IsAssetEditor && assetName.Contains('.'))
                        {
                            m_skins[assetName.Split('.').Last()] = m_skins[assetName];
                        }
                    });
                }
            }

        }

        private static int ReadWEFile(int wtsLayoutId, string filePath)
        {
            bool hasEnded = false;
            var task = Dispatcher.main.Dispatch(() => ModInstance.Controller.ConnectorWE.RegisterWELayout(File.ReadAllText(filePath)));
            task.WhenEnded(() => hasEnded = true);
            var maxWait = 20;
            var wait = 0;
            do
            {
                Thread.Sleep(100);
            } while (!hasEnded && wait++ < maxWait);
            if (task.hasSucceeded)
            {
                wtsLayoutId = task.result;
            }

            return wtsLayoutId;
        }

        public IEnumerator RealoadSkinsOfInfo(VehicleInfo info, Action callback = null)
        {
            yield return 0;
            var targetSkins = new Dictionary<string, Dictionary<string, MaterialContainer>>();
            var thread = ThreadHelper.CreateThread(() => LoadFromFolder(GetDirectoryForAssetOwn(info), info, Source.WORKSHOP, targetSkins, 1, 1));
            yield return new WaitWhile(() => thread.isAlive);
            if (!SceneUtils.IsAssetEditor)
            {
                thread = ThreadHelper.CreateThread(() => LoadFromFolder(GetDirectoryForAssetShared(info), info, Source.SHARED, targetSkins, 1, 1));
                yield return new WaitWhile(() => thread.isAlive);
            }
            foreach (var skinId in targetSkins.Keys)
            {
                m_skins[skinId] = targetSkins[skinId];
            }
            m_cachedSkins.Clear();
            callback?.Invoke();
        }
        internal string CreateSkin(VehicleInfo info, string newName, Action onLoadComplete, bool shared = true)
        {
            var dir = shared ? GetDirectoryForAssetShared(info) : GetDirectoryForAssetOwn(info);
            if (dir is null)
            {
                return dir;
            }

            KFileUtils.EnsureFolderCreation(dir);
            (info.m_material.mainTexture as Texture2D).DumpTo(Path.Combine(dir, $"{newName}_{MAIN_FILESUFFIX}"));
            //(info.m_lodMaterial.mainTexture as Texture2D).DumpTo(Path.Combine(dir, $"{newName}_{MAIN_LOD_FILESUFFIX}"));

            if (info.m_material.GetTexture(XYS_MAP) is Texture2D xys)
            {
                xys.DumpTo(Path.Combine(dir, $"{newName}_{XYS_FILESUFFIX}"));
            }
            //if (info.m_lodMaterial.GetTexture(XYS_MAP) is Texture2D xys2)
            //{
            //    xys2.DumpTo(Path.Combine(dir, $"{newName}_{XYS_LOD_FILESUFFIX}"));
            //}
            if (info.m_material.GetTexture(ACI_MAP) is Texture2D aci)
            {
                aci.DumpTo(Path.Combine(dir, $"{newName}_{ACI_FILESUFFIX}"));
            }
            //if (info.m_lodMaterial.GetTexture(ACI_MAP) is Texture2D aci2)
            //{
            //    aci2.DumpTo(Path.Combine(dir, $"{newName}_{ACI_LOD_FILESUFFIX}"));
            //}
            StartCoroutine(RealoadSkinsOfInfo(info, onLoadComplete));
            return dir;
        }

        internal void SetSkin(VehicleInfo info, string v)
        {
            if (m_skins.TryGetValue(info.name, out var skinData) && skinData.TryGetValue(v, out var materialData))
            {
                //info.m_lodMaterial = materialData.lod;
                info.m_material = materialData.main;
            }
        }

        internal bool SetSkin(VehicleInfo info, ushort vehicleId, ref Vehicle vehicle)
        {
            var tryForceSkin = "";
            if (ModInstance.Controller.ConnectorCD.IsBridgeEnabled && vehicle.m_sourceBuilding != 0)
            {
                tryForceSkin = ModInstance.Controller.ConnectorCD.GetPreferredSkin(vehicle.m_sourceBuilding);
            }
            GetSkin(info, vehicleId, false, out var material, tryForceSkin);
            if (!(material is null))
            {
                //info.m_lodMaterial = material.lod;
                info.m_material = material.main;
            }
            return !(material is null);
        }
        internal bool SetSkin(VehicleInfo info, ushort vehicleId)
        {
            GetSkin(info, vehicleId, true, out var material, null);
            if (!(material is null))
            {
                //info.m_lodMaterial = material.lod;
                info.m_material = material.main;
            }
            return !(material is null);
        }

        public bool GetSkin(VehicleInfo info, ushort vehicleId, bool isParked, out MaterialContainer material, string tryUseSkin)
        {
            var targetIdx = vehicleId | (isParked ? 0x80000000u : 0);
            material = null;
            if (m_skins.TryGetValue(info.name, out var skinData))
            {
                if (ModInstance.Controller.ConnectorWE.IsWEVehicleEditorOpen
                    && ModInstance.Controller.ConnectorWE.CurrentFocusVehicle == vehicleId
                    && !isParked)
                {
                    return ModInstance.Controller.ConnectorWE.CurrentSelectionSkin is string skinNameOverride && skinData.TryGetValue(skinNameOverride, out material);
                }
                if (SceneUtils.IsAssetEditor && VSBaseLiteUI.Instance.Visible)
                {
                    return VSBaseLiteUI.Instance.GrabbedTargetSkin is string skinNameOverride && skinData.TryGetValue(skinNameOverride, out material);
                }
                if (VSBaseLiteUI.Instance.Visible
                 //&& VSBaseLiteUI.LockSelection 
                 && VSBaseLiteUI.GrabbedIsParked == isParked
                && VSBaseLiteUI.GrabbedId == vehicleId)
                {
                    return VSBaseLiteUI.Instance.GrabbedTargetSkin is string skinNameOverride && skinData.TryGetValue(skinNameOverride, out material);
                }
                if (isParked)
                {
                    if (!m_cachedSkins.TryGetValue(targetIdx, out var skinName) || (!skinData.TryGetValue(skinName, out material)))
                    {
                        return UpdateSkinCache(targetIdx, ref material, skinData, tryUseSkin);
                    }
                }
                else
                {
                    var firstVehicle = VehicleManager.instance.m_vehicles.m_buffer[vehicleId].GetFirstVehicle(vehicleId);
                    if (firstVehicle != vehicleId)
                    {
                        if (!m_cachedSkins.TryGetValue(targetIdx, out var skinName))
                        {
                            m_skins.TryGetValue(VehicleManager.instance.m_vehicles.m_buffer[firstVehicle].Info.name, out var skinDataFirst);
                            if (skinDataFirst is null || skinDataFirst.Count == 0)
                            {
                                UpdateSkinCache(targetIdx, ref material, skinData, tryUseSkin);
                                m_cachedSkins[firstVehicle] = null;
                            }
                            else
                            {
                                UpdateSkinCache(firstVehicle, ref material, skinDataFirst, tryUseSkin);
                                skinName = m_cachedSkins[targetIdx] = m_cachedSkins[firstVehicle];
                                if (skinName is null || !skinData.ContainsKey(skinName))
                                {
                                    m_cachedSkins[targetIdx] = "";
                                }
                            }
                        }
                        return !skinName.IsNullOrWhiteSpace() && skinData.TryGetValue(skinName, out material);
                    }
                    else
                    {
                        if (!m_cachedSkins.TryGetValue(targetIdx, out var skinName) || !(skinName is null || skinData.TryGetValue(skinName, out material)))
                        {
                            return UpdateSkinCache(targetIdx, ref material, skinData, tryUseSkin);
                        }
                    }
                }
                return !m_cachedSkins[targetIdx].IsNullOrWhiteSpace();
            }
            return false;
        }

        private bool UpdateSkinCache(uint targetIdx, ref MaterialContainer material, Dictionary<string, MaterialContainer> skinData, string tryUseSkin)
        {
            if (skinData is null || skinData.Count == 0)
            {
                return false;
            }
            else
            {
                if (tryUseSkin.TrimToNull() != null && skinData.ContainsKey(tryUseSkin))
                {
                    m_cachedSkins[targetIdx] = tryUseSkin;
                }
                else
                {
                    var activeSkins = skinData.ToList().Where(x => x.Value.Active).ToList();
                    m_cachedSkins[targetIdx] = activeSkins.Count > 0 ? activeSkins[(int)((targetIdx * 2465737L) & 0x7FFFFFFL) % activeSkins.Count].Key : "";
                }
                skinData.TryGetValue(m_cachedSkins[targetIdx], out material);
            }
            return !m_cachedSkins[targetIdx].IsNullOrWhiteSpace();
        }

        public bool GetSkin(VehicleInfo info, string skinName, out MaterialContainer material)
        {
            material = null;
            return m_skins.TryGetValue(info.name, out var skinData) && skinData.TryGetValue(skinName, out material);
        }

        public void ResetCache()
        {
            m_cachedSkins.Clear();
        }

        private readonly SimpleNonSequentialList<string> m_cachedSkins = new SimpleNonSequentialList<string>();

        private static readonly string[] ALLOWED_FILES = new[]
        {
            MAIN_FILESUFFIX,
            XYS_FILESUFFIX,
            ACI_FILESUFFIX,
            //MAIN_LOD_FILESUFFIX,
            //XYS_LOD_FILESUFFIX,
            //ACI_LOD_FILESUFFIX,
            WE_FILESUFFIX
        };

    }
}