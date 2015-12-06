using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Mid
{
    internal class Cassiopeia : Heart
    {
        private Menu _spellMenu;

        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Skillshot R;

        public Cassiopeia()
        {
            InitializeSpells();
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            _spellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            _spellMenu.AddGroupLabel("Q Settings");
            _spellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            _spellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            _spellMenu.Add("qthe", new CheckBox("Only Q in Harass if target has E"));
            _spellMenu.Add("qtl", new CheckBox("Use Q in Laneclear"));
            _spellMenu.Add("qtj", new CheckBox("Use Q in Jungleclear"));

            _spellMenu.AddGroupLabel("E Settings");
            _spellMenu.Add("etc", new CheckBox("Use E in Combo"));
            _spellMenu.Add("eth", new CheckBox("Use E in Harass"));
            _spellMenu.Add("focuse", new CheckBox("Always Focus E Target"));
            _spellMenu.Add("etl", new CheckBox("Use E in Laneclear", false));
            _spellMenu.Add("etj", new CheckBox("Use E in Jungleclear"));
            _spellMenu.Add("eontower", new CheckBox("Use E on Tower"));
            _spellMenu.AddLabel("Use E on: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                _spellMenu.Add("e" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }

            _spellMenu.AddGroupLabel("R Settings");
            _spellMenu.Add("agc", new CheckBox("Anti-Gapcloser"));
            _spellMenu.Add("int", new CheckBox("Interrupter"));
            _spellMenu.Add("rks", new CheckBox("SmartFinisher E+R"));
            _spellMenu.AddLabel("Use SmartFinisher On: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                _spellMenu.Add("r" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
        }

        private void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Skillshot, Initialize.Type.Targeted, Initialize.Type.Skillshot);
            Q = (Spell.Skillshot)PlayerData.Spells[0];
            W = (Spell.Skillshot)PlayerData.Spells[1];
            E = (Spell.Targeted)PlayerData.Spells[2];
            R = (Spell.Skillshot)PlayerData.Spells[3];
            Q.AllowedCollisionCount = int.MaxValue;
            W.AllowedCollisionCount = int.MaxValue;
            R.AllowedCollisionCount = int.MaxValue;
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            if (ComboActive())
            {
                Combo();
            }
            if (LaneClearActive())
            {
                LaneClear();
            }
        }

        private static void Combo()
        {
            CastManager.Cast.Circle.Optimized(Q, DamageType.Magical);
            CastManager.Cast.Circle.Optimized(W, DamageType.Magical, 0, 2);
        }

        private static void LaneClear()
        {
            CastManager.Cast.Circle.Farm(Q);
        }
    }
}