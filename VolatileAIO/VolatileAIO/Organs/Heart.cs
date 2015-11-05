using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs.Brain;

namespace VolatileAIO.Organs
{
    internal class Heart
    {
        protected static readonly AIHeroClient Player = ObjectManager.Player;
        public static Menu VolatileMenu;
        protected static Menu _hackMenu;

        public static ManaManager ManaManager = new ManaManager();
        public static DrawManager DrawManager = new DrawManager();

        protected Heart()
        {
            Game.OnUpdate += OnUpdateDeathChecker;
            Drawing.OnEndScene += OnDrawDeathChecker;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Orbwalker.OnPostAttack += OrbwalkerOnOnPostAttack;
            Orbwalker.OnPreAttack += OrbwalkerOnOnPreAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Game.OnWndProc += Game_OnWndProc;
            AttackableUnit.OnDamage += OnDamage;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnBuffGain += OnBuffGain;
            Obj_AI_Base.OnBuffLose += OnBuffLose;
            Spellbook.OnUpdateChargeableSpell += OnUpdateChargeableSpell;
            Spellbook.OnCastSpell += OnCastSpell;
            Spellbook.OnStopCast += OnStopCast;
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

            VolatileMenu = MainMenu.AddMenu(Player.ChampionName, "volatilemenu", "Volatile " + Player.ChampionName);

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
                VolatileMenu.AddLabel(champion.Key + " Status: " + champion.Value);
            }
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("Developer Options:");
            VolatileMenu.Add("debug", new CheckBox("Debug", false));
            new ExtensionLoader();
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                CastManager.Champions.Add(new CastManager.DifferencePChamp(enemy.Name));
            }
            SkinManager.Initialize();
        }

        #region privatevoid

        private void OnUpdateDeathChecker(EventArgs args)
        {
            if (Player.IsDead) return;
            Volatile_OnHeartBeat(args);
        }

        private void OnDrawDeathChecker(EventArgs args)
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
            if ((args.Msg == (uint) WindowMessages.LeftButtonDown)) TargetManager.SetChosenTarget(args);
        }

        private void OrbwalkerOnOnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            Volatile_OnPreAttack(target, args);
        }

        private void Obj_AI_Base_OnTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            Volatile_OnTeleport(sender, args);
        }

        #endregion

        #region virtualvoid

        protected virtual void Volatile_OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_OnUpdateChargeableSpell(Spellbook sender, SpellbookUpdateChargeableSpellEventArgs args)
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

        protected virtual void Volatile_OnTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            //for extensions
        }

        #endregion

        //Checks if player is in fountain
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
    }
}
