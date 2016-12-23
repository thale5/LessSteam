using System;
using System.IO;
using ColossalFramework.UI;
using ICities;

namespace LessSteam
{
    internal sealed class Settings
    {
        const string FILENAME = "LessSteam.txt";
        public bool disableAdPanel;
        bool dirty = false;

        static Settings singleton;
        internal static UIHelperBase helper;
        static bool Dirty => singleton?.dirty ?? false;

        public static Settings settings
        {
            get
            {
                if (singleton == null)
                    singleton = Load();

                return singleton;
            }
        }

        Settings() { }

        static Settings Load()
        {
            Settings s = new Settings();

            try
            {
                s.disableAdPanel = File.Exists(FILENAME);
            }
            catch (Exception) { }

            return s;
        }

        void Save()
        {
            try
            {
                dirty = false;

                if (disableAdPanel)
                    File.Create(FILENAME).Dispose();
                else
                    File.Delete(FILENAME);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        // Late initialization (faster startup, less Unity GC).
        static internal void OnSettingsUI(UIHelperBase newHelper)
        {
            UIComponent comp = Self(helper);

            if (comp != null)
                comp.eventVisibilityChanged -= OnVisibilityChanged;

            helper = newHelper;
            comp = Self(newHelper);
            comp.eventVisibilityChanged -= OnVisibilityChanged;
            comp.eventVisibilityChanged += OnVisibilityChanged;
        }

        static UIComponent Self(UIHelperBase h) => ((UIHelper) h)?.self as UIComponent;

        static void OnVisibilityChanged(UIComponent comp, bool visible)
        {
            if (visible && comp == Self(helper) && comp.childCount == 0)
                settings.LateSettingsUI(helper);
            else if (!visible && Dirty)
                settings.Save();
        }

        void LateSettingsUI(UIHelperBase helper)
        {
            Check(helper, "Also disable the Workshop Ad Panel and its Steam traffic", disableAdPanel, b => { disableAdPanel = b; dirty = true; });
        }

        void Check(UIHelperBase group, string text, bool enabled, OnCheckChanged action)
        {
            try
            {
                group.AddCheckbox(text, enabled, action);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
