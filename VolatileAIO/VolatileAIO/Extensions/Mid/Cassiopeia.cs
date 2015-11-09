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
        }

        private static void InitializeSpells()
        {
            var spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Skillshot, Initialize.Type.Targeted, Initialize.Type.Skillshot);
            Q = (Spell.Skillshot)spells[0];
            W = (Spell.Skillshot)spells[1];
            E = (Spell.Targeted)spells[2];
            R = (Spell.Skillshot)spells[3];
            Q.AllowedCollisionCount = int.MaxValue;
            W.AllowedCollisionCount = int.MaxValue;
            R.AllowedCollisionCount = int.MaxValue;
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
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