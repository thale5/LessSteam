using ICities;

namespace LessSteam
{
    public sealed class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => "Less Steam";
        public string Description => "Faster startup";
        public void OnEnabled() => LessTraffic.Setup();

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
