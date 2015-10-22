using System;
using System.Collections.Generic;
using System.Net;
using EloBuddy;
using EloBuddy.SDK;
using VolatileAIO.Extensions.ADC;
using VolatileAIO.Extensions.Jungle;
using VolatileAIO.Extensions.Mid;
using VolatileAIO.Extensions.Support;

namespace VolatileAIO.Organs.Brain
{
    public class ExtensionLoader
    {
        private static bool _loaded;

        private readonly Dictionary<string, State> _extensionState = new Dictionary<string, State>()
        {
            {"blitzcrank", State.PartDeveloped },
            {"cassiopeia", State.BeingDeveloped },
            {"evelynn", State.PartDeveloped },
            {"ezreal", State.PartDeveloped }
        };

        enum State
        {
            Outdated = 0,
            BeingDeveloped = 1,
            PartDeveloped = 2,
            FullyDeveloped = 3,
        }

        private void WelcomeChat()
        {
            Chat.Print("<font color = \"#00FF00\">Succesfully loaded Extension: </font><font color = \"#FFFF00\">" + ObjectManager.Player.ChampionName + "</font>");
            State state;
            _extensionState.TryGetValue(ObjectManager.Player.ChampionName.ToLower(), out state);
            if ((int)state<4) Chat.Print("<font color = \"#FFCC00\">Please note:</font> <font color = \"#FFFF00\">" + ObjectManager.Player.ChampionName + "</font><font color = \"#FFCC00\"> is </font>!<font color = \"#800000\">" + Enum.GetName(state.GetType(),state)+"</font>!");
        }

        public ExtensionLoader()
        {
            if (_loaded) return;
            if (_extensionState.ContainsKey(ObjectManager.Player.ChampionName.ToLower())) WelcomeChat();
            switch (ObjectManager.Player.ChampionName.ToLower())
            {
                case "cassiopeia":
                    new Cassiopeia();
                    _loaded = true;
                    break;
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