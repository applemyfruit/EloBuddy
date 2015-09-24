using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;

namespace TKogMaw
{
    //Created by turkey for the EloBuddy Community! =)
    internal class Program
    {
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Skillshot E;
        public static Spell.Skillshot R;
        public static Menu KogMenu;
        public static AIHeroClient Me = ObjectManager.Player;

        private static int GetRRange
        {
            get { return 900 + (R.Level*300); }
        }

        private static int GetWRange
        {
            get { return 565 + 110 + (W.Level*20); }
        }

        private static float GetRDamage
        {
            get { return 40 + (R.Level*40) + 0.3f*Me.FlatMagicDamageMod + 0.5f*Me.FlatPhysicalDamageMod; }
        }

        private static int GetRStacks()
        {
            return (from buff in ObjectManager.Player.Buffs
                    where buff.DisplayName.ToLower() == "kogmawlivingartillery"
                    select buff.Count).FirstOrDefault();
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoaded;
        }

        private static void OnLoaded(EventArgs args)
        {
            Bootstrap.Init(null);
            Q = new Spell.Skillshot(SpellSlot.Q, (int) 900f, SkillShotType.Linear, (int) 250f, (int) 1650f, (int) 60f)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, (int) 1260f, SkillShotType.Linear, (int) 500f, (int) 1400f, (int) 120f)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, 20000, SkillShotType.Circular, (int) 1200f, 9999999,
                (int) 120f)
            {
                AllowedCollisionCount = int.MaxValue
            };

            KogMenu = MainMenu.AddMenu("T.KogMaw", "tkog");
            KogMenu.AddGroupLabel("T.KogMaw");
            KogMenu.AddSeparator();
            KogMenu.AddLabel("An AddOn made by EB user, turkey :)");
            KogMenu.Add("mode", new CheckBox("Current Mode: AD", true));
            KogMenu.AddSeparator();

            KogMenu.Add("rlimit", new Slider("R Limiter", 4, 0, 6));
            KogMenu.AddSeparator();
            
            KogMenu.Add("ksr", new CheckBox("R to KS"));
            KogMenu.Add("kse", new CheckBox("E to KS"));
            KogMenu.Add("egap", new CheckBox("E if target is out of W range"));

            KogMenu.AddSeparator();
            KogMenu.Add("drawr", new CheckBox("Draw R"));

            Game.OnTick += Tick;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnPostAttack += AfterAttack;
            Orbwalker.OnPreAttack += BeforeAttack;
        }

        private static void BeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
        }

        private static void AfterAttack(AttackableUnit target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                var rTarget = TargetSelector.GetTarget(GetRRange, DamageType.Magical);
                if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                {
                    if (qTarget.IsValidTarget(Q.Range) && Q.GetPrediction(qTarget).HitChance >= HitChance.Medium &&
                        Q.IsReady())
                    {
                        Q.Cast(qTarget);
                    }
                    else if (eTarget.IsValidTarget(E.Range) && E.GetPrediction(eTarget).HitChance >= HitChance.High &&
                             E.IsReady())
                    {
                        E.Cast(eTarget);
                    }
                    else if (GetRStacks() < KogMenu["rlimit"].Cast<Slider>().CurrentValue && rTarget.IsValidTarget(GetRRange) && R.GetPrediction(rTarget).HitChance >= HitChance.High &&
                             R.IsReady())
                    {
                        R.Cast(rTarget);
                    }
                }
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                var rTarget = TargetSelector.GetTarget(GetRRange, DamageType.Magical);
                if (GetRStacks() < KogMenu["rlimit"].Cast<Slider>().CurrentValue && rTarget.IsValidTarget(GetRRange) && R.GetPrediction(rTarget).HitChance >= HitChance.High &&
                             R.IsReady())
                {
                    R.Cast(rTarget);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Me.IsDead)
            {
                if (KogMenu["drawr"].Cast<CheckBox>().CurrentValue && R.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, GetRRange, Color.White);
                }
            }
        }

        private static void Tick(EventArgs args)
        {
            ModeSwitch();
            KillSecure();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                Harass();   
            }
        }

        private static void Harass()
        {
            var rTarget = TargetSelector.GetTarget(GetRRange, DamageType.Magical);
            if (R.IsReady() && rTarget.IsValidTarget(R.Range) && R.GetPrediction(rTarget).HitChance >= HitChance.High)
            {
                R.Cast(rTarget);
            }
            if (W.IsReady())
            {
                var wTarget = TargetSelector.GetTarget(GetWRange, DamageType.Physical);
                if (wTarget != null && Me.Distance(wTarget) < GetWRange*0.9)
                {
                    W.Cast();
                }
            }
        }

        private static void ModeSwitch()
        {
            if (KogMenu["mode"].Cast<CheckBox>().CurrentValue)
            {
                KogMenu["mode"].Cast<CheckBox>().DisplayName = "Current Mode: AD";
                KogMenu["kse"].Cast<CheckBox>().IsVisible = false;
                KogMenu["egap"].Cast<CheckBox>().IsVisible = true;
            }
            else
            {
                KogMenu["mode"].Cast<CheckBox>().DisplayName = "Current Mode: AP";
                KogMenu["kse"].Cast<CheckBox>().IsVisible = true;
                KogMenu["egap"].Cast<CheckBox>().IsVisible = false;
            }
        }


        private static void KillSecure()
        {
            if (R.IsReady())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                enemy =>
                                    enemy.IsEnemy && enemy.IsValid && !enemy.IsDead && enemy.Distance(Me) < GetRRange))
                {
                    if (Prediction.Health.GetPrediction(enemy, R.CastDelay*1000) <
                        Me.CalculateDamageOnUnit(enemy, DamageType.Magical, GetRDamage)*2)
                    {
                                   Prediction.Health.GetPrediction(enemy, R.CastDelay*1000);
                        if (enemy != null)
                        R.Cast(enemy);
                    }
                }
            }
        }

        private static void Combo()
        {

            if (!KogMenu["mode"].Cast<CheckBox>().CurrentValue)
            {
                var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                var rTarget = TargetSelector.GetTarget(GetRRange, DamageType.Magical);
                if (Q.IsReady() && qTarget.IsValidTarget(Q.Range) && Q.GetPrediction(qTarget).HitChance >= HitChance.High) { Q.Cast(qTarget); return; }
                if (E.IsReady() && eTarget.IsValidTarget(E.Range) && E.GetPrediction(eTarget).HitChance >= HitChance.High) { E.Cast(eTarget); return; }
                if (R.IsReady() && rTarget.IsValidTarget(R.Range) && R.GetPrediction(rTarget).HitChance >= HitChance.High) { R.Cast(rTarget); return; }
            }
            else
            {
                if (KogMenu["egap"].Cast<CheckBox>().CurrentValue)
                {
                    var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                    if (eTarget.IsValidTarget(E.Range) && Me.Distance(eTarget) > GetWRange && E.IsReady() && E.GetPrediction(eTarget).HitChance >= HitChance.High)
                    {
                        E.Cast(eTarget);
                    }
                }
            }

            var wTarget = TargetSelector.GetTarget(GetWRange, DamageType.Physical);
            if (wTarget != null && Me.Distance(wTarget)<GetWRange*0.9)
            {
                W.Cast();
            }
        }
    }
}
