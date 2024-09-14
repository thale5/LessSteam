using ICities;

namespace LessSteam
{
    public sealed class Mod : LoadingExtensionBase, IUserMod
    {
        static bool done = false;
        public string Name
        {
            get
            {
                if (!done)
                {
                    done = true;
                    LessTraffic.Setup();
                }

                return "Less Steam";
            }
        }

        public string Description => "Less network traffic";
        public void OnEnabled()
        {
            // LessTraffic.Setup();
        }

        public void OnDisabled()
        {
            if (LessTraffic.instance != null)
                LessTraffic.instance.Dispose();
        }

        public void OnSettingsUI(UIHelperBase helper) => Settings.OnSettingsUI(helper);

        public override void OnLevelLoaded(LoadMode mode)
        {
            Settings.helper = null;

            if (LessTraffic.instance != null)
            {
                LessTraffic.instance.disableAds = Settings.settings.disableAdPanel;
                LessTraffic.instance.pending.Clear();
            }
        }
    }
}
