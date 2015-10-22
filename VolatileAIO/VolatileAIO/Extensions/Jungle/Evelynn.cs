using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;

namespace VolatileAIO.Extensions.Jungle
{
    internal class Evelynn : Heart
    {
        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            if (Player.IsDead) return;
            AutoCastSpells();
            ManaManager.SetMana();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
        }

        private static void Combo()
        {
            CastSpellLogic(SpellMenu["qtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["wtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["etc"].Cast<CheckBox>().CurrentValue);
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (SpellMenu["wtosafe"].Cast<CheckBox>().CurrentValue && W.IsReady() && sender.IsEnemy &&
                Player.Mana > ManaManager.ManaQ + ManaManager.ManaR &&
                Player.Position.Extend(Game.CursorPos, Q.Range).CountEnemiesInRange(400) < 3)
            {
                if (sender.IsValidTarget(E.Range))
                {
                    W.Cast();
                }
            }
        }

        private static void CastSpellLogic(bool useQ = false, bool useW = false, bool useE = false,
            Obj_AI_Base target = null)
        {
            if (useQ)
            {
                var enemy = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (enemy != null && enemy.IsValidTarget(Q.Range))
                    Q.Cast();
            }

            if (useW)
            {
                if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue && Player.CountEnemiesInRange(700) >= 2)
                    W.Cast();
            }
            if (useE && E.IsReady() && TickManager.NoLag(3))
            {
                var enemy = TargetSelector.GetTarget(E.Range, DamageType.Mixed);

                if (enemy != null && enemy.IsValidTarget(E.Range))
                    E.Cast(enemy);
            }
        }

        private static void AutoCastSpells()
        {
            if (Q.IsReady() && TickManager.NoLag(0))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(Q.Range)))
                {
                    if (enemy.IsEnemy && enemy.IsValidTarget(Q.Range) && !enemy.IsDead)
                    {
                        if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                            Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                Player.GetSpellDamage(Player, SpellSlot.Q)))
                        {
                            CastSpellLogic(true, false, false, enemy);
                            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("TRY KS Q");
                        }
                    }

                    if
                        (enemy.Distance(Player) >= Q.Range &&
                         SpellMenu["qon"].Cast<CheckBox>().CurrentValue)
                    {
                        Q.Cast();
                    }
                }
            }
            if (W.IsReady() && TickManager.NoLag(2))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(700)))
                {
                    if (enemy.IsEnemy && SpellMenu["wt2"].Cast<CheckBox>().CurrentValue &&
                        Player.CountEnemiesInRange(700) >= 2 && !enemy.IsDead)
                    {
                        W.Cast();
                    }
                }

                if (R.IsReady() && TickManager.NoLag(4))
                {
                    if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue)
                        foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(900)))
                        {
                            if (Prediction.Health.GetPrediction(enemy, R.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.R)))
                            {
                                    CastManager.Cast.Circle.WujuStyle(R, DamageType.Magical, 0, SpellMenu["rslider"].Cast<Slider>().CurrentValue, HitChance.Medium, enemy);
                            }
                        }
                }
            }
        }

        #region Spell and Menu Declaration

        public static int Mode;
        public static bool IsAutoAttacking;

        public static Spell.Active Q;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Skillshot R;

        public static Menu SpellMenu;

        #endregion

        #region Spell and Menu Loading

        public Evelynn()
        {
            InitializeSpells();
            InitializeMenu();
            DrawManager.UpdateValues(Q, W, E, R);
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("useQTL", new CheckBox("Use Q in farm"));
            SpellMenu.Add("qon", new CheckBox("Auto Q if enemy is near"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wtosafe", new CheckBox("Anti-Gapcloser W"));
            SpellMenu.Add("wt2", new CheckBox("Auto W if at least 2 enemy is near"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("useETL", new CheckBox("Use E in farm"));
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo"));
            SpellMenu.AddSeparator();
            SpellMenu.Add("rslider", new Slider("Minimum people for R", 1, 0, 5));
        }

        public static void InitializeSpells()
        {
            Q = new Spell.Active(SpellSlot.Q, 475);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 225);
            R = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Circular, 250, 1200, 150)
            {
                AllowedCollisionCount = int.MaxValue
            };
        }

        #endregion
    }
}