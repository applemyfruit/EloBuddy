using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;
using VolatileAIO.Organs._Test;

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
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Active, Initialize.Type.Skillshot, Initialize.Type.Targeted,
                Initialize.Type.Targeted);
            Q = (Spell.Active) PlayerData.Spells[0];
            W = (Spell.Skillshot) PlayerData.Spells[1];
            E = (Spell.Targeted) PlayerData.Spells[2];
            R = (Spell.Targeted) PlayerData.Spells[3];
            W.AllowedCollisionCount = int.MaxValue;
            InitializeMenu();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qthe", new CheckBox("Only Q in Harass if target has E"));
            SpellMenu.Add("qtl", new CheckBox("Use Q in Laneclear"));
            SpellMenu.Add("qtj", new CheckBox("Use Q in Jungleclear"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("focuse", new CheckBox("Always Focus E Target"));
            SpellMenu.Add("etl", new CheckBox("Use E in Laneclear", false));
            SpellMenu.Add("etj", new CheckBox("Use E in Jungleclear"));
            SpellMenu.Add("eontower", new CheckBox("Use E on Tower"));
            SpellMenu.AddLabel("Use E on: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("e" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("agc", new CheckBox("Anti-Gapcloser"));
            SpellMenu.Add("int", new CheckBox("Interrupter"));
            SpellMenu.Add("rks", new CheckBox("SmartFinisher E+R"));
            SpellMenu.AddLabel("Use SmartFinisher On: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("r" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
        }

        #endregion

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo ||
                Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                if (SpellMenu["focuse"].Cast<CheckBox>().CurrentValue)
                {
                    var target =
                        EntityManager.Heroes.Enemies.FirstOrDefault(
                            e => e.IsValidTarget(Player.Distance(e)) && e.HasBuff("TristanaECharge"));
                    Orbwalker.ForcedTarget = target;
                }
                else
                {
                    Orbwalker.ForcedTarget = null;
                }
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                Combo();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
                Harass();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LaneClear)
                LaneClear();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.JungleClear)
                JungleClear();
            if (TickManager.NoLag(0))
            {
                E.Range = 625 + 9*((uint) Player.Level - 1);
                R.Range = 517 + 9*((uint) Player.Level - 1);
            }
        }

        private void JungleClear()
        {
            var camp = EntityManager.MinionsAndMonsters.Monsters.Where(m => Player.Distance(m) < E.Range*1.2);
            Obj_AI_Minion target = null;
            foreach (var monster in camp)
            {
                if (target == null)
                    target = monster;
                else if (monster.MinionLevel > target.MinionLevel)
                    target = monster;
            }
            if (target == null) return;
            if (E.IsReady() && SpellMenu["etj"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                E.Cast(target);
            }
            if (Q.IsReady() && SpellMenu["qtj"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                Q.Cast();
            }
        }

        private void LaneClear()
        {
            if (SpellMenu["eontower"].Cast<CheckBox>().CurrentValue)
            {
                var turret =
                    EntityManager.Turrets.Enemies.FirstOrDefault(
                        t => !t.IsDead && t.HealthPercent > 5 && t.Distance(Player)>E.Range);
                if (turret != null)
                    E.Cast(turret);
            }
            var minions = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition,
                E.Range,
                MinionTypes.All,
                MinionTeam.NotAlly,
                MinionOrderTypes.MaxHealth);
            if (minions.Count <= 0) return;
            if (minions.Count <= 0)
            {
                return;
            }

            if (E.IsReady() && SpellMenu["etl"].Cast<CheckBox>().CurrentValue && minions.Count > 2)
            {
                foreach (var minion in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(minion => minion.IsValidTarget(Player.AttackRange)))
                {
                    E.Cast(minion);
                }
            }

            var eminion =
                minions.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(Player.AttackRange));

            if (eminion != null)
            {
                Orbwalker.ForcedTarget = eminion;
            }

            if (Q.IsReady() && SpellMenu["qtl"].Cast<CheckBox>().CurrentValue)
            {
                var eMob = minions.FindAll(x => x.IsValidTarget() && x.HasBuff("TristanaECharge"));
                if (eMob.Any())
                {
                    Q.Cast();
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (Q.IsReady() && SpellMenu["qth"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(E.Range))
            {
                if (SpellMenu["qthe"].Cast<CheckBox>().CurrentValue)
                {
                    if (target.HasBuff("TristanaECharge"))
                    {
                        Q.Cast();
                    }
                }
                else
                {
                    Q.Cast();
                }
            }

            if (E.IsReady() && SpellMenu["eth"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                if (SpellMenu["e" + target.ChampionName].Cast<CheckBox>().CurrentValue)
                    E.Cast(target);
                else if (Player.CountEnemiesInRange(1400) == 1)
                {
                    E.Cast(EntityManager.Heroes.Enemies.Find(e => Player.Distance(e) < 1400));
                }
                else
                {
                    foreach (
                        var enemy in
                            EntityManager.Heroes.Enemies.Where(
                                e =>
                                    Player.IsValidTarget(Player.Distance(e)) &&
                                    SpellMenu["e" + e.ChampionName].Cast<CheckBox>().CurrentValue))
                    {
                        E.Cast(enemy);
                    }
                }
            }
        }

        private void Combo()
        {
            var eTarget =
                EntityManager.Heroes.Enemies.Find(
                    x => x.HasBuff("TristanaECharge") && x.IsValidTarget(Player.AttackRange));
            var target = eTarget ?? TargetManager.Target(E, DamageType.Physical);
            if (target == null || !target.IsValidTarget()) return;

            if (Q.IsReady() && target.IsValid && SpellMenu["qtc"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(1))
            {
                Q.Cast();
            }
            if (E.IsReady() && SpellMenu["etc"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                if (SpellMenu["e" + target.ChampionName].Cast<CheckBox>().CurrentValue)
                    E.Cast(target);
                else if (Player.CountEnemiesInRange(1400) == 1)
                {
                    E.Cast(EntityManager.Heroes.Enemies.Find(e => Player.Distance(e) < 1400));
                }
                else
                {
                    foreach (
                        var enemy in
                            EntityManager.Heroes.Enemies.Where(
                                e =>
                                    Player.IsValidTarget(Player.Distance(e)) &&
                                    SpellMenu["e" + e.ChampionName].Cast<CheckBox>().CurrentValue))
                    {
                        E.Cast(enemy);
                    }
                }
            }
            if (R.IsReady() && SpellMenu["rks"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(4))
            {
                if ((GetEDamage(target) + Player.GetSpellDamage(target, SpellSlot.R)) >
                    Prediction.Health.GetPrediction(target, R.CastDelay) + 10)
                {
                    R.Cast(target);
                }
            }
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (!SpellMenu["agc"].Cast<CheckBox>().CurrentValue) return;
            if (sender.IsValidTarget(R.Range) && R.IsReady())
            {
                R.Cast(sender);
            }
        }

        protected override void Volatile_OnInterruptable(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (!SpellMenu["int"].Cast<CheckBox>().CurrentValue || args.DangerLevel<DangerLevel.Medium || sender.Distance(Player)>R.Range) return;
            R.Cast(sender);
        }

        private double GetEDamage(Obj_AI_Base target)
        {
            if (target.GetBuffCount("TristanaECharge") != 0)
            {
                return (Player.GetSpellDamage(target, SpellSlot.E)*((0.3*target.GetBuffCount("TristanaECharge") + 1))
                        + (Player.TotalAttackDamage) + (Player.TotalMagicalDamage*0.5));
            }
            return 0;
        }
    }
}