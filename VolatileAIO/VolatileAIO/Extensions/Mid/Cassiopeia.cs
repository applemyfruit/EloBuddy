using System;
using EloBuddy;
using EloBuddy.SDK;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;
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
            CastManager.Cast.Circle.Optimized(Q, DamageType.Magical);
            CastManager.Cast.Circle.Optimized(W, DamageType.Magical, 0, 2);
        }

        private static void LaneClear()
        {
            CastManager.Cast.Circle.Farm(Q);
        }
    }
}