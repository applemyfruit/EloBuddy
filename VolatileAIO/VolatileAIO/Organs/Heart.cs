using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Security.Permissions;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using Newtonsoft.Json;
using SharpDX;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Cars;
using Activator = VolatileAIO.Organs.Brain.Activator;

namespace VolatileAIO.Organs
{
    internal class Heart
    {
        protected static readonly AIHeroClient Player = ObjectManager.Player;
        public static Menu VolatileMenu;
        protected static Menu HackMenu;
        protected static Menu TargetMenu;
        protected static Volkswagen Golf;
        public static ExtensionLoader ExtensionLoader;
        public static AutoLeveler AutoLeveler;
        public static ManaManager ManaManager;
        public static DrawManager DrawManager;
        public static RecallTracker RecallTracker;
        public static Activator Activator;
        private static ChampionProfiles _championProfiles;
        protected static bool UsingVorb;

        protected Heart()
        {
            Game.OnUpdate += OnUpdateDeathChecker;
            Chat.OnInput += Chat_OnInput;
            Drawing.OnEndScene += OnDrawDeathChecker;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Orbwalker.OnPostAttack += OrbwalkerOnOnPostAttack;
            Volkswagen.AfterAttack += Volkswagen_AfterAttack;
            Orbwalker.OnPreAttack += OrbwalkerOnOnPreAttack;
            Volkswagen.BeforeAttack += Volkswagen_BeforeAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Game.OnWndProc += Game_OnWndProc;
            AttackableUnit.OnDamage += OnDamage;
            Teleport.OnTeleport += Teleport_OnTeleport;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnBuffGain += OnBuffGain;
            Obj_AI_Base.OnBuffLose += OnBuffLose;
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
            Spellbook.OnUpdateChargeableSpell += OnUpdateChargeableSpell;
            Spellbook.OnCastSpell += OnCastSpell;
            Spellbook.OnStopCast += OnStopCast;
            Game.OnEnd += Game_OnEnd;
            Game.OnDisconnect += Game_OnDisconnect;
        }

        public Heart(bool beat)
        {
            if (beat)
                StartHeartBeat();
        }

        private static void StartHeartBeat()
        {
            Bootstrap.Init(null);
            Hacks.RenderWatermark = false;
            Chat.Print(
                "Starting <font color = \"#740000\">Volatile AIO</font> <font color = \"#B87F7F\">Heart.cs</font>:");
            if (!Directory.Exists(ChampionProfiles.VolatileDir))
                Directory.CreateDirectory(ChampionProfiles.VolatileDir);
            if (!File.Exists(Path.Combine(
                    ChampionProfiles.VolatileDir, "Volatile.json")))
                SaveSettings(false);
            else 
            if (LoadSettings())
            {
                UsingVorb = true;
                Volkswagen.AddToMenu();
            }
            else
            {
                UsingVorb = false;
            }
            VolatileMenu = MainMenu.AddMenu("V." + Player.ChampionName, "volatilemenu", "Volatile " + Player.ChampionName);
            ExtensionLoader = new ExtensionLoader();

            //InfoBoard
            VolatileMenu.AddGroupLabel("Heart.cs");
            VolatileMenu.AddLabel("\"I.. I'm alive.. I can feel my heart beating.\"");
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("Welcome to Volatile AIO." + Environment.NewLine +
                                  "Volatile is an intelligent and aware AIO." + Environment.NewLine +
                                  "It strives to include the most thorough logic" + Environment.NewLine +
                                  "and the most pleasant game experience" + Environment.NewLine);
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("I hope you'll like it.");
            VolatileMenu.AddSeparator();
            VolatileMenu.AddGroupLabel("Supported Champions:");
            foreach (var champion in ExtensionLoader.Champions)
            {
                var label = champion.Name + " by " + champion.Developer;
                for (var i = champion.Name.Length; i < 20; i++)
                    label += " ";
                label += "Status: " + champion.State;
                VolatileMenu.AddLabel(label);
            }
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("AIO Options:");
            VolatileMenu.Add("debug", new CheckBox("Debug", false));
            VolatileMenu.Add("golf", new CheckBox("Use Volatile Orbwalker", false)).OnValueChange += Secret_OnValueChange; 
            VolatileMenu.AddLabel("*Orbwalker requires reload. Press f5 to reload, and please turn off EB Orbwalker Drawings");
            //VolatileMenu.Add("vpred2", new Slider("Super Ultra Secret Dont Even Look", 0, 0, 2));
            if (ExtensionLoader.Champions.All(c => c.Name != Player.ChampionName)) return;
            TargetMenu = VolatileMenu.AddSubMenu("Target Manager", "targetmenu", "Volatile TargetManager");
            TargetMenu.Add("chosenignores", new CheckBox("Ignore all other champions if Selected Target", false));
            ManaManager = new ManaManager();
            AutoLeveler = new AutoLeveler();
            DrawManager = new DrawManager();
            HackMenu = VolatileMenu.AddSubMenu("Hacks", "hacks", "Volatile Hacks");
            SkinManager.Initialize();
            RecallTracker = new RecallTracker();
            Activator = new Activator();
            _championProfiles = new ChampionProfiles();
            if (!AutoLeveler.PrioritiesAreSet() &&
                AutoLeveler.AutoLevelMenu["autolevel"].Cast<CheckBox>().CurrentValue)
                Chat.Print("Auto-Leveler: Priorities not Set!");
            if (!ManaManager.PrioritiesAreSet() && ManaManager.MmMenu["manamanager"].Cast<CheckBox>().CurrentValue)
                Chat.Print("Mana Manager: Priorities not Set!");
            OrbHandler();
        }

