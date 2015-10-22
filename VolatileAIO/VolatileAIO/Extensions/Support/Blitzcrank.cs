using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs._Test;
using Color = System.Drawing.Color;

namespace VolatileAIO.Extensions.Support
{
    internal class Blitzcrank : Heart
    {
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active E;
        public static Spell.Active R;

        public Blitzcrank()
        {
            InitializeMenu();
            InitializeSpells();
            DrawManager.UpdateValues(Q, W, E, R);
        }
         
        private static void InitializeMenu()
        {

        }

        public static void InitializeSpells()
        {
            var qdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.Q);

            Q = new Spell.Skillshot(SpellSlot.Q, (uint)qdata.Range, qdata.Type, qdata.Delay, qdata.MissileSpeed, qdata.Radius);
            W = new Spell.Active(SpellSlot.W, 150);
            E = new Spell.Active(SpellSlot.E, 150);
            R = new Spell.Active(SpellSlot.R, 550);
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            AutoCast();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
        }

        private static void AutoCast()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(e=>e.IsValidTarget(Q.Range)).Where(enemy => TargetSelector.GetPriority(enemy) > 3))
            {
                CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical, (int)Q.Range, HitChance.High, enemy);
            }
        }

        private static void Combo()
        {
            CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
            if (E.IsReady() && TickManager.NoLag(3))
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 300);
                if (enemy != null)
                {
                    Orbwalker.DisableMovement = true;
                    Orbwalker.DisableAttacking = true;
                    E.Cast();
                    EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, enemy);
                    Orbwalker.DisableMovement = false;
                    Orbwalker.DisableAttacking = false;
                }
            }
            if (EntityManager.Heroes.Enemies.Exists(e => Player.Distance(e) < R.Range && e.HasBuffOfType(BuffType.Knockup)) && R.IsReady())
            {
                R.Cast();
            }
        }
    }
}