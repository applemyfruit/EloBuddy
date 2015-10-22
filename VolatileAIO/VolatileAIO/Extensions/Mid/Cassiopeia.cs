using System;
using EloBuddy;
using EloBuddy.SDK;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs._Test;

namespace VolatileAIO.Extensions.Mid
{
    internal class Cassiopeia : Heart
    {
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Skillshot R;

        public Cassiopeia()
        {
            InitializeSpells();
            DrawManager.UpdateValues(Q, W, E, R);
        }

        private static void InitializeSpells()
        {
            var qdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.Q);
            var wdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.Q);
            var rdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.R);
            Q = new Spell.Skillshot(SpellSlot.Q, (uint)qdata.Range, qdata.Type, qdata.Delay, qdata.MissileSpeed, qdata.Radius);
            W = new Spell.Skillshot(SpellSlot.W, (uint)wdata.Range, wdata.Type, wdata.Delay, wdata.MissileSpeed, wdata.Radius);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Skillshot(SpellSlot.R, (uint)rdata.Range, rdata.Type, rdata.Delay, rdata.MissileSpeed, rdata.Radius);
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
        }

        private static void Combo()
        {
            CastManager.Cast.Circle.WujuStyle(Q, DamageType.Magical);
            CastManager.Cast.Circle.WujuStyle(W, DamageType.Magical, 0, 2);
        }

        private static void LaneClear()
        {
            CastManager.Cast.Circle.Farm(Q);
        }
    }
}