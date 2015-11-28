using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;
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

        public static Menu SpellMenu;

        public Blitzcrank()
        {
            InitializeMenu();
            InitializeSpells();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qontop", new CheckBox("Auto Q top priority target(common ts)"));
            SpellMenu.Add("qonimmo", new CheckBox("Auto Q on Immobile Targets"));
            SpellMenu.AddLabel("Never Grab");
            foreach (var hero in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("dontgrab" + hero.ChampionName.ToLower(),
                    TargetSelector.GetPriority(hero) <= 2
                        ? new CheckBox(hero.ChampionName)
                        : new CheckBox(hero.ChampionName, false));
            }

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo", false));
            SpellMenu.Add("wtj", new CheckBox("Use W to clear jungle", false));
            SpellMenu.Add("wtpush", new CheckBox("Use W to push towers and clear wards faster"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("etj", new CheckBox("Use E to clear jungle", false));
            SpellMenu.Add("etpush", new CheckBox("Use E to push towers and clear wards faster"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtks", new CheckBox("Use R Finisher", false));
            SpellMenu.Add("rcmode", new Slider("ComboMode - Q->E->R", 0, 0, 2)).OnValueChange +=
                Blitzcrank_OnComboModeChanged;
            SpellMenu.Add("ramount", new Slider("Enemies in range", 2, 1, 5)).IsVisible = false;
            UpdateSlider();
        }

        private static void Blitzcrank_OnComboModeChanged(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSlider();
        }

        private static void UpdateSlider()
        {
            if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 0)
            {
                SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - Q->E->R";
                SpellMenu["ramount"].Cast<Slider>().IsVisible = false;
            }
            else if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 1)
            {
                SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - R if ";
                SpellMenu["ramount"].Cast<Slider>().IsVisible = true;
            }
            else if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 2)
            {
                SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - Dont use R in combo";
                SpellMenu["ramount"].Cast<Slider>().IsVisible = false;
            }
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Active,
                Initialize.Type.Active, Initialize.Type.Active);
            Q = (Spell.Skillshot) PlayerData.Spells[0];
            W = (Spell.Active) PlayerData.Spells[1];
            E = (Spell.Active) PlayerData.Spells[2];
            R = (Spell.Active) PlayerData.Spells[3];
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            AutoCast();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (target.IsWard() || target.IsStructure())
            {
                if (SpellMenu["etpush"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    E.Cast();
                }
                if (SpellMenu["wtpush"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    W.Cast();
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && MinionManager.GetMinions(Player.Position, 400, MinionTypes.All, MinionTeam.Neutral).Any())
            {
                if (SpellMenu["etj"].Cast<CheckBox>().CurrentValue && E.IsReady())
                {
                    E.Cast();
                }
                if (SpellMenu["wtj"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    W.Cast();
                }
            }
        }

        private static void Harass()
        {
            var target = TargetManager.Target(Q, DamageType.Magical);
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue &&
                !SpellMenu["dontgrab" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                Player.Distance(target) > Player.GetAutoAttackRange())
                CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);

            if (SpellMenu["eth"].Cast<CheckBox>().CurrentValue && E.IsReady() && TickManager.NoLag(3))
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
        }

        protected override void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe) return;
            if (args.Slot == SpellSlot.E) Orbwalker.ResetAutoAttack();
        }

        private static void AutoCast()
        {
            if (SpellMenu["qontop"].Cast<CheckBox>().CurrentValue)
            {
                var max = EntityManager.Heroes.Enemies.Max(t => TargetSelector.GetPriority(t));
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            e =>
                                e.IsValidTarget(Q.Range) &&
                                !SpellMenu["dontgrab" + e.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue)
                            .Where(enemy => TargetSelector.GetPriority(enemy) == max))
                {
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical, (int) Q.Range, HitChance.High, enemy);
                }
            }
            if (SpellMenu["qonimmo"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e => !SpellMenu["dontgrab" + e.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                             e.IsValidTarget(Q.Range) &&
                             (Q.GetPrediction(e).HitChance == HitChance.Dashing ||
                              Q.GetPrediction(e).HitChance == HitChance.Immobile));
                if (enemy != null)
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical, (int) Q.Range, HitChance.High, enemy);
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
                    (!SpellMenu["dontgrab" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue ||
                     (SpellMenu["dontgrab" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                      TargetManager.ChosenTarget == target)) && Player.Distance(target) > Player.GetAutoAttackRange())
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);

            if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue && W.IsReady() && TickManager.NoLag(2))
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 400);
                if (enemy != null)
                {
                    W.Cast();
                }
            }

            if (SpellMenu["etc"].Cast<CheckBox>().CurrentValue && E.IsReady() && TickManager.NoLag(3))
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
            if (TickManager.NoLag(4))
            {
                if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 0 &&
                    EntityManager.Heroes.Enemies.Exists(
                        e => Player.Distance(e) < R.Range && e.HasBuffOfType(BuffType.Knockup)) && R.IsReady())
                {
                    R.Cast();
                }
                if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 1 &&
                    EntityManager.Heroes.Enemies.Count(e => Player.Distance(e) < R.Range) >=
                    SpellMenu["ramount"].Cast<Slider>().CurrentValue && R.IsReady())
                {
                    R.Cast();
                }
            }
        }
    }
}