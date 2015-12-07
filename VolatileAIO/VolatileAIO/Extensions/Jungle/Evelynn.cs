using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Jungle
{
    internal class Evelynn : Heart
    {
        public enum AttackSpell
        {
            Q
        };

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            AutoW();
            if (Player.IsDead) return;
            AutoCastSpells();
            if (ComboActive())
            {
                Combo();
            }
            if (LastHitActive())
            {
                LastHitB();
            }
            if (LaneClearActive())
            {
                LaneClearB();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
        }

        private static void AutoW()
        {
            var useW = SpellMenu["asw"].Cast<CheckBox>().CurrentValue;

            if (Player.HasBuffOfType(BuffType.Slow) || Player.CountEnemiesInRange(550) >= 3 && useW)
            {
                W.Cast();
            }
        }

        private static void Combo()
        {
            CastSpellLogic(SpellMenu["qtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["wtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["etc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["rtc"].Cast<CheckBox>().CurrentValue);
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (SpellMenu["wtosafe"].Cast<CheckBox>().CurrentValue && W.IsReady() && sender.IsEnemy &&
                Player.Position.Extend(Game.CursorPos, Q.Range).CountEnemiesInRange(400) < 3)
            {
                if (sender.IsValidTarget(E.Range))
                {
                    W.Cast();
                }
            }
        }

        private static void CastSpellLogic(bool useQ = false, bool useW = false, bool useE = false,
            bool useR = false)
        {
            if (useQ)
            {
                var enemy = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (enemy != null && enemy.IsValidTarget(Q.Range))
                    Q.Cast();
            }

            if (useW)
            {
                if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue && Player.CountEnemiesInRange(700) >= 1)
                    W.Cast();
            }
            if (useE && E.IsReady() && TickManager.NoLag(3))
            {
                var enemy = TargetSelector.GetTarget(E.Range, DamageType.Mixed);

                if (enemy != null && enemy.IsValidTarget(E.Range))
                    E.Cast(enemy);
            }
            if (useR && R.IsReady() && TickManager.NoLag(4))
            {
                if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue)
                {
                    var target = TargetManager.Target(650, DamageType.Magical);
                    if (target == null || !target.IsValidTarget(R.Range) ||
                        !TickManager.NoLag(4) ||
                        !R.IsReady())
                        return;
                    {
                        var champs =
                            EntityManager.Heroes.Enemies.Where(e => e.Distance(target) < R.Radius)
                                .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                                .ToList();
                        var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 1025);
                        if (location.ChampsHit < SpellMenu["rslider"].Cast<Slider>().CurrentValue) return;
                        {
                            R.Cast(location.Position.To3D());
                        }
                    }
                }
            }
        }

        private static void Flee()
        {
            Orbwalker.MoveTo(Game.CursorPos);
            W.Cast();
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
                            CastSpellLogic(true, true, false, true);
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
            }
        }

        private static float Qcalc(Obj_AI_Base target)
        {
            {
                return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                    new float[] { 0, 40, 50, 60, 70, 80 }[Q.Level] +
                    new float[] { 0, 35, 40, 45, 50, 55 }[Q.Level] / 100 * EloBuddy.Player.Instance.FlatMagicDamageMod +
                    new float[] { 0, 50, 55, 60, 65, 70 }[Q.Level] / 100 * EloBuddy.Player.Instance.FlatPhysicalDamageMod);
            }
        }

        private static Obj_AI_Base GetEnemy(float range, GameObjectType t)
        {
            switch (t)
            {
                case GameObjectType.AIHeroClient:
                    return EntityManager.Heroes.Enemies.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
                default:
                    return EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health).FirstOrDefault(
                        a =>
                            a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable &&
                            a.Health <= Qcalc(a));
            }
        }

        private static Obj_AI_Base GetClearEnemy(float range, GameObjectType t)
        {
            switch (t)
            {
                case GameObjectType.AIHeroClient:
                    return EntityManager.Heroes.Enemies.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
                default:
                    return EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health).FirstOrDefault(
                        a =>
                            a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
            }
        }

        public static void LastHitB()
        {
            var qcheck = SpellMenu["lhq"].Cast<CheckBox>().CurrentValue;
            var qready = Q.IsReady();
            if (!qcheck || !qready) return;
            var minion = (Obj_AI_Minion)GetEnemy(Q.Range, GameObjectType.obj_AI_Minion);
            if (minion != null)
            {
                Q.Cast();
            }
        }

        public static void LaneClearB()
        {
            var qcheck = SpellMenu["lcq"].Cast<CheckBox>().CurrentValue;
            var qready = Q.IsReady();
            var echeck = SpellMenu["lce"].Cast<CheckBox>().CurrentValue;
            var eready = E.IsReady();
            if (!qcheck || !qready) return;
            var qminion = (Obj_AI_Minion)GetClearEnemy(Q.Range, GameObjectType.obj_AI_Minion);
            if (qminion != null)
            {
                Q.Cast();
            }
            if (!echeck || !eready) return;
            {
                var eminion = (Obj_AI_Minion)GetClearEnemy(E.Range, GameObjectType.obj_AI_Minion);

                if (eminion != null)
                    E.Cast(eminion);
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
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("lhq", new CheckBox("Use Q to Lasthit"));
            SpellMenu.Add("lcq", new CheckBox("Use Q to Laneclear"));
            SpellMenu.Add("qon", new CheckBox("Auto Q if enemy is near"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wtosafe", new CheckBox("Anti-Gapcloser W"));
            SpellMenu.Add("wt2", new CheckBox("Auto W if at least 2 enemy is near"));
            SpellMenu.Add("asw", new CheckBox("Auto W to Remove Slow"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("lce", new CheckBox("Use E to Laneclear"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo"));
            SpellMenu.AddSeparator();
            SpellMenu.Add("rslider", new Slider("Minimum people for R", 2, 0, 5));
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Active, Initialize.Type.Active,
                Initialize.Type.Targeted, Initialize.Type.Skillshot);
            Q = (Spell.Active)PlayerData.Spells[0];
            W = (Spell.Active)PlayerData.Spells[1];
            E = (Spell.Targeted)PlayerData.Spells[2];
            R = (Spell.Skillshot)PlayerData.Spells[3];
            R.AllowedCollisionCount = int.MaxValue;
        }

        #endregion
    }
}