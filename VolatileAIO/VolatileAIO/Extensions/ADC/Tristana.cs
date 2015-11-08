using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;

namespace VolatileAIO.Extensions.ADC
{
    internal class Tristana : Heart
    {
        #region Spell and Menu Declaration

        public static Spell.Active Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;

        public static Menu SpellMenu;

        #endregion

        #region Spell and Menu Loading

        public Tristana()
        {
            var spells = new Initialize().Spells(Initialize.Type.Active, Initialize.Type.Skillshot, Initialize.Type.Targeted, Initialize.Type.Targeted);
            Q = (Spell.Active)spells[0];
            W = (Spell.Skillshot)spells[1];
            E = (Spell.Targeted)spells[2];
            R = (Spell.Targeted)spells[3];
            W.AllowedCollisionCount = int.MaxValue;

            InitializeMenu();
            DrawManager.UpdateValues(Q, W, E, R);
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qthe", new CheckBox("Only Q in Harass if target has E"));
            SpellMenu.Add("qtl", new CheckBox("Use Q in Laneclear"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("focuse", new CheckBox("Always Focus E Target"));
            SpellMenu.Add("etl", new CheckBox("Use E in Laneclear"));
            SpellMenu.Add("eontower", new CheckBox("Use E on Tower"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rks", new CheckBox("SmartFinisher E+R"));
            SpellMenu.Add("agc", new CheckBox("Anti-Gapcloser"));
            SpellMenu.Add("int", new CheckBox("Interrupter"));

            SpellMenu.AddSeparator();
            SpellMenu.AddLabel("Use R On: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("r" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
        }

        #endregion
    }
}