using MHServerEmu.Core.Config;

namespace MHServerEmuMini.Auth
{
    public class AuthConfig : ConfigContainer
    {
        public string Address { get; private set; } = "localhost";
        public string Port { get; private set; } = "8080";
    }
}
