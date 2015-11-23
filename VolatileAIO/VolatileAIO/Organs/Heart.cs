using System;
using System.Media;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs.Brain;
using Activator = VolatileAIO.Organs.Brain.Activator;

namespace VolatileAIO.Organs
{
    internal class Heart
    {
        protected static readonly AIHeroClient Player = ObjectManager.Player;
        public static Menu VolatileMenu;
        protected static Menu HackMenu;
        protected static Menu TargetMenu;
        public static AutoLeveler AutoLeveler;
        public static ManaManager ManaManager;
        public static DrawManager DrawManager;
        public static RecallTracker RecallTracker;
        public static Activator Activator;
        private static readonly SoundPlayer Initiated = new SoundPlayer(Properties.Resources.Initiated);
        private static ChampionProfiles _championProfiles;

        protected Heart()
        {
            Game.OnUpdate += OnUpdateDeathChecker;
            Chat.OnInput += Chat_OnInput;
            Drawing.OnEndScene += OnDrawDeathChecker;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Orbwalker.OnPostAttack += OrbwalkerOnOnPostAttack;
            Orbwalker.OnPreAttack += OrbwalkerOnOnPreAttack;
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

        private void StartHeartBeat()
        {
            Bootstrap.Init(null);
            Hacks.RenderWatermark = false;
            Chat.Print(
                "Starting <font color = \"#740000\">Volatile AIO</font> <font color = \"#B87F7F\">Heart.cs</font>:");

            VolatileMenu = MainMenu.AddMenu("V." + Player.ChampionName, "volatilemenu", "Volatile " + Player.ChampionName);

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
            foreach (var champion in ExtensionLoader.ExtensionState)
            {
                var label = champion.Key;
                for (var i = champion.Key.Length; i < 20; i++)
                    label += " ";
                label += "Status: " + champion.Value;
                VolatileMenu.AddLabel(label);
            }
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("AIO Options:");
            VolatileMenu.Add("debug", new CheckBox("Debug", false));
            VolatileMenu.Add("welcome", new CheckBox("Play 'initiated' sound", false));
            new ExtensionLoader();
            TargetMenu = VolatileMenu.AddSubMenu("Target Manager", "targetmenu", "Volatile TargetManager");
            TargetMenu.Add("chosenignores", new CheckBox("Ignore all other champions if Selected Target", false));
            ManaManager = new ManaManager();
            AutoLeveler = new AutoLeveler();
            DrawManager = new DrawManager();
            HackMenu = VolatileMenu.AddSubMenu("Hacks", "hacks", "Volatile Hacks");
            SkinManager.Initialize();
            RecallTracker = new RecallTracker();
            if (VolatileMenu["welcome"].Cast<CheckBox>().CurrentValue)
            Initiated.Play();
            Activator = new Activator();
            _championProfiles = new ChampionProfiles();
        }

        #region privatevoid

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
            var map = EloBuddy.Game.MapId;
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

        #endregion
    }
}
