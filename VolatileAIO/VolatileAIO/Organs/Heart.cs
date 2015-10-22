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
            VolatileMenu.AddLabel("Ezreal");
            VolatileMenu.AddLabel("Blitzcrank");
            VolatileMenu.AddLabel("Evelynn");
            VolatileMenu.AddSeparator();
            VolatileMenu.AddLabel("Developer Options:");
            VolatileMenu.Add("debug", new CheckBox("Debug", false));
            new ExtensionLoader();
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                CastManager.Champions.Add(new CastManager.DifferencePChamp(enemy.Name));
            }
        }

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

        protected virtual void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            //for addons
        }

        private void OrbwalkerOnOnPostAttack(AttackableUnit target, EventArgs args)
        {
            Volatile_OnPostAttack(target, args);
        }

        protected virtual void Volatile_OnHeartBeat(EventArgs args)
        {
            //for addons
        }

        private void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            Volatile_OnDamage(sender,args);
        }

        private void OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            //for addons
        }

        private void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            OnSpellCast(sender, args);
        }

        private void OnUpdateChargeableSpell(Spellbook sender, SpellbookUpdateChargeableSpellEventArgs args)
        {
            //for addons
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //for addons
        }

        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            //for addons
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            //for addons
        }

        private void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
                Volatile_AntiGapcloser(sender, e);
        }

        private void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            //for addons
        }

        private void OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            //for addons
        }

        private void OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            //for addons
        }

        protected virtual void Volative_OnDraw(EventArgs args)
        {
            //for addons
        }

        private void OrbwalkerOnOnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            Volatile_OnPreAttack(target, args);
        }

        protected virtual void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            //for addons
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            Volatile_OnWndProc(args);
            if ((args.Msg == (uint)WindowMessages.LeftButtonDown)) TargetManager.SetChosenTarget(args);
        }

        protected virtual void Volatile_OnWndProc(WndEventArgs args)
        {
            //for extensions
        }

        protected virtual void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            //for extensions
        }

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

        protected virtual void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            //
        }

        protected virtual void Volatile_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            //
        }
    }
}
