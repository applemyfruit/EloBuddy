using System;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Support
{
    internal class Alistar : Heart
    {
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active E;
        public static Spell.Active R;

        public static Menu SpellMenu;

        public Alistar()
        {
            InitializeMenu();
            InitializeSpells();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass only to chain cc"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wth", new CheckBox("Use W in Harass"));
            SpellMenu.Add("wonlyq", new CheckBox("Only use W if Q is ready"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("ramount", new Slider("Enemies in range", 2, 1, 5));
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Active,
                Initialize.Type.Active, Initialize.Type.Active);
            Q = (Spell.Skillshot)PlayerData.Spells[0];
            W = (Spell.Active)PlayerData.Spells[1];
            E = (Spell.Active)PlayerData.Spells[2];
            R = (Spell.Active)PlayerData.Spells[3];
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
        }

        private void Harass()
        {
            throw new NotImplementedException();
        }

        private void Combo()
        {
            throw new NotImplementedException();
        }
    }
}