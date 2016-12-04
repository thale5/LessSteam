using ICities;

namespace LessSteam
{
    public sealed class Mod : IUserMod
    {
        public string Name => "Less Steam";
        public string Description => "Less net traffic to improve loading times";
        public void OnEnabled() => LessTraffic.Setup();
        public void OnDisabled() => LessTraffic.instance.Dispose();
    }
}
