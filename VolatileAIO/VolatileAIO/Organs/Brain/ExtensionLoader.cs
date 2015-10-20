using System.Net;
using EloBuddy;
using VolatileAIO.Extensions.ADC;
using VolatileAIO.Extensions.Jungle;
using VolatileAIO.Extensions.Support;

namespace VolatileAIO.Organs.Brain
{
    public class ExtensionLoader
    {
        private static bool _loaded;

        public ExtensionLoader()
        {
            if (!_loaded)
            {
                switch (ObjectManager.Player.ChampionName.ToLower())
                {
                    case "ezreal":
                        new Ezreal();
                        _loaded = true;
                        break;
                    case "blitzcrank":
                        new Blitzcrank();
                        _loaded = true;
                        break;
                    case "evelynn":
                        new Evelynn();
                        _loaded = true;
                        break;
                    default:
                        Chat.Print("<font color = \"#740000\">Volatile AIO</font> doesn't support " + ObjectManager.Player.ChampionName + " yet.");
                        break;
                }
            }
        }
    }
}