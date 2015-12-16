using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using Newtonsoft.Json;

namespace VolatileAIO.Organs.Brain
{
    internal class ChampionProfiles : Heart
    {
        internal enum OptionType
        {
            Checkbox,
            Slider,
            Key
        }

        internal enum MenuType
        {
            Manamanager,
            Autoleveler,
            Castmanager
        }

        private struct ChampionProfile
        {
            [JsonProperty(PropertyName = "Version")] public readonly double Version;
            [JsonProperty(PropertyName = "Settings")] public readonly List<ProfileOption> Settings;

            public ChampionProfile(double version, List<ProfileOption> settings)
            {
                Version = version;
                Settings = settings;
            }
        }

        internal struct ProfileOption
        {
            [JsonProperty(PropertyName = "MenuType")] internal readonly MenuType MenuType;
            [JsonProperty(PropertyName = "Id")] internal readonly string Id;
            [JsonProperty(PropertyName = "Type")] internal readonly OptionType Type;
            [JsonProperty(PropertyName = "Value")] internal readonly string Value;

            public ProfileOption(MenuType menuType, string id, OptionType type, string value)
            {
                MenuType = menuType;
                Id = id;
                Type = type;
                Value = value;
            }
        }

        public static string VolatileDir
        {
            get
            {
                // ReSharper disable once ConvertPropertyToExpressionBody
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EloBuddy", "Volatile");
            }
        }

        public static string VolatileFile
        {
            get
            {
                // ReSharper disable once ConvertPropertyToExpressionBody
                return Path.Combine(
                    VolatileDir, Player.ChampionName + ".json");
            }
        }

        public ChampionProfiles()
        {
            Options = new List<ProfileOption>
        {
            new ProfileOption(MenuType.Manamanager, "s1", OptionType.Slider,
                ManaManager.MmMenu["s1"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Manamanager, "s2", OptionType.Slider,
                ManaManager.MmMenu["s2"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Manamanager, "s3", OptionType.Slider,
                ManaManager.MmMenu["s3"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Manamanager, "s4", OptionType.Slider,
                ManaManager.MmMenu["s4"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Autoleveler, "s1", OptionType.Slider,
                AutoLeveler.AutoLevelMenu["s1"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Autoleveler, "s2", OptionType.Slider,
                AutoLeveler.AutoLevelMenu["s2"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Autoleveler, "s3", OptionType.Slider,
                AutoLeveler.AutoLevelMenu["s3"].Cast<Slider>().CurrentValue.ToString()),
            new ProfileOption(MenuType.Autoleveler, "s4", OptionType.Slider,
                AutoLeveler.AutoLevelMenu["s4"].Cast<Slider>().CurrentValue.ToString())
        };
            if (!Directory.Exists(VolatileDir))
                Directory.CreateDirectory(VolatileDir);

            if (!File.Exists(VolatileFile))
                SaveProfile();
            else LoadProfile();
        }

        protected override void Volatile_OnDisconnect(EventArgs args)
        {
            SaveProfile();
        }

        protected override void Volatile_OnEnd(GameEndEventArgs args)
        {
            SaveProfile();
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        private static void LoadProfile()
        {
            var json = File.ReadAllText(VolatileFile);
            var profile = JsonConvert.DeserializeObject<ChampionProfile>(json);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (profile.Version == 1.0)
            {
                foreach (var setting in profile.Settings)
                {
                    Menu settingMenu = MainMenu.GetMenu("volatilemenu.autoleveler");
                    if (setting.MenuType == MenuType.Manamanager)
                        settingMenu = MainMenu.GetMenu("volatilemenu.manamanager");
                    else if (setting.MenuType == MenuType.Castmanager) settingMenu = MainMenu.GetMenu("volatilemenu.castmenu");
                    switch (setting.Type)
                    {
                        case OptionType.Checkbox:
                            settingMenu[setting.Id].Cast<CheckBox>().CurrentValue = Convert.ToBoolean(setting.Value);
                            break;
                        case OptionType.Slider:
                            settingMenu[setting.Id].Cast<Slider>().CurrentValue = Convert.ToInt32(setting.Value);
                            break;
                    }
                }
            }
            Chat.Print("Loaded Settings for " + Player.ChampionName);
        }

        protected override void Volatile_OnChatInput(ChatInputEventArgs args)
        {
            switch (args.Input)
            {
                case "/save":
                    SaveProfile();
                    args.Process = false;
                    break;
                case "/load":
                    LoadProfile();
                    args.Process = false;
                    break;
                case "/help":
                    Chat.Print(Options.Count);
                    args.Process = false;
                    break;
            }
        }


        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        private static void SaveProfile()
        {
            var profile = new ChampionProfile(1.0, Options);
            var json = JsonConvert.SerializeObject(profile);
            if (File.Exists(VolatileFile))
            {
                File.Delete(VolatileFile);
                Chat.Print("Saved Settings for " + Player.ChampionName);
            }
            else
            {
                Chat.Print("Settings File created for " + Player.ChampionName);
            }
            using (var sw = new StreamWriter(VolatileFile))
            {
                sw.Write(json);
            }
        }

        public static List<ProfileOption> Options;
    }
}