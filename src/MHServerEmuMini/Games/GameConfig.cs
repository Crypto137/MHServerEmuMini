using MHServerEmu.Core.Config;

namespace MHServerEmuMini.Games
{
    public class GameConfig : ConfigContainer
    {
        public string PlayerName { get; private set; }
        public string DefaultAvatar { get; private set; }
    }
}
