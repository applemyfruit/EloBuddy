using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Mid
{
    internal class Ziggs : Heart
    {
        private static Spell.Skillshot W, R, Q, E, Q2, Q3;

        public static Menu SpellMenu;

        public Ziggs()
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
            SpellMenu.Add("qtl", new CheckBox("Use Q in Laneclear"));
            SpellMenu.Add("ksq", new CheckBox("Use Q to Killsteal"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("peel", new CheckBox("Peel from Melee Enemies"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo"));
            SpellMenu.Add("autoult", new CheckBox("Auto Ult"));
            SpellMenu.Add("auamount", new Slider("Minimum enemies hit to Auto R", 3, 1, 5));

            SpellMenu.AddGroupLabel("Prediction Settings");
            SpellMenu.AddGroupLabel("Q Hitchance");
            var qslider = SpellMenu.Add("hQ", new Slider("Q HitChance", 2, 0, 2));
            var qMode = new[] {"Low (Fast Casting)", "Medium", "High (Slow Casting)"};
            qslider.DisplayName = qMode[qslider.CurrentValue];

            qslider.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = qMode[changeArgs.NewValue];
                };
            SpellMenu.AddGroupLabel("W Hitchance");
            var wslider = SpellMenu.Add("hW", new Slider("W HitChance", 1, 0, 2));
            var wMode = new[] {"Low (Fast Casting)", "Medium", "High (Slow Casting)"};
            wslider.DisplayName = wMode[wslider.CurrentValue];

            wslider.OnValueChange +=
                delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = wMode[changeArgs.NewValue];
                };
            SpellMenu.AddGroupLabel("E Hitchance");
            var eslider = SpellMenu.Add("hE", new Slider("E HitChance", 2, 0, 2));
            var eMode = new[] { "Low (Fast Casting)", "Medium", "High (Slow Casting)" };
            eslider.DisplayName = eMode[eslider.CurrentValue];

            eslider.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = eMode[changeArgs.NewValue];
                };
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Skillshot,
                Initialize.Type.Skillshot, Initialize.Type.Skillshot);
            Q = (Spell.Skillshot) PlayerData.Spells[0];
            Q2 = new Spell.Skillshot(SpellSlot.Q, 1125, SkillShotType.Circular, 250 + Q.CastDelay, 1700, 130);
            Q3 = new Spell.Skillshot(SpellSlot.Q, 1400, SkillShotType.Circular, 300 + Q2.CastDelay, 1700, 140);
            W = (Spell.Skillshot) PlayerData.Spells[1];
            E = (Spell.Skillshot) PlayerData.Spells[2];
            R = (Spell.Skillshot) PlayerData.Spells[3];
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                Combo();
            if (SpellMenu["autoult"].Cast<CheckBox>().CurrentValue) AutoR();
            if (SpellMenu["peel"].Cast<CheckBox>().CurrentValue) Peel();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Flee) Flee();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) LaneClear();
            Killsteal();
        }

        private static void Flee()
        {
            Orbwalker.MoveTo(Game.CursorPos);
            if (TickManager.NoLag(2) && W.IsReady())
                W.Cast(Player);
        }

        private static void Harass()
        {
            var target = TargetManager.Target(Q, DamageType.Magical);
            var qpredvalue = Q.GetPrediction(target).HitChance >= PredQ();
            if (!Q.IsReady() || !TickManager.NoLag(1) || !SpellMenu["qth"].Cast<CheckBox>().CurrentValue ||
                target == null || !target.IsValidTarget(Q.Range) || qpredvalue)
                return;
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Optimized(Q, DamageType.Magical, (int) Q.Range, 1, PredQ(), target);
            }
        }

        private static void Killsteal()
        {
            if (!SpellMenu["ksq"].Cast<CheckBox>().CurrentValue || !Q.IsReady()) return;
            var enemy =
                EntityManager.Heroes.Enemies.Find(
                    e =>
                        e.IsValidTarget(Q.Range) &&
                        Player.GetSpellDamage(e, SpellSlot.Q) >= Prediction.Health.GetPrediction(e, Q.CastDelay));
            if (enemy != null)
                CastManager.Cast.Circle.Optimized(Q, DamageType.Magical, (int) Q.Range, 1, PredQ(), enemy);
        }

        private static void AutoR()
        {
            if (EntityManager.Heroes.Enemies.Count(e => e.Distance(Player) < 1250) <
                SpellMenu["auamount"].Cast<Slider>().CurrentValue || !TickManager.NoLag(4) ||
                !R.IsReady())
                return;
            var champs =
                EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < R.Range)
                    .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                    .ToList();
            var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 1250);
            if (location.ChampsHit < SpellMenu["auamount"].Cast<Slider>().CurrentValue) return;
            R.CastDelay = 1900 + 1500*(int) Player.Distance(location.Position)/5300;
            R.Cast(location.Position.To3D());
        }

        public static void LaneClear()
        {
            if (!SpellMenu["qtl"].Cast<CheckBox>().CurrentValue) return;
            CastManager.Cast.Circle.Farm(Q, 3);
        }

        private static void Peel()
        {
            if (!SpellMenu["peel"].Cast<CheckBox>().CurrentValue) return;
            foreach (var pos in from enemy in ObjectManager.Get<Obj_AI_Base>()
                where
                    enemy.IsValidTarget() &&
                    enemy.Distance(Player) <=
                    enemy.BoundingRadius + enemy.AttackRange + Player.BoundingRadius &&
                    enemy.IsMelee
                let direction =
                    (enemy.ServerPosition.To2D() - Player.ServerPosition.To2D()).Normalized()
                let pos = Player.ServerPosition.To2D()
                select pos + Math.Min(200, Math.Max(50, enemy.Distance(Player)/2))*direction)
            {
                W.Cast(pos.To3D());
            }
            if (Player.HealthPercent <= 13)
                foreach (var pos in from enemy in ObjectManager.Get<Obj_AI_Base>()
                    where
                        enemy.IsValidTarget() &&
                        enemy.Distance(Player) <=
                        enemy.BoundingRadius + enemy.AttackRange + Player.BoundingRadius
                    let direction =
                        (enemy.ServerPosition.To2D() - Player.ServerPosition.To2D()).Normalized()
                    let pos = Player.ServerPosition.To2D()
                    select pos + Math.Min(200, Math.Max(50, enemy.Distance(Player)/2))*direction)
                {
                    W.Cast(pos.To3D());
                }
        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (Q.IsReady() && TickManager.NoLag(1) && target != null)
            {
                PredictionResult prediction;

                if (Q.IsInRange(target))
                {
                    prediction = Q.GetPrediction(target);
                    Q.Cast(prediction.CastPosition);
                }
                else if (Q2.IsInRange(target))
                {
                    prediction = Q2.GetPrediction(target);
                    Q2.Cast(prediction.CastPosition);
                }
                else if (Q3.IsInRange(target))
                {
                    prediction = Q3.GetPrediction(target);
                    Q3.Cast(prediction.CastPosition);
                }
                else
                {
                    return;
                }
                if (prediction.HitChance < HitChance.High) return;
                if (Player.ServerPosition.Distance(prediction.CastPosition) <= Q.Range + Q.Width)
                {
                    Vector3 p;
                    if (Player.ServerPosition.Distance(prediction.CastPosition) > 300)
                    {
                        p = prediction.CastPosition -
                            100*
                            (prediction.CastPosition.To2D() - Player.ServerPosition.To2D()).Normalized()
                                .To3D();
                    }
                    else
                    {
                        p = prediction.CastPosition;
                    }

                    Q.Cast(p);
                }
                else if (Player.ServerPosition.Distance(prediction.CastPosition) <=
                         (Q.Range + Q2.Range)/2.0)
                {
                    var p = Player.ServerPosition.To2D()
                        .Extend(prediction.CastPosition.To2D(), Q.Range - 100);

                    if (!CheckQCollision(target, prediction.UnitPosition, p.To3D()))
                    {
                        Q.Cast(p.To3D());
                    }
                }
                else
                {
                    var p = Player.ServerPosition.To2D() +
                            Q.Range*
                            (prediction.CastPosition.To2D() - Player.ServerPosition.To2D()).Normalized
                                ();

                    if (!CheckQCollision(target, prediction.UnitPosition, p.To3D()))
                    {
                        Q.Cast(p.To3D());
                    }
                }
            }
        }

        private static bool CheckQCollision(Obj_AI_Base target, Vector3 targetPosition, Vector3 castPosition)
        {
            var direction = (castPosition.To2D() - Player.ServerPosition.To2D()).Normalized();
            var firstBouncePosition = castPosition.To2D();
            var secondBouncePosition = firstBouncePosition +
                                       direction*0.4f*
                                       Player.ServerPosition.To2D().Distance(firstBouncePosition);
            var thirdBouncePosition = secondBouncePosition +
                                      direction*0.6f*firstBouncePosition.Distance(secondBouncePosition);

            if (thirdBouncePosition.Distance(targetPosition.To2D()) < Q.Width + target.BoundingRadius)
            {
                if ((from minion in ObjectManager.Get<Obj_AI_Minion>()
                    where minion.IsValidTarget(3000)
                    let predictedPos = Q2.GetPrediction(minion)
                    where predictedPos.UnitPosition.To2D().Distance(secondBouncePosition) <
                          Q2.Width + minion.BoundingRadius
                    select minion).Any())
                {
                    return true;
                }
            }

            if (secondBouncePosition.Distance(targetPosition.To2D()) < Q.Width + target.BoundingRadius ||
                thirdBouncePosition.Distance(targetPosition.To2D()) < Q.Width + target.BoundingRadius)
            {
                return (from minion in ObjectManager.Get<Obj_AI_Minion>()
                    where minion.IsValidTarget(3000)
                    let predictedPos = Q.GetPrediction(minion)
                    where
                        predictedPos.UnitPosition.To2D().Distance(firstBouncePosition) < Q.Width + minion.BoundingRadius
                    select minion).Any();
            }

            return true;
        }

        private static void Combo()
        {
            var target = TargetManager.Target(1200, DamageType.Magical);
            if (target == null) return;
            if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue
                && Q.IsReady())
            {
                CastQ(target);
            }
            if (SpellMenu["etc"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Optimized(E, DamageType.Magical, (int) E.Range, 1, PredE());
            }
            if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Optimized(W, DamageType.Magical, (int) W.Range, 1, PredW());
            }
            if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e =>
                            e.IsValidTarget(R.Range) &&
                            Player.GetSpellDamage(e, SpellSlot.R) >= Prediction.Health.GetPrediction(e, R.CastDelay));
                if (enemy != null)
                {
                    R.CastDelay = 1900 + 1500*(int) Player.Distance(target)/5300;
                    CastManager.Cast.Circle.Optimized(R, DamageType.Magical, (int) R.Range, 1, HitChance.High, enemy);
                }
            }
        }

        private static HitChance PredQ()
        {
            var mode = SpellMenu["hQ"].DisplayName;
            switch (mode)
            {
                case "Low (Fast Casting)":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High (Slow Casting)":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }

        private static HitChance PredE()
        {
            var mode = SpellMenu["hE"].DisplayName;
            switch (mode)
            {
                case "Low (Fast Casting)":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High (Slow Casting)":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }

        private static HitChance PredW()
        {
            var mode = SpellMenu["hW"].DisplayName;
            switch (mode)
            {
                case "Low (Fast Casting)":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High (Slow Casting)":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }
    }

    public enum AttackSpell
    {
        Q
    };
}