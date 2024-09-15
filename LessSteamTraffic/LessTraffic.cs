using System;
using System.Reflection;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using System.Collections.Generic;

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
            instance = this;
            this.disableAds = disableAds;
            Type coroutine = typeof(CategoryContentPanel).GetNestedType(ROUTINE, BindingFlags.NonPublic);

            if (coroutine != null)
                init(coroutine, "MoveNext");

            init(typeof(PackageEntry), "SetNameLabel", typeof(MyHook), "HookedNameLabel");
        }

        internal static void Setup()
        {
            bool disableAds = Settings.settings.disableAdPanel;

            try
            {
                new LessTraffic(disableAds).Deploy();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }

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
            try
            {
                Util.DebugPrint("Mod disabled");
                Revert();
                base.Dispose();
                instance = null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        bool MoveNext()
        {
            try
            {
                if (LessTraffic.instance.disableAds)
                {
                    UIComponent comp = UIView.Find("WorkshopAdPanel");
                    UILabel label = comp?.Find<UILabel>("DisabledLabel");

                    if (label != null)
                    {
                        label.text = "The Ad Panel is inactive";
                        LessTraffic.instance.disableAds = false;
                    }
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

            if (id != PublishedFileId.invalid && m_EntryData.needUpdateData && LessTraffic.instance.pending.Add(id))
                try
                {
                    m_EntryData.lastDataRequest = UnityEngine.Time.time;
                    PlatformService.workshop.RequestItemDetails(id);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

            if (authorName != "[unknown]" || !m_NameLabel.text.StartsWith(entryName))
                m_NameLabel.text = FormatPackageName(entryName, authorName, isWorkshopItem);
        }
    }
}
