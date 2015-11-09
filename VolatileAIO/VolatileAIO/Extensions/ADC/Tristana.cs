using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
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

            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);
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

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("focuse", new CheckBox("Always Focus E Target"));
            SpellMenu.Add("etl", new CheckBox("Use E in Laneclear", false));
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
            SpellMenu.AddSeparator();
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
            if (TickManager.NoLag(0))
            {
                E.Range = 625 + 9*((uint) Player.Level - 1);
                R.Range = 517 + 9*((uint) Player.Level - 1);
            }
        }

        private void LaneClear()
        {
            if (SpellMenu["eontower"].Cast<CheckBox>().CurrentValue)
            {
                var turret =
                    EntityManager.Turrets.Enemies.FirstOrDefault(t => !t.IsDead && t.HealthPercent > 5 && t.IsValidTarget(Player.AttackRange));
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
            var target = eTarget ?? TargetSelector.GetTarget(E.Range, DamageType.Physical);
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