using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Support
{
    internal class Morgana : Heart
    {
        public enum AttackSpell
        {
            Q
        };

        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Active R;
        public static Menu SpellMenu;

        public Morgana()
        {
            InitializeMenu();
            InitializeSpells();
        }

        public static float Qcalc(Obj_AI_Base target)
        {
            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] {0, 80, 135, 190, 245, 300}[Q.Level] + (0.90f*Player.FlatMagicDamageMod)));
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("intq", new CheckBox("Interrupt Spells with Q"));
            SpellMenu.Add("qtlh", new CheckBox("Last Hit with Q"));
            SpellMenu.Add("peel", new CheckBox("Peel from Melee Champions"));
            SpellMenu.Add("qonimmo", new CheckBox("Auto Q on Immobile Targets"));
            SpellMenu.AddLabel("Never Bind");
            foreach (var hero in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("dontbind" + hero.ChampionName.ToLower(),
                    TargetSelector.GetPriority(hero) <= 2
                        ? new CheckBox(hero.ChampionName)
                        : new CheckBox(hero.ChampionName, false));
            }

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wonimmo", new CheckBox("Auto W on Immobile Targets"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("autoe", new CheckBox("Auto E"));
            foreach (var obj in ObjectManager.Get<AIHeroClient>().Where(obj => obj.Team == Player.Team))
            {
                SpellMenu.Add("shield" + obj.ChampionName.ToLower(), new CheckBox("Shield " + obj.ChampionName));
            }
            SpellMenu.Add("antigapcloser", new CheckBox("Anti Gapcloser E"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtks", new CheckBox("Use R Finisher", false));
            SpellMenu.Add("rcmode", new Slider("ComboMode - Q->E->R", 0, 0, 1)).OnValueChange +=
                Morgana_OnComboModeChanged;
            SpellMenu.Add("ramount", new Slider("Enemies in range", 2, 1, 5)).IsVisible = false;
            UpdateSlider();

            SpellMenu.AddGroupLabel("Other Settings");
            SpellMenu.Add("support", new CheckBox("Support Mode (Disable Minion Hit)", false));
        }

        private static void Morgana_OnComboModeChanged(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSlider();
        }

        private static void UpdateSlider()
        {
            if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 0)
            {
                SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - R if ";
                SpellMenu["ramount"].Cast<Slider>().IsVisible = true;
            }
            else if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 1)
            {
                SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - Dont use R in combo";
                SpellMenu["ramount"].Cast<Slider>().IsVisible = false;
            }
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Skillshot,
                Initialize.Type.Targeted, Initialize.Type.Active);
            Q = (Spell.Skillshot) PlayerData.Spells[0];
            W = (Spell.Skillshot) PlayerData.Spells[1];
            E = (Spell.Targeted) PlayerData.Spells[2];
            R = (Spell.Active) PlayerData.Spells[3];
            W.AllowedCollisionCount = int.MaxValue;
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            AutoCast();
            if (SpellMenu["peel"].Cast<CheckBox>().CurrentValue) Peel();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit) LastHitB();
        }

        protected override void Volatile_ProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            if (sender.Type != Player.Type || !E.IsReady() || !sender.IsEnemy ||
                !SpellMenu["autoe"].Cast<CheckBox>().CurrentValue)
                return;
            foreach (var ally in EntityManager.Heroes.Allies.Where(x => x.IsValidTarget(E.Range)))
            {
                var detectRange = ally.ServerPosition +
                                  (args.End - ally.ServerPosition).Normalized()*ally.Distance(args.End);
                if (detectRange.Distance(ally.ServerPosition) > ally.AttackRange - ally.BoundingRadius)
                    continue;
                {
                    if (!args.SData.IsAutoAttack())
                    {
                        if (CCDataBase.IsCC_SkillShot(args.SData.Name) &&
                            (SpellMenu["Shield" + ally.ChampionName].Cast<CheckBox>().CurrentValue))
                        {
                            E.Cast(ally);
                        }
                    }
                    if (CCDataBase.IsCC_NonSkillShot(args.SData.Name))
                    {
                        if (ally.Distance(args.End) < 365 &&
                            (SpellMenu["Shield" + ally.ChampionName].Cast<CheckBox>().CurrentValue))
                        {
                            E.Cast(ally);
                        }
                    }
                }
            }
        }


        protected override void Volatile_OnInterruptable(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs args)
        {
            var intTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            {
                if (Q.IsReady() && sender.IsValidTarget(Q.Range) && SpellMenu["intq"].Cast<CheckBox>().CurrentValue)
                    Q.Cast(intTarget.ServerPosition);
            }
        }

        private static void Peel()
        {
            if (SpellMenu["peel"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var pos in from enemy in ObjectManager.Get<Obj_AI_Base>()
                    where
                        enemy.IsValidTarget() &&
                        enemy.Distance(ObjectManager.Player) <=
                        enemy.BoundingRadius + enemy.AttackRange + ObjectManager.Player.BoundingRadius &&
                        enemy.IsMelee
                    let direction =
                        (enemy.ServerPosition.To2D() - ObjectManager.Player.ServerPosition.To2D()).Normalized()
                    let pos = ObjectManager.Player.ServerPosition.To2D()
                    select pos + Math.Min(200, Math.Max(50, enemy.Distance(ObjectManager.Player)/2))*direction)
                {
                    Q.Cast(pos.To3D());
                }
            }
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs a)
        {
            var agapcloser = SpellMenu["antigapcloser"].Cast<CheckBox>().CurrentValue;
            var antigapc = E.IsReady() && agapcloser;
            if (antigapc)
            {
                if (sender.IsMe)
                {
                    var gap = a.Sender;
                    if (gap.IsValidTarget(4000))
                    {
                        E.Cast(Player);
                    }
                }
            }
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) ||
                (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) ||
                 Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)))
            {
                var t = target as Obj_AI_Minion;
                if (t != null)
                {
                    {
                        if (SpellMenu["support"].Cast<CheckBox>().CurrentValue)
                            args.Process = false;
                    }
                }
            }
        }

        private static void Harass()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) return;
            var target = TargetManager.Target(Q, DamageType.Magical);
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue &&
                !SpellMenu["dontbind" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                Player.Distance(target) > Player.GetAutoAttackRange())
                CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
        }

        private static void AutoCast()
        {
            if (SpellMenu["qonimmo"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e => !SpellMenu["dontbind" + e.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                             e.IsValidTarget(Q.Range) &&
                             Q.GetPrediction(e).HitChance == HitChance.Immobile);
                if (enemy != null)
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical, (int) Q.Range, HitChance.Immobile,
                        enemy);
            }
            if (SpellMenu["wonimmo"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(e =>
                        e.IsValidTarget(W.Range) &&
                        W.GetPrediction(e).HitChance == HitChance.Immobile);
                if (enemy != null)
                    CastManager.Cast.Circle.Optimized(W, DamageType.Magical, (int) W.Range, 1, HitChance.Immobile, enemy);
            }
            if (SpellMenu["rtks"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e =>
                            e.IsValidTarget(R.Range) &&
                            Player.GetSpellDamage(e, SpellSlot.R) >= Prediction.Health.GetPrediction(e, R.CastDelay));
                if (enemy != null)
                    R.Cast();
            }
        }

        private static void Combo()
        {
            var target = TargetManager.Target(Q, DamageType.Magical);
            if (target != null)
                if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue &&
                    (!SpellMenu["dontbind" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue ||
                     (SpellMenu["dontbind" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                      TargetManager.ChosenTarget == target)) && Player.Distance(target) > Player.GetAutoAttackRange())
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
            {
                if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 0 &&
                    EntityManager.Heroes.Enemies.Count(e => Player.Distance(e) < R.Range) >=
                    SpellMenu["ramount"].Cast<Slider>().CurrentValue && R.IsReady())
                {
                    R.Cast();
                }
            }
        }

        public static Obj_AI_Base MinionLh(GameObjectType type, AttackSpell spell)
        {
            return
                ObjectManager.Get<Obj_AI_Base>()
                    .OrderBy(a => a.Health)
                    .FirstOrDefault(
                        a =>
                            a.IsEnemy && a.Type == type && a.Distance(Player) <= Q.Range && !a.IsDead &&
                            !a.IsInvulnerable && a.IsValidTarget(Q.Range) && a.Health <= Qcalc(a));
        }

        public static void LastHitB()
        {
            var qcheck = SpellMenu["qtlh"].Cast<CheckBox>().CurrentValue;
            var qready = Q.IsReady();
            if (qcheck && qready)
            {
                var minion = (Obj_AI_Minion) MinionLh(GameObjectType.obj_AI_Minion, AttackSpell.Q);
                if (minion != null)
                    if (Q.MinimumHitChance >= HitChance.Medium)
                    {
                        Q.Cast(minion.ServerPosition);
                    }
            }
        }
    }
}