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

        public static readonly Dictionary<string, State> ExtensionState = new Dictionary<string, State>()
        {
            {"Alistar", State.FullyDeveloped },
            {"Blitzcrank", State.FullyDeveloped },
            {"Cassiopeia", State.PartDeveloped },
            {"Evelynn", State.PartDeveloped },
            {"Ezreal", State.BeingOptimized },
            {"Tristana", State.BeingOptimized }
        };

        public enum State
        {
            Outdated = 0,
            PartDeveloped = 1,
            BeingOptimized = 2,
            FullyDeveloped = 3,
        }

        private void WelcomeChat()
        {
            Chat.Print("<font color = \"#00FF00\">Succesfully loaded Extension: </font><font color = \"#FFFF00\">" + ObjectManager.Player.ChampionName + "</font>");
            State state;
            ExtensionState.TryGetValue(ObjectManager.Player.ChampionName, out state);
            if ((int)state<3) Chat.Print("<font color = \"#FFCC00\">Please note:</font> <font color = \"#FFFF00\">" + ObjectManager.Player.ChampionName + "</font><font color = \"#FFCC00\"> is still </font>!<font color = \"#800000\">" + Enum.GetName(state.GetType(),state)+"</font>!");
        }

        public ExtensionLoader()
        {
            if (_loaded) return;
            if (ExtensionState.ContainsKey(ObjectManager.Player.ChampionName)) WelcomeChat();
            switch (ObjectManager.Player.ChampionName.ToLower())
            {
                case "alistar":
                    new Alistar();
                    _loaded = true;
                    break;
                case "blitzcrank":
                    new Blitzcrank();
                    _loaded = true;
                    break;
                case "cassiopeia":
                    new Cassiopeia();
                    _loaded = true;
                    break;
                case "evelynn":
                    new Evelynn();
                    _loaded = true;
                    break;
                case "ezreal":
                    new Ezreal();
                    _loaded = true;
                    break;
                case "tristana":
                    new Tristana();
                    _loaded = true;
                    break;
                default:
                    Chat.Print("<font color = \"#740000\">Volatile AIO</font> doesn't support " + ObjectManager.Player.ChampionName + " yet.");
                    break;
            }
        }

    }
}