using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;
using SharpDX;
using SharpDX.Multimedia;
using EloBuddy.SDK.Enumerations;
using HitChance = EloBuddy.SDK.Enumerations.HitChance;
using Prediction = EloBuddy.SDK.Prediction;

namespace VolatileAIO.Organs.Brain
{
    internal class CastManager : Heart
    {
        internal static bool IsAutoAttacking;

        internal struct OptimizedLocation
        {
            public int ChampsHit;
            public Vector2 Position;

            public OptimizedLocation(Vector2 position, int champsHit)
            {
                Position = position;
                ChampsHit = champsHit;
            }
        }

        internal class Cast
        {
            internal class Line
            {
                internal static void SingleTargetHero(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, HitChance hitChance = HitChance.Medium, AIHeroClient targetHero = null)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4))) return;

                    if (!spell.IsReady() || IsAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!VolatileMenu["vpred"].Cast<CheckBox>().CurrentValue)
                    {
                        if (!target.IsValidTarget(spell.Range) || spell.GetPrediction(target).HitChance < hitChance)
                            return;

                        spell.Cast(spell.GetPrediction(target).CastPosition);
                    }
                    /*else
                    {
                        var CoreType2 = SkillshotType.SkillshotLine;
                        bool aoe2 = false;
                        if ((int) spell.Type == (int) SkillshotType.SkillshotCircle)
                        {
                            CoreType2 = SkillshotType.SkillshotCircle;
                            aoe2 = true;
                        }
                        if (spell.Width > 80 && spell.AllowedCollisionCount < 100)
                            aoe2 = true;
                        var predInput2 = new PredictionInput
                        {
                            Aoe = aoe2,
                            Collision = spell.AllowedCollisionCount < 100,
                            Speed = spell.Speed,
                            Delay = spell.CastDelay,
                            Range = spell.Range,
                            From = Player.ServerPosition,
                            Radius = spell.Radius,
                            Unit = target,
                            Type = CoreType2
                        };
                        var poutput2 = Test.TopSecret.Prediction.GetPrediction(predInput2);
                        //var poutput2 = spell.GetPrediction(target);
                        Chat.Print(spell.Slot+" "+predInput2.Collision+poutput2.Hitchance);
                        if (spell.Speed != float.MaxValue && CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                            return;

                        if (VolatileMenu["vpred2"].Cast<Slider>().CurrentValue == 0)
                        {
                            if (poutput2.Hitchance >= Test.TopSecret.HitChance.VeryHigh)
                                spell.Cast(poutput2.CastPosition);
                            else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 &&
                                     poutput2.Hitchance >= Test.TopSecret.HitChance.High)
                            {
                                spell.Cast(poutput2.CastPosition);
                            }

                        }
                        else if (VolatileMenu["vpred2"].Cast<Slider>().CurrentValue == 1)
                        {
                            if (poutput2.Hitchance >= Test.TopSecret.HitChance.High)
                                spell.Cast(poutput2.CastPosition);

                        }
                        else if (VolatileMenu["vpred2"].Cast<Slider>().CurrentValue == 2)
                        {
                            if (poutput2.Hitchance >= Test.TopSecret.HitChance.Medium)
                                spell.Cast(poutput2.CastPosition);
                        }
                    }*/
                }


            internal static void Farm(Spell.Skillshot spell, int minHit = 1)
            {
                if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                    (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                    (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                    (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                    return;
                if (!spell.IsReady() || IsAutoAttacking) return;

                var minions =
                    MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius).Select(
                            minion =>
                                Prediction.Position.PredictUnitPosition(minion,
                                    (int)(spell.CastDelay + (Player.Distance(minion) / spell.Speed))))
                        .ToList();

                if (MinionManager.GetBestLineFarmLocation(minions, spell.Width, spell.Range).MinionsHit >= minHit)
                    spell.Cast(
                        MinionManager.GetBestLineFarmLocation(minions, spell.Width, spell.Range).Position.To3D());
            }
        }

        internal class Circle
            {
                internal static void Farm(Spell.Skillshot spell, int minHit = 1)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4))) return;
                    if (!spell.IsReady() || IsAutoAttacking) return;

                    var minions =
                        MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius)
                            .Select(
                                minion =>
                                    Prediction.Position.PredictUnitPosition(minion,
                                        (int) (spell.CastDelay + (Player.Distance(minion)/spell.Speed))))
                            .ToList();
                    
                    if (MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).MinionsHit <
                        minHit) return;
                        spell.Cast(
                            MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).Position.To3D());
                }

                internal static void Optimized(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, int minHit = 1, HitChance hitChance = HitChance.Medium,
                    AIHeroClient targetHero = null)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                        return;

                    if (!spell.IsReady() || IsAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!target.IsValidTarget(spell.Range + spell.Radius) ||
                        spell.GetPrediction(target).HitChance < hitChance)
                        return;

                    var champs = EntityManager.Heroes.Enemies.Where(e => e.Distance(target) < spell.Radius).Select(champ => Prediction.Position.PredictUnitPosition(champ, ((int)Player.Distance(champ) / spell.Speed) + spell.CastDelay)).ToList();

                    var posAndHits = GetOptimizedCircleLocation(champs, spell.Width, spell.Range);

                    if (posAndHits.ChampsHit >= minHit)
                        spell.Cast(posAndHits.Position.To3DWorld());
                }
            }
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAutoAttacking = false;
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsAutoAttacking = true;
        }

        public static OptimizedLocation GetOptimizedCircleLocation(List<Vector2> champPositions,
            float width,
            float range,
            // ReSharper disable once InconsistentNaming
            int useMECMax = 9)
        {
            var result = new Vector2();
            var champsHit = 0;
            var startPos = ObjectManager.Player.ServerPosition.To2D();

            range = range*range;

            if (champPositions.Count == 0)
            {
                return new CastManager.OptimizedLocation(result, champsHit);
            }
            
            if (champPositions.Count <= useMECMax)
            {
                var subGroups = GetCombinations(champPositions);
                foreach (var subGroup in subGroups)
                {
                    if (subGroup.Count > 0)
                    {
                        var circle = MEC.GetMec(subGroup);

                        if (circle.Radius <= width && circle.Center.Distance(startPos, true) <= range)
                        {
                            champsHit = subGroup.Count;
                            return new CastManager.OptimizedLocation(circle.Center, champsHit);
                        }
                    }
                }
            }
            else
            {
                foreach (var pos in champPositions)
                {
                    if (pos.Distance(startPos, true) <= range)
                    {
                        var count = champPositions.Count(pos2 => pos.Distance(pos2, true) <= width*width);

                        if (count >= champsHit)
                        {
                            result = pos;
                            champsHit = count;
                        }
                    }
                }
            }

            return new OptimizedLocation(result, champsHit);
        }

        private static List<List<Vector2>> GetCombinations(List<Vector2> allValues)
        {
            var collection = new List<List<Vector2>>();
            for (var counter = 0; counter < (1 << allValues.Count); ++counter)
            {
                var combination = allValues.Where((t, i) => (counter & (1 << i)) == 0).ToList();

                collection.Add(combination);
            }
            return collection;
        }
    }
}