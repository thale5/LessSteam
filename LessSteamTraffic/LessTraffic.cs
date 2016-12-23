using System;
using System.Reflection;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;

namespace LessSteam
{
    public sealed class LessTraffic : DetourUtility
    {
        public static LessTraffic instance;
        internal readonly Type evt;
        internal readonly MethodInfo onDetailsReceived, add, remove;
        internal bool disableAds;

        public LessTraffic(bool disableAds)
        {
            try
            {
                instance = this;
                this.disableAds = disableAds;
                System.Reflection.EventInfo ev = typeof(Workshop).GetEvent("eventUGCRequestUGCDetailsCompleted");
                evt = ev.EventHandlerType;
                onDetailsReceived = typeof(EntryData).GetMethod("OnDetailsReceived", BindingFlags.NonPublic | BindingFlags.Instance);
                add = ev.GetAddMethod();
                remove = ev.GetRemoveMethod();
                init(typeof(EntryData), "RequestDetails");
                init(typeof(PackageEntry), "SetDetails", typeof(MyHook), "MyDetails");
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

        /// <summary>
        /// Notice how the default version registers a large number of Steam callbacks in a short time. When Steam responses are received,
        /// every callback gets every response, which leads to quadratic behavior, which is bad. I tested with just a few hundred workshop
        /// items and got 30 000 callback invocations.
        /// </summary>
        static void RequestDetails(EntryData data)
        {
            if (instance.disableAds)
                try
                {
                    UIComponent comp = UIView.Find("WorkshopAdPanel");
                    Util.DebugPrint("WorkshopAdPanel exists:", comp != null);
                    UILabel label = comp?.Find<UILabel>("DisabledLabel");
                    Util.DebugPrint("DisabledLabel exists:", label != null);

                    if (label != null)
                    {
                        label.text = "The Ad Panel is inactive";
                        instance.disableAds = false;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
        }
    }

    class MyHook : PackageEntry
    {
        void MyDetails()
        {
            try
            {
                if (ReferenceEquals(authorName, string.Empty))
                    RequestDetails(m_EntryData);

                if (m_EntryData.publishedFileId != PublishedFileId.invalid)
                {
                    if (m_LastUpdateLabel != null)
                        m_LastUpdateLabel.tooltip = FormatTimeInfo(m_WorkshopDetails.timeCreated, m_WorkshopDetails.timeUpdated);

                    if (m_ShareButton != null)
                        m_ShareButton.isVisible = (PlatformService.userID == m_WorkshopDetails.creatorID);

                    Update();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        static void RequestDetails(EntryData data)
        {
            if (!data.detailsPending && data.publishedFileId != PublishedFileId.invalid)
            {
                Set(data, "m_DetailsPending", true);

                //PlatformService.workshop.eventUGCRequestUGCDetailsCompleted -= new Workshop.UGCDetailsHandler(data.OnDetailsReceived);
                //PlatformService.workshop.eventUGCRequestUGCDetailsCompleted += new Workshop.UGCDetailsHandler(data.OnDetailsReceived);
                // ==
                Delegate d = Delegate.CreateDelegate(LessTraffic.instance.evt, data, LessTraffic.instance.onDetailsReceived);
                object[] args = { d };
                LessTraffic.instance.remove.Invoke(PlatformService.workshop, args);
                LessTraffic.instance.add.Invoke(PlatformService.workshop, args);
                PlatformService.workshop.RequestItemDetails(data.publishedFileId);
            }
        }

        static string FormatTimeInfo(uint timeCreated, uint timeUpdated)
        {
            string text = string.Empty;
            DateTime kEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            if (timeCreated != 0u)
            {
                text += LocaleFormatter.FormatGeneric("CONTENTMANAGER_TIMECREATED", kEpoch.AddSeconds(timeCreated).ToLocalTime().ToString());

                if (timeUpdated != 0u)
                    text += "\n";
            }

            if (timeUpdated != 0u)
                text += LocaleFormatter.FormatGeneric("CONTENTMANAGER_TIMEUPDATED", kEpoch.AddSeconds(timeUpdated).ToLocalTime().ToString());

            return text;
        }

        static void Set(object instance, string field, object value)
        {
            instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
        }
    }
}
