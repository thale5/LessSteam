using System;
using System.Reflection;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using System.Collections.Generic;
using ColossalFramework.Subscription;
using ColossalFramework.Packaging;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using UnityEngine;

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
            init(typeof(EntryData), "OnDetailsReceived", typeof(MyEntry), "MyDetailsReceived");
            init(typeof(EntryData), "OnNameReceived", typeof(MyEntry), "MyNameReceived");
        }

        internal static void Setup()
        {
            Util.stopWatch.Reset(); Util.stopWatch.Start();
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
            Util.DebugPrint("In MoveNext");
            try
            {
                if (LessTraffic.instance.disableAds)
                {
                    UIComponent comp = UIView.Find("WorkshopAdPanel");
                    UILabel label = comp?.Find<UILabel>("DisabledLabel");

                    if (comp != null)
                        Util.DebugPrint("WorkshopAdPanel found");

                    if (label != null)
                    {
                        label.text = "The Ad Panel is inactive";
                        LessTraffic.instance.disableAds = false;
                        Util.DebugPrint("DisabledLabel set");
                    }
                    else
                        Util.DebugPrint("DisabledLabel not found");
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
                    MyEntry.DebugPrint("REQUESTDETAILS", m_EntryData);
                    m_EntryData.lastDataRequest = Time.time;
                    PlatformService.workshop.RequestItemDetails(id);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            else
            {
                if (id == PublishedFileId.invalid)
                    Util.DebugPrint("Invalid Id  entryName", entryName, "authorName", authorName);
                else
                    MyEntry.DebugPrint("Not Requesting", m_EntryData);
            }

            m_NameLabel.text = FormatPackageName(entryName, authorName, isWorkshopItem);
        }
    }

    class MyEntry: EntryData
    {
        public MyEntry(Package.Asset asset) : base(asset) { }

        public void MyDetailsReceived(UGCDetails details, bool ioError)
        {
            if (publishedFileId == details.publishedFileId)
            {
                Util.Set(this, "m_WorkshopDetails", details);
                if (subscription != null)
                {
                    subscription.title = details.title;
                    Util.SetProperty(this, "entryName", details.title);
                }
                authorName = new Friend(details.creatorID).personaName;
                authorID = details.creatorID.AsUInt64;
                updateTime = new DateTime(1970, 1, 1).AddSeconds(details.timeUpdated);
                dataTimestamp = DateTime.UtcNow;

                DebugPrint("DetailsReceived", this);

                var namesPending = (Dictionary<ulong, HashSet<EntryData>>) Util.GetStatic(typeof(EntryData), "m_namesPending");
                if (!namesPending.TryGetValue(details.creatorID.AsUInt64, out HashSet<EntryData> value))
                {
                    value = new HashSet<EntryData>((IEqualityComparer<EntryData>) Util.GetStatic(typeof(EntryData), "Comparer"));
                    namesPending.Add(details.creatorID.AsUInt64, value);
                }
                value.Add(this);

                Util.Set(this, "m_DetailsPending", false);
                attachedEntry?.SetEntry(this);
            }
        }

        private static void MyNameReceived(UserID id, PersonaChange flags)
        {
            var namesPending = (Dictionary<ulong, HashSet<EntryData>>) Util.GetStatic(typeof(EntryData), "m_namesPending");
            if (namesPending.TryGetValue(id.AsUInt64, out HashSet<EntryData> value))
            {
                foreach (EntryData item in value)
                {
                    item.authorName = new Friend(item.workshopDetails.creatorID).personaName;
                    item.authorID = item.workshopDetails.creatorID.AsUInt64;

                    DebugPrint("NameReceived", item);

                    item.attachedEntry?.SetNameLabel(item.entryName, item.authorName);
                }
                namesPending.Remove(id.AsUInt64);
            }
            else
                Util.DebugPrint("NameReceived  authorID miss", id.AsUInt64);
        }

        internal static void DebugPrint(string what, EntryData item)
        {
            string aps = "";
            aps += item.asset != null ? "A" : " ";
            aps += item.pluginInfo != null ? "P" : " ";
            aps += item.subscription != null ? "S" : " ";

            Util.DebugPrint(what, aps, "entryName", item.entryName, "authorName", item.authorName, "authorID", item.authorID,
                "publishedFileId", item.publishedFileId, "dataTimestamp", item.dataTimestamp, "updateTime", item.updateTime);
        }
    }
}
