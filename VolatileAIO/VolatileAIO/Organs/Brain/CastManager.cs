using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;

namespace VolatileAIO.Organs.Brain
{
    class CastManager : Heart
    {
        private static bool _isAutoAttacking = false;
        public static int hitCount = 0;
        public static int castCount = 0;

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

        internal class DifferencePChamp
        {
            public string Name;
            public List<float> Differences;

            public DifferencePChamp(string n)
            {
                Name = n;
                Differences = new List<float>();
            }
        }

        public static List<DifferencePChamp> Champions = new List<DifferencePChamp>(); 

        public static List<LastSpells> _lastSpells = new List<LastSpells>();

        internal class Cast
        {
            internal class Line
            {
                internal static void SingleTarget(Spell.Skillshot spell, DamageType damageType,
                    int range=0, HitChance hitChance = HitChance.Medium, Obj_AI_Base targetHero = null)
                {
                    if ((spell.Slot == SpellSlot.Q && TickManager.NoLag(1)) ||
                        (spell.Slot == SpellSlot.W && TickManager.NoLag(2)) ||
                        (spell.Slot == SpellSlot.E && TickManager.NoLag(3)) ||
                        (spell.Slot == SpellSlot.R && TickManager.NoLag(4)))
                    {
                        if (spell.IsReady() && !_isAutoAttacking)
                        {
                            var target = range!=0 ? TargetManager.Target(range, damageType) : TargetManager.Target(spell, damageType);

                            if (target == null) return;
                            if (target.IsValidTarget(spell.Range) && spell.GetPrediction(target).HitChance >= hitChance)
                            {
                                spell.Cast(spell.GetPrediction(target).CastPosition);

                                lock (_lastSpells)
                                {
                                    _lastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);
                                    if (!_lastSpells.Exists(p => p.name == spell.Name) && spell.Slot == SpellSlot.Q)
                                    {
                                        if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                                            Chat.Print("Cast Q: dmgPredict=" + Player.GetSpellDamage(target, spell.Slot) +
                                                       " & target=" +
                                                       target.Name);
                                        _lastSpells.Add(new LastSpells(spell.Name, Environment.TickCount,
                                            Player.GetSpellDamage(target, spell.Slot), target.Name));
                                        castCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Volatile_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!sender.IsMe) return;
            _lastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);

            if (args.Source.NetworkId == Player.NetworkId &&
                EntityManager.Heroes.Enemies.Exists(p => p.NetworkId == args.Target.NetworkId))
            {
                if (_lastSpells.Count == 0) return;
                var sremove = new LastSpells("", 0, 0, "");
                foreach (var spell in _lastSpells)
                {

                    if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                        Chat.Print(spell.name +
                                   " & args dmg: " + args.Damage + " & preddmg: " + spell.dmg);

                    if (spell.target == args.Target.Name)
                    {
                        if (Champions.Exists(p => p.Name == spell.target))
                        {
                            Champions.Find(p => p.Name == spell.target)
                                .Differences.Add(Math.Abs(spell.dmg - args.Damage));
                            hitCount++;
                            sremove = spell;
                        }
                    }
                }
                _lastSpells.Remove(sremove);
            }
        }


        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            _isAutoAttacking = false;
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            _isAutoAttacking = true;
        }

    }
}