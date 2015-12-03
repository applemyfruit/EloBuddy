using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using VolatileAIO.Organs.Brain.Cars;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Organs.Brain.Utils
{
    class HealthDeath
    {

        private static int _lastTick;

        public static readonly Dictionary<int, DamageMaker> ActiveDamageMakers = new Dictionary<int, DamageMaker>();

        public static Dictionary<int, Damager> DamagerSources = new Dictionary<int, Damager>();

        public static readonly Dictionary<int, DamageMaker> ActiveTowerTargets = new Dictionary<int, DamageMaker>();

        public static List<Obj_AI_Base> MinionsAround = new List<Obj_AI_Base>();

        private const int TowerDamageDelay = 250;

        public static int Now
        {
            get { return (int)DateTime.Now.TimeOfDay.TotalMilliseconds; }
        }

        static HealthDeath()
        {
            Game.OnUpdate += OnUpdate;

            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;

            Obj_AI_Base.OnSpellCast += OnDoCast;

            Obj_AI_Base.OnProcessSpellCast += OnMeleeStartAutoAttack;
            Spellbook.OnStopCast += OnMeleeStopAutoAttack;
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

        }

        private static void OnMeleeStartAutoAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {


            if (sender is Obj_AI_Turret)
            {
                ActiveTowerTargets.Remove(sender.NetworkId);
                var dMake = new DamageMaker(sender,
                    (Obj_AI_Base)args.Target,
                    null,
                    args.SData,
                    true);

                ActiveTowerTargets.Add(sender.NetworkId, dMake);


            }

            if (!args.SData.IsAutoAttack())
                return;
            if (args.Target != null && args.Target is Obj_AI_Base && !sender.IsMe)
            {

                var tar = (Obj_AI_Base)args.Target;
                if (DamagerSources.ContainsKey(sender.NetworkId))
                {
                    DamagerSources[sender.NetworkId].SetTarget(tar);
                }
                else
                {
                    DamagerSources.Add(sender.NetworkId, new Damager(sender, tar));
                }
            }


            if (!sender.IsMelee)
                return;

            if (args.Target is Obj_AI_Base)
            {
                ActiveDamageMakers.Remove(sender.NetworkId);
                var dMake = new DamageMaker(sender,
                    (Obj_AI_Base)args.Target,
                    null,
                    args.SData,
                    true);

                ActiveDamageMakers.Add(sender.NetworkId, dMake);
            }

            if (sender is AIHeroClient)
            {
                Volkswagen.LastDmg = Now;
            }
        }

        private static void OnMeleeStopAutoAttack(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            //if (!sender.Owner.IsMelee())
            //    return;

            if (ActiveDamageMakers.ContainsKey(sender.NetworkId))
                ActiveDamageMakers.Remove(sender.NetworkId);

            //Ranged aswell
            if (args.DestroyMissile && ActiveDamageMakers.ContainsKey((int) args.MissileNetworkId))
                ActiveDamageMakers.Remove((int) args.MissileNetworkId);
            if (DamagerSources.ContainsKey(sender.NetworkId))
                DamagerSources[sender.NetworkId].SetTarget(null);
        }

        private static void OnUpdate(EventArgs args)
        {
            //Some failsafe l8er if needed

            //Hope it wont lag :S
            MinionsAround = MinionManager.GetMinions(1700, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.None);

            foreach (var minion in MinionsAround)
            {
                if (!DamagerSources.ContainsKey(minion.NetworkId))
                    DamagerSources.Add(minion.NetworkId, new Damager(minion, null));
            }

            DamagerSources.ToList()
                .Where(pair => !pair.Value.IsValidDamager())
                .ToList()
                .ForEach(pair => DamagerSources.Remove(pair.Key));

            if (Now - _lastTick <= 60 * 1000)
            {
                return;
            }


            ActiveDamageMakers.ToList()
                .Where(pair => pair.Value.CreatedTick < Now - 60000)
                .ToList()
                .ForEach(pair => ActiveDamageMakers.Remove(pair.Key));




            _lastTick = Now;
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            //most likely AA
            if (sender is MissileClient)
            {
                var mis = (MissileClient)sender;
                if (mis.Target is Obj_AI_Base)
                {
                    var dMake = new DamageMaker(mis.SpellCaster,
                        (Obj_AI_Base)mis.Target,
                        mis,
                        mis.SData);

                    ActiveDamageMakers.Add(mis.NetworkId, dMake);
                }
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            DamagerSources.Remove(sender.NetworkId);

            if (sender is MissileClient || sender is Obj_SpellMissile)
            {
                if (ActiveDamageMakers.ContainsKey(sender.NetworkId))
                    ActiveDamageMakers.Remove(sender.NetworkId);
            }

            if (sender is Obj_AI_Base)
            {
                int i = 0;
                foreach (var dmgMk in ActiveDamageMakers)
                {
                    if (dmgMk.Value.Source == null || dmgMk.Value.Missle == null)
                        continue;
                    if (dmgMk.Value.Source.NetworkId == sender.NetworkId)
                    {
                        ActiveDamageMakers.Remove(dmgMk.Value.Missle.NetworkId);
                        return;
                    }
                    i++;
                }
            }
        }
        //Maybe later change so would return data about missile
        public static DamageMaker AttackedByTurret(AttackableUnit unit)
        {
            return ActiveTowerTargets.Values.Where(v => v.Target.NetworkId == unit.NetworkId).FirstOrDefault(attack => attack.Source is Obj_AI_Turret);
        }

        //Only active attacks
        public static int GetTimeTillDeath(AttackableUnit unit, bool ignoreAlmostDead = true)
        {
            int hp = (int)unit.Health;
            foreach (var attacks in ActiveDamageMakers.Values.OrderBy(atk => atk.HitOn))
            {
                if (attacks.Target == null || attacks.Target.NetworkId != unit.NetworkId || (ignoreAlmostDead && AlmostDead(attacks.Source)))
                    continue;
                int hitOn = attacks.HitOn;
                if (hitOn > Now)
                {
                    hp -= (int)attacks.DealDamage;
                    if (hp <= 0)
                        return hitOn - Now;
                }
            }
            return int.MaxValue;
        }

        public static bool AlmostDead(AttackableUnit unit)
        {
            if (unit == null)
                return true;
            try
            {

                var hitingUnitDamage = MisslesHeadedOnDamage(unit);
                // if (unit.Health < hitingUnitDamage * 0.65)
                //    Console.WriteLine("Ignore cus almost dead!");

                return unit.Health < hitingUnitDamage * 0.65;
            }
            catch (Exception)
            {
                return true;
            }

        }

        public static float GetLastHitPred(AttackableUnit unit, int msTime, bool ignoreAlmostDead = true)
        {
            var predDmg = 0f;
            var predDmgPlus500Ms = 0f;

            foreach (var attacks in ActiveDamageMakers.Values)
            {
                if (attacks.Target == null || attacks.Target.NetworkId != unit.NetworkId || (ignoreAlmostDead && AlmostDead(attacks.Source)))
                    continue;
                int hitOn = 0;
                if (attacks.Missle == null || attacks.SData.MissileSpeed == 0)
                {
                    hitOn = (int)(attacks.CreatedTick + attacks.Source.AttackCastDelay * 1000);
                }
                else
                {
                    hitOn = Now + (int)((attacks.Missle.Position.Distance(unit.Position) * 1000) / attacks.SData.MissileSpeed) + 100;
                }

                if (Now < hitOn && hitOn < Now + msTime)
                {
                    predDmg += attacks.DealDamage;
                }
            }
            return unit.Health - predDmg;
        }

        public static float GetLastHitPredPeriodic(AttackableUnit unit, int msTime, bool ignoreAlmostDead = true)
        {
            var predDmg = 0f;

            msTime = (msTime > 10000) ? 10000 : msTime;

            foreach (var attacks in ActiveDamageMakers.Values)
            {
                if (attacks.Target == null || attacks.Target.NetworkId != unit.NetworkId || (ignoreAlmostDead && AlmostDead(attacks.Source)) || attacks.Source.IsMe)
                    continue;
                int hitOn = 0;
                if (attacks.Missle == null || attacks.SData.MissileSpeed == 0)
                {
                    hitOn = (int)(attacks.CreatedTick + attacks.Source.AttackCastDelay * 1000);
                }
                else
                {
                    hitOn = Now + (int)((attacks.Missle.Position.Distance(unit.Position) * 1000) / attacks.SData.MissileSpeed);
                }

                int timeTo = Now + msTime;

                int hits = (attacks.Cycle == 0) ? 0 : (int)((timeTo - hitOn) / attacks.Cycle) + 1;

                if (Now < hitOn && hitOn <= Now + msTime)
                {
                    predDmg += attacks.DealDamage * hits;
                }
            }
            return unit.Health - predDmg;
        }

        public static float GetLaneClearPred(AttackableUnit unit, int msTime, bool ignoreAlmostDead = true)
        {
            float predictedDamage = 0;
            var damageDoneTill = Now + msTime;
            foreach (var damager in DamagerSources.Values)
            {
                if (!damager.IsValidDamager() || !(unit is Obj_AI_Base))
                    continue;
                var target = damager.GetTarget();
                if (target == null || target.NetworkId != unit.NetworkId || (ignoreAlmostDead && AlmostDead(damager.Source)))
                    continue;
                if (damager.FirstHitAt > damageDoneTill)
                    continue;
                damager.FirstHitAt = (damager.FirstHitAt < Now) ? Now + damager.Cycle : damager.FirstHitAt;
                predictedDamage += damager.Damage;
                //Console.WriteLine(damager.damage);
                //Can be optimized??
                var nextAa = damager.FirstHitAt + damager.Cycle;
                while (damageDoneTill > nextAa)
                {
                    predictedDamage += damager.Damage;
                    nextAa += damager.Cycle;
                }
            }
            //if (predictedDamage > 0)
            // Console.WriteLine("dmg: " + predictedDamage);
            return unit.Health - predictedDamage;
        }

        public static int MisslesHeadedOn(AttackableUnit unit)
        {
            return ActiveDamageMakers.Count(un => un.Value.Target.NetworkId == unit.NetworkId);
        }

        public static float MisslesHeadedOnDamage(AttackableUnit unit)
        {
            return ActiveDamageMakers.Where(un => un.Value.Target.NetworkId == unit.NetworkId).Sum(un => un.Value.DealDamage);
        }
        //Used for laneclear
        public class Damager
        {
            public Obj_AI_Base Source;

            private Obj_AI_Base _target;

            public int CreatedTick;

            public int Cycle;

            public int FirstHitAt;

            public int LastAaTry;

            public float Damage;

            public Damager(Obj_AI_Base s, Obj_AI_Base t)
            {
                Source = s;
                _target = t;
                CreatedTick = Now;
                Cycle = (int)(Source.AttackDelay * 1000);
                FirstHitAt = HitOn;
                Damage = GetDamage();
                LastAaTry = Now;
            }

            public bool IsValidDamager()
            {
                return Source != null && Source.IsValid && !Source.IsDead;
            }

            public bool IsValidTarget()
            {
                return _target != null && _target.IsValid && !_target.IsDead;
            }

            public void SetTarget(Obj_AI_Base tar)
            {
                if (tar == null)
                {
                    _target = null;
                    return;
                }
                if (_target != null && _target.NetworkId == tar.NetworkId)
                    return;
                _target = tar;
                CreatedTick = Now;
                FirstHitAt = HitOn;
                Damage = GetDamage();

            }

            public Obj_AI_Base GetTarget()
            {
                if (IsValidTarget())
                    return _target;
                var predTarget = MinionsAround.Where(min => !min.IsDead && min.Distance(Source, true) < 650 * 650)
                        .OrderBy(min => min.Distance(Source.Position, true))
                        .FirstOrDefault();
                SetTarget(predTarget);
                return predTarget;
            }

            private float GetDamage()
            {
                var tar = GetTarget();
                if (tar == null || Source == null)
                    return 0;
                // Console.WriteLine("Return damge");
                return (float)Source.GetAutoAttackDamage(tar, true);
            }

            private int HitOnTar(Obj_AI_Base tar)
            {
                if (tar == null)
                    return int.MaxValue;
                int addTime = 0;
                if (Volkswagen.inAutoAttackRange(Source, tar))//+ check if want to move to killabel minion and range it wants to
                {
                    var realDist = Volkswagen.realDistanceTill(Source, _target);
                    var aaRange = Volkswagen.getRealAutoAttackRange(Source, tar);

                    addTime += (int)(((realDist - aaRange) * 1000) / Source.MoveSpeed);
                }

                if (Source.IsMelee || Volkswagen.Azir)
                {
                    return (int)(CreatedTick + Source.AttackCastDelay * 1000) + addTime;
                }
                else
                {
                    return CreatedTick + (int)((Source.Position.Distance(tar.Position) * 1000) / (Source.BasicAttack.MissileSpeed)) + ((Source is Obj_AI_Turret) ? TowerDamageDelay : 0) + addTime;//lil delay cus dunno l8er could try to values what says delay of dmg dealing
                }
            }

            private int HitOn
            {
                get
                {
                    try
                    {
                        if (Source == null || !Source.IsValid)
                            return int.MaxValue;
                        var tar = GetTarget();
                        return HitOnTar(tar);

                    }
                    catch (Exception)
                    {
                        return int.MaxValue;
                    }
                }
            }

        }

        public class DamageMaker
        {
            public readonly GameObject Missle;

            public readonly Obj_AI_Base Source;

            public readonly Obj_AI_Base Target;

            public readonly SpellData SData;

            public readonly float FullDamage;//Unused for now

            public readonly float DealDamage;

            public readonly bool IsAutoAtack;

            public readonly int CreatedTick;

            public readonly bool Melee;

            public readonly int Cycle;

            public int HitOn
            {
                get
                {
                    try
                    {
                        if (Source == null || !Source.IsValid)
                            return int.MaxValue;
                        if (Missle == null || Volkswagen.Azir)
                        {
                            return (int)(CreatedTick + Source.AttackCastDelay * 1000);
                        }
                        else
                        {
                            return Now + (int)((Missle.Position.Distance(Target.Position) * 1000) / (SData.MissileSpeed)) + ((Source is Obj_AI_Turret) ? TowerDamageDelay : 0);//lil delay cus dunno l8er could try to values what says delay of dmg dealing
                        }

                    }
                    catch (Exception)
                    {
                        return int.MaxValue;
                    }
                }
            }

            public DamageMaker(Obj_AI_Base sourceIn, Obj_AI_Base targetIn, GameObject missleIn, SpellData dataIn, bool meleeIn = false)
            {
                Source = sourceIn;
                Target = targetIn;
                Missle = missleIn;
                SData = dataIn;
                Melee = !meleeIn;
                CreatedTick = Now;
                IsAutoAtack = SData.IsAutoAttack();

                if (IsAutoAtack)
                {

                    DealDamage = (float)Source.GetAutoAttackDamage(Target, true);
                    if (Source.IsMelee)
                        Cycle = (int)(Source.AttackDelay * 1000);
                    else
                    {
                        //var dist = source.Distance(target);
                        Cycle = (int)((Source.AttackDelay * 1000)) /*+ (dist*1000)/sData.MissileSpeed)*/;
                        //Console.WriteLine("cycle: " + cycle);
                    }
                    //Console.WriteLine("cycle: " + source.AttackSpeedMod);
                }
                else
                {
                    Cycle = 0;
                    if (Source is AIHeroClient)
                    {
                        var tSpell = TargetSpellDatabase.GetByName(SData.Name);
                        if (tSpell == null)
                        {
                            //Console.WriteLine("Unknown targeted spell: " + sData.Name);
                            DealDamage = 0;
                        }
                        else
                        {
                            try
                            {

                                DealDamage = (float)((AIHeroClient)Source).GetSpellDamage(Target, tSpell.Spellslot);
                            }
                            catch (Exception)
                            {
                                DealDamage = 0;
                            }
                        }
                    }
                    else
                    {
                        DealDamage = 0;
                    }
                }


            }

        }

    }
}