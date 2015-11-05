using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;
using SharpDX;
using SharpDX.Multimedia;

namespace VolatileAIO.Organs.Brain
{
    internal class CastManager : Heart
    {
        private static bool _isAutoAttacking;
        public static int HitCount;
        public static int CastCount;

        internal struct LastSpells
        {
            public string name;
            public int tick;
            public float dmg;
            public string target;

            public LastSpells(string n, int t, float d, string tg)
            {
                name = n;
                tick = t;
                dmg = d;
                target = tg;
            }
        }

        public static List<LastSpells> _lastSpells = new List<LastSpells>();

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

                    if (!spell.IsReady() || _isAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!target.IsValidTarget(spell.Range) || spell.GetPrediction(target).HitChance < hitChance)
                        return;

                    spell.Cast(spell.GetPrediction(target).CastPosition);

                    lock (_lastSpells)
                    {
                        _lastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);

                        if (_lastSpells.Exists(p => p.name == spell.Name) || spell.Slot != SpellSlot.Q)
                            return;

                        _lastSpells.Add(new LastSpells(spell.Name, Environment.TickCount,
                            Player.GetSpellDamage(target, spell.Slot), target.Name));

                        CastCount++;
                    }
                }

                internal static void Farm(Spell.Skillshot spell, int minHit = 1)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                        return;
                    if (!spell.IsReady() || _isAutoAttacking) return;

                    var minions =
                        MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius)
                            .Select(
                                minion =>
                                    Prediction.Position.PredictUnitPosition(minion,
                                        (int)(spell.CastDelay + (Player.Distance(minion) / spell.Speed))))
                            .ToList();

                    if (MinionManager.GetBestLineFarmLocation(minions, spell.Width, spell.Range).MinionsHit>=minHit)
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
                    if (!spell.IsReady() || _isAutoAttacking) return;

                    var minions =
                        MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius)
                            .Select(
                                minion =>
                                    Prediction.Position.PredictUnitPosition(minion,
                                        (int) (spell.CastDelay + (Player.Distance(minion)/spell.Speed))))
                            .ToList();

                    if (MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).MinionsHit >=
                        minHit)
                        spell.Cast(
                            MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).Position.To3D());
                }

                internal static void WujuStyle(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, int minHit = 1, HitChance hitChance = HitChance.Medium, AIHeroClient targetHero = null)
                {
                    //Credits to WujuSan for the original algorithm, now optimized by turkey for better hitchance
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                        return;

                    if (!spell.IsReady() || _isAutoAttacking) return;

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
                    var posAndHits = CircleSpellPos(spell.GetPrediction(target).CastPosition.To2D(), spell);

                    if (posAndHits.First().Value >= minHit)
                        spell.Cast(posAndHits.First().Key.To3D());
                }
            }
        }

        protected override void Volatile_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!sender.IsMe) return;
            _lastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);

            if (args.Source.NetworkId != Player.NetworkId ||
                !EntityManager.Heroes.Enemies.Exists(p => p.NetworkId == args.Target.NetworkId)) return;

            if (_lastSpells.Count == 0) return;

            var sremove = new LastSpells("", 0, 0, "");
            foreach (var spell in _lastSpells)
            {

                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                    Chat.Print(spell.name +
                               " & args dmg: " + args.Damage + " & preddmg: " + spell.dmg);

                if (spell.target != args.Target.Name) continue;
                HitCount++;
                sremove = spell;
            }
            if (_lastSpells.Contains(sremove))
                _lastSpells.Remove(sremove);
        }


        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            _isAutoAttacking = false;
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            _isAutoAttacking = true;
        }
       
        private static int CountSpellHits(Vector2 castPosition, Spell.Skillshot spell)
        {
            return GetEnemiesPosition(spell).Count(enemyPos => castPosition.Distance(enemyPos) <= spell.Radius);
        }

        private static Dictionary<Vector2, int> CircleSpellPos(Vector2 targetPosition, Spell.Skillshot spell)
        {
            var spellPos = new List<Vector2>
            {
                new Vector2(targetPosition.X + (spell.Radius/(float) 1.25), targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X + (spell.Radius/(float) 1.25), targetPosition.Y),
                new Vector2(targetPosition.X + (spell.Radius/3)*2, targetPosition.Y - spell.Radius),
                new Vector2(targetPosition.X + (spell.Radius/3)*2, targetPosition.Y - (spell.Radius/3)*2),
                new Vector2(targetPosition.X + (spell.Radius/3)*2, targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X + (spell.Radius/3)*2, targetPosition.Y + (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X + (spell.Radius/3)*2, targetPosition.Y),
                new Vector2(targetPosition.X + (spell.Radius/(float) 2), targetPosition.Y + (spell.Radius/(float) 2)),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y - spell.Radius),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/3)*2),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/(float) 1.25)),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y + (spell.Radius/3)*2),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y + (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X + (spell.Radius/(float) 3), targetPosition.Y),
                new Vector2(targetPosition.X, targetPosition.Y - spell.Radius),
                new Vector2(targetPosition.X, targetPosition.Y - (spell.Radius/3)*2),
                new Vector2(targetPosition.X, targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X, targetPosition.Y),
                new Vector2(targetPosition.X, targetPosition.Y + (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X, targetPosition.Y + (spell.Radius/3)*2),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y + (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y + (spell.Radius/3)*2),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/3)*2),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y - (spell.Radius/(float) 1.25)),
                new Vector2(targetPosition.X - (spell.Radius/(float) 3), targetPosition.Y - spell.Radius),
                new Vector2(targetPosition.X - (spell.Radius/(float) 2), targetPosition.Y + (spell.Radius/(float) 2)),
                new Vector2(targetPosition.X - (spell.Radius/3)*2, targetPosition.Y),
                new Vector2(targetPosition.X - (spell.Radius/3)*2, targetPosition.Y + (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X - (spell.Radius/3)*2, targetPosition.Y - (spell.Radius/(float) 3)),
                new Vector2(targetPosition.X - (spell.Radius/3)*2, targetPosition.Y - (spell.Radius/3)*2),
                new Vector2(targetPosition.X - (spell.Radius/3)*2, targetPosition.Y - spell.Radius),
                new Vector2(targetPosition.X - (spell.Radius/(float) 1.25), targetPosition.Y),
                new Vector2(targetPosition.X - (spell.Radius/(float) 1.25), targetPosition.Y - (spell.Radius/(float) 3)),
            };

            var posAndHits = spellPos.ToDictionary(pos => pos, pos => CountSpellHits(pos, spell));

            var posToGg = posAndHits.First(pos => pos.Value == posAndHits.Values.Max()).Key;
            var hits = posAndHits.First(pos => pos.Key == posToGg).Value;
            return hits <= 1
                ? new Dictionary<Vector2, int> {{targetPosition, hits}}
                : new Dictionary<Vector2, int> {{posToGg, hits}};
        }

        private static IEnumerable<Vector2> GetEnemiesPosition(Spell.Skillshot spell)
        {
            return
                EntityManager.Heroes.Enemies.Where(
                    hero => !hero.IsDead && Player.Distance(hero) <= spell.Range + spell.Radius)
                    .Select(hero => spell.GetPrediction(hero).CastPosition.To2D())
                    .ToList();
        }
    }
}