        #region privatevoid

        private static void Secret_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            SaveSettings(VolatileMenu["golf"].Cast<CheckBox>().CurrentValue);
        }

        private static void OrbHandler()
        {
            if (LoadSettings())
            {
                Orbwalker.DisableMovement = true;
                Orbwalker.DisableAttacking = true;
                MainMenu.GetMenu("Orbwalker").DisplayName = "EBORB - Disabled";
                MainMenu.GetMenu("orb").DisplayName = "VORB - Enabled";
            }
            else
            {
                Orbwalker.DisableMovement = false;
                Orbwalker.DisableAttacking = false;
            }
        }

        private void Volkswagen_BeforeAttack(Volkswagen.BeforeAttackEventArgs args)
        {
            Volatile_VWBeforeAttack(args);
        }

        private void Volkswagen_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            Volatile_VWAfterAttack(unit, target);
        }

        private void Game_OnDisconnect(EventArgs args)
        {
            Volatile_OnDisconnect(args);
        }

        private void Chat_OnInput(ChatInputEventArgs args)
        {
            Volatile_OnChatInput(args);
        }

        private void Game_OnEnd(GameEndEventArgs args)
        {
            Volatile_OnEnd(args);
        }

        private void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            Volatile_OnLevelUp(sender,args);
        }

        private void OnUpdateDeathChecker(EventArgs args)
        {
            if (Player.IsDead) return;
            Volatile_OnHeartBeat(args);
            TickManager.Tick();
        }

        private void OnDrawDeathChecker(EventArgs args)
        {
            if (Player.IsDead) return;
            Volative_OnDrawEnd(args);
        }
        
        private void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            Volative_OnDraw(args);
        }
        private void OrbwalkerOnOnPostAttack(AttackableUnit target, EventArgs args)
        {
            Volatile_OnPostAttack(target, args);
        }

        private void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            Volatile_OnDamage(sender, args);
        }

        private void OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            Volatile_OnStopCast(sender, args);
        }

        private void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            OnSpellCast(sender, args);
        }

        private void OnUpdateChargeableSpell(Spellbook sender, SpellbookUpdateChargeableSpellEventArgs args)
        {
            Volatile_OnUpdateChargeableSpell(sender, args);
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            Volatile_ProcessSpellCast(sender, args);
        }

        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            Volatile_OnObjectDelete(sender, args);
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            Volatile_OnObjectCreate(sender, args);
        }

        private void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs args)
        {
            Volatile_AntiGapcloser(sender, args);
        }

        private void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            Volatile_OnInterruptable(sender, args);
        }

        private void OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            Volatile_OnBuffLose(sender, args);
        }

        private void OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            Volatile_OnBuffGain(sender, args);
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            Volatile_OnWndProc(args);
            if ((args.Msg == (uint) WindowMessages.LeftButtonDown))
            {
                TargetManager.SetChosenTarget(args);
                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                {
                    Chat.Print(Game.CursorPos2D.X+","+Game.CursorPos2D.Y);
                }
            }
        }

        private void OrbwalkerOnOnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            Volatile_OnPreAttack(target, args);
        }

        private void Teleport_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            Volatile_OnTeleport(sender, args);
        }

        #endregion

        #region virtualvoid

        protected virtual void Volatile_VWBeforeAttack(Volkswagen.BeforeAttackEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_VWAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            //for extensions
        }

        protected virtual void Volatile_OnDisconnect(EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnChatInput(ChatInputEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnEnd(GameEndEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnUpdateChargeableSpell(Spellbook sender,
            SpellbookUpdateChargeableSpellEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_ProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnObjectDelete(GameObject sender, EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnObjectCreate(GameObject sender, EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnInterruptable(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnHeartBeat(EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volative_OnDrawEnd(EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volative_OnDraw(EventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnWndProc(WndEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            //for extensions
        }

        protected virtual void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            //for extensions
        }

        #endregion

        #region Utility

        public static bool InFountain(AIHeroClient hero)
        {
            float fountainRange = 562500; //750 * 750
            Vector3 vec3 = (hero.Team == GameObjectTeam.Order)
                ? new Vector3(363, 426, 182)
                : new Vector3(14340, 14390, 172);
            var map = Game.MapId;
            if (map == GameMapId.SummonersRift)
            {
                fountainRange = 1102500; //1050 * 1050
            }
            return hero.IsVisible && hero.Distance(vec3, true) < fountainRange;
        }

        public static bool IsChampion(Obj_AI_Base unit)
        {
            var hero = unit as AIHeroClient;
            return hero != null && hero.IsValid;
        }

        private struct VSettings
        {
            [JsonProperty(PropertyName = "UseOrb")]
            public readonly bool UseOrb;
            [JsonProperty(PropertyName = "Version")]
            public readonly double Version;

            public VSettings(bool useorb, double version)
            {
                UseOrb = useorb;
                Version = version;
            }
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        private static bool LoadSettings()
        {
            var json = File.ReadAllText(Path.Combine(
                    ChampionProfiles.VolatileDir, "Volatile.json"));
            var profile = JsonConvert.DeserializeObject<VSettings>(json);
            return profile.UseOrb;
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        private static void SaveSettings(bool UseOrb)
        {
            var profile = new VSettings(UseOrb, 1.0);
            var json = JsonConvert.SerializeObject(profile);
            if (File.Exists(Path.Combine(
                    ChampionProfiles.VolatileDir, "Volatile.json")))
            {
                File.Delete(Path.Combine(
                    ChampionProfiles.VolatileDir, "Volatile.json"));
            }
            else
            {
            }
            using (var sw = new StreamWriter(Path.Combine(
                    ChampionProfiles.VolatileDir, "Volatile.json")))
            {
                sw.Write(json);
            }
        }

        protected static bool ComboActive()
        {
            if (UsingVorb)
            {
                return MainMenu.GetMenu("orb")["com"].Cast<KeyBind>().CurrentValue;
            }
            else
            {
                return Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo;
            }
        }

        protected static bool HarassActive()
        {
            if (UsingVorb)
            {
                return MainMenu.GetMenu("orb")["mix"].Cast<KeyBind>().CurrentValue;
            }
            else
            {
                return Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass;
            }
        }

        protected static bool LaneClearActive()
        {
            if (UsingVorb)
            {
                return MainMenu.GetMenu("orb")["lan"].Cast<KeyBind>().CurrentValue;
            }
            else
            {
                return Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LaneClear;
            }
        }

        protected static bool LastHitActive()
        {
            if (UsingVorb)
            {
                return MainMenu.GetMenu("orb")["las"].Cast<KeyBind>().CurrentValue;
            }
            else
            {
                return Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit;
            }
        }
        #endregion
    }
}
