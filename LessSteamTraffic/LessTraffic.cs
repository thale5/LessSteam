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
            try
            {
                instance = this;
                this.disableAds = disableAds;
                Type coroutine = typeof(CategoryContentPanel).GetNestedType(ROUTINE, BindingFlags.NonPublic);

                if (coroutine != null)
                    init(coroutine, "MoveNext");

                init(typeof(PackageEntry), "SetNameLabel", typeof(MyHook), "HookedNameLabel");
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
    }
}
