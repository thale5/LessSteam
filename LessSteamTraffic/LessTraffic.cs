using System;
using System.Reflection;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using System.Collections.Generic;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework;

namespace LessSteam
{
    public sealed class LessTraffic : DetourUtility
    {
        public static LessTraffic instance;
        public HashSet<PublishedFileId> pending = new HashSet<PublishedFileId>();
        internal bool disableAds;
        const string ROUTINE = "<RequestDetailsCoroutine>c__Iterator0";

        public LessTraffic(bool disableAds)
        {
            try
            {
                instance = this;
                this.disableAds = disableAds;
                Type coroutine = typeof(CategoryContentPanel).GetNestedType(ROUTINE, BindingFlags.NonPublic);

                if (coroutine != null)
                    init(coroutine, "MoveNext");

                init(typeof(PackageEntry), "SetNameLabel", typeof(MyHook), "HookedNameLabel");
                init(typeof(PackageEntry), "GetMissingPresetAssets", typeof(MyHook), "MyMissingAssets");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        internal static void Setup()
        {
            bool disableAds = Settings.settings.disableAdPanel;
            new LessTraffic(disableAds).Deploy();

            if (disableAds)
                try
                {
                    typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
        }

        internal override void Dispose()
        {
            Revert();
            base.Dispose();
            instance = null;
        }

        bool MoveNext()
        {
            if (LessTraffic.instance.disableAds)
                try
                {
                    UIComponent comp = UIView.Find("WorkshopAdPanel");
                    UILabel label = comp?.Find<UILabel>("DisabledLabel");

                    if (label != null)
                    {
                        label.text = "The Ad Panel is inactive";
                        LessTraffic.instance.disableAds = false;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

            return false;
        }
    }

    class MyHook : PackageEntry
    {
        void HookedNameLabel(string entryName, string authorName)
        {
            PublishedFileId id = publishedFileId;

            if (id != PublishedFileId.invalid)
                try
                {
                    var pending = LessTraffic.instance.pending;

                    if (ReferenceEquals(authorName, string.Empty))
                    {
                        if (pending.Add(id))
                            PlatformService.workshop.RequestItemDetails(id);
                    }
                    else
                        pending.Remove(id);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

            m_NameLabel.text = FormatPackageName(entryName, authorName, isWorkshopItem);
        }

        List<ModInfo> MyMissingAssets()
        {
            List<ModInfo> list = new List<ModInfo>();

            try
            {
                HashSet<ulong> whatWeHave = new HashSet<ulong>() { PublishedFileId.invalid.AsUInt64 };

                foreach (Package p in PackageManager.allPackages)
                    if (ulong.TryParse(p.packageName, out ulong value))
                        whatWeHave.Add(value);

                ModInfo[] presetAssets = GetPresetAssets();

                for (int i = 0; i < presetAssets.Length; i++)
                    if (!whatWeHave.Contains(presetAssets[i].modWorkshopID))
                        list.Add(presetAssets[i]);

                whatWeHave.Clear();

                foreach (PluginManager.PluginInfo m in Singleton<PluginManager>.instance.GetPluginsInfo())
                    whatWeHave.Add(m.publishedFileID.AsUInt64);

                ModInfo[] presetMods = GetPresetMods();

                for (int i = 0; i < presetMods.Length; i++)
                    if (!whatWeHave.Contains(presetMods[i].modWorkshopID))
                        list.Add(presetMods[i]);

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

            return list;
        }

        ModInfo[] GetPresetAssets()
        {
            SaveGameMetaData saveGameMetaData = (!(this.asset.type == UserAssetType.SaveGameMetaData)) ? null : ((this.m_EntryData.metaData == null) ? null : ((SaveGameMetaData) this.m_EntryData.metaData));
            ScenarioMetaData scenarioMetaData = (!(this.asset.type == UserAssetType.ScenarioMetaData)) ? null : ((this.m_EntryData.metaData == null) ? null : ((ScenarioMetaData) this.m_EntryData.metaData));
            if (saveGameMetaData != null && saveGameMetaData.assets != null)
            {
                return saveGameMetaData.assets;
            }
            if (scenarioMetaData != null && scenarioMetaData.assets != null)
            {
                return scenarioMetaData.assets;
            }
            return new ModInfo[0];
        }

        ModInfo[] GetPresetMods()
        {
            SaveGameMetaData saveGameMetaData = (!(this.asset.type == UserAssetType.SaveGameMetaData)) ? null : ((this.m_EntryData.metaData == null) ? null : ((SaveGameMetaData) this.m_EntryData.metaData));
            ScenarioMetaData scenarioMetaData = (!(this.asset.type == UserAssetType.ScenarioMetaData)) ? null : ((this.m_EntryData.metaData == null) ? null : ((ScenarioMetaData) this.m_EntryData.metaData));
            if (saveGameMetaData != null && saveGameMetaData.mods != null)
            {
                return saveGameMetaData.mods;
            }
            if (scenarioMetaData != null && scenarioMetaData.mods != null)
            {
                return scenarioMetaData.mods;
            }
            return new ModInfo[0];
        }
    }
}
