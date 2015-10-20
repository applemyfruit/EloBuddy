using System;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;

namespace VolatileAIO.Extensions.Support
{
    internal class Blitzcrank : Heart
    {
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active E;
        public static Spell.Active R;

        private readonly DrawManager _drawManager = new DrawManager();

        public Blitzcrank()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1000, SkillShotType.Linear, (int)250f, (int)1800f, (int)70f);
            W = new Spell.Active(SpellSlot.W, 150);
            E = new Spell.Active(SpellSlot.E, 150);
            R = new Spell.Active(SpellSlot.R, 550);
            _drawManager.UpdateValues(Q, W, E, R);
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
        }
    }
}