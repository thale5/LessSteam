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

namespace LessSteam
{
    public sealed class LessTraffic : DetourUtility
    {
        public static LessTraffic instance;
        public HashSet<PublishedFileId> pending = new HashSet<PublishedFileId>();
        internal bool disableAds;
        //const string ROUTINE = "<RequestDetailsCoroutine>c__Iterator0";

        public LessTraffic(bool disableAds)
        {
            instance = this;
            this.disableAds = disableAds;
            //Type coroutine = typeof(CategoryContentPanel).GetNestedType(ROUTINE, BindingFlags.NonPublic);

            //if (coroutine != null)
            //    init(coroutine, "MoveNext");

            //init(typeof(PackageEntry), "SetNameLabel", typeof(MyHook), "HookedNameLabel");
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
            Util.DebugPrint("entryName:", entryName, " authorName:", authorName, "id:", id);

            if (id != PublishedFileId.invalid)
                try
                {
                    var pending = LessTraffic.instance.pending;

                    if (ReferenceEquals(authorName, string.Empty))
                    {
                        if (pending.Add(id))
                        {
                            Util.DebugPrint("RequestItemDetails:", entryName);
                            PlatformService.workshop.RequestItemDetails(id);
                        }
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

    class MyEntry: EntryData
    {
        public MyEntry(Package.Asset asset) : base(asset) { }

        public void MyDetailsReceived(UGCDetails details, bool ioError)
        {
            string aps = "";
            aps += asset != null ? "A" : " ";
            aps += pluginInfo != null ? "P" : " ";
            aps += subscription != null ? "S" : " ";

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

                Util.DebugPrint("DetailsReceived", aps, "entryName", entryName, "authorName", authorName, "authorID", authorID,
                    "publishedFileId", publishedFileId, "dataTimestamp", dataTimestamp, "updateTime", updateTime, "title", details.title);

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
            else
                Util.DebugPrint("DetailsReceived", aps, "entryName", entryName, publishedFileId, "!=", details.publishedFileId);
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
                    Util.DebugPrint("NameReceived", "entryName", item.entryName, "authorName", item.authorName, "authorID", item.authorID,
                        "publishedFileId", item.publishedFileId);
                    item.attachedEntry?.SetNameLabel(item.entryName, item.authorName);
                }
                namesPending.Remove(id.AsUInt64);
            }
            else
                Util.DebugPrint("NameReceived", "authorID miss", id.AsUInt64);
        }
    }
}
