using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using VolatileAIO.Organs.Brain.Utils;
using Color = System.Drawing.Color;
using Dash = EloBuddy.SDK.Events.Dash;
using VPrediction = VolatileAIO.Organs.Brain.Utils.VPrediction;

// ReSharper disable LocalizableElement

namespace VolatileAIO.Organs.Brain.Cars
{
    public class Volkswagen
    {

        public static bool Azir = false;

        //Spells that reset the attack timer.
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianw",
            "lucianq",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack",
            "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq",
            "xenzhaocombotarget", "yorickspectral", "reksaiq"
        };

        //Spells that are not attacks even if they have the "attack" word in their name.
        private static readonly string[] NoAttacks =
        {
            "jarvanivcataclysmattack", "monkeykingdoubleattack",
            "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2",
            "zyragraspingplantattackfire", "zyragraspingplantattack2fire", "viktorpowertransfer"
        };

        //Spells that are attacks even if they dont have the "attack" word in their name.
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute",
            "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2",
            "xenzhaothrust3", "viktorqbuff"
        };

        //cant cancel attacks
        private static readonly string[] AttacksCantCancel =
        {
            "azirbasicattacksoldier",
        };

        public static int Now
        {
            get { return (int) DateTime.Now.TimeOfDay.TotalMilliseconds; }
        }

        public static Menu Menu;

        public enum Mode
        {
            Combo,
            Harass,
            LaneClear,
            LaneFreeze,
            Lasthit,
            Flee,
            None,
        }

        public static Mode CurrentMode
        {
            get
            {
                if (_orbwalkerMenu["com"].Cast<KeyBind>().CurrentValue)
                    return Mode.Combo;
                if (_orbwalkerMenu["mix"].Cast<KeyBind>().CurrentValue)
                    return Mode.Harass;
                if (_orbwalkerMenu["lan"].Cast<KeyBind>().CurrentValue)
                    return Mode.LaneClear;
                if (_orbwalkerMenu["las"].Cast<KeyBind>().CurrentValue)
                    return Mode.Lasthit;
                return Mode.None;
            }
        }


        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);

        public delegate void OnUnkillableEvenH(AttackableUnit unit, AttackableUnit target, int msTillDead);

        public static event BeforeAttackEvenH BeforeAttack;
        public static event AfterAttackEvenH AfterAttack;
        public static event OnUnkillableEvenH OnUnkillable;
        public static AIHeroClient Player = ObjectManager.Player;

        public static int LastDmg = HealthDeath.Now;

        private static int _previousAttack;

        private static bool _isTryingToAttack;
        private static int _lastAutoAttack;
        private static int _lastAutoAttackMove;
        private static int _lastmove;

        private static int _cantMoveTill;

        private static bool _attack = true;

        private static bool _disableNextAttack;

        private static bool _playerStoped;

        private static AttackableUnit _killUnit;

        public static Obj_AI_Base ForcedTarget;


        public static AttackableUnit LastAutoAttackUnit;


        public static List<AIHeroClient> AllEnemys = new List<AIHeroClient>();
        public static List<AIHeroClient> AllAllys = new List<AIHeroClient>();

        public static List<Obj_AI_Turret> EnemyTowers = new List<Obj_AI_Turret>();

        public static List<Obj_BarracksDampener> EnemyBarracs = new List<Obj_BarracksDampener>();


        public static List<Obj_AI_Base> EnemiesAround = new List<Obj_AI_Base>();

        public static List<Obj_HQ> EnemyHq = new List<Obj_HQ>();

        public static int AttackTime
        {
            get { return (int) (Player.AttackDelay*1000 + _orbwalkerMenu["att"].Cast<Slider>().CurrentValue); }
        }

        public static float MoveTime
        {
            get { return (int) (Player.AttackCastDelay*1000 + _orbwalkerMenu["mov"].Cast<Slider>().CurrentValue); }
        }

        private static void Init()
        {
            //While testing menu
            AllEnemys = EntityManager.Heroes.Enemies;
            AllAllys = EntityManager.Heroes.Allies;

            EnemyTowers = EntityManager.Turrets.Enemies;
            
            EnemyBarracs = ObjectManager.Get<Obj_BarracksDampener>().Where(tow => tow.IsEnemy).ToList();

            EnemyHq = ObjectManager.Get<Obj_HQ>().Where(tow => tow.IsEnemy).ToList();

        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            //Console.WriteLine("sender: "+sender.Name+" type: "+sender.Type);

            /*if (sender is MissileClient)
        {
            var mis = (MissileClient) sender;
            if (mis.SpellCaster.IsMe && IsAutoAttack(mis.SData.Name))
            {
                FireAfterAttack(player,(AttackableUnit) mis.Target);
            }
        }*/
            if (!Azir) return;
            if (sender.Name == "AzirSoldier" && sender.IsAlly)
            {
                Obj_AI_Minion myMin = sender as Obj_AI_Minion;
                if (myMin != null && myMin.BaseSkinName == "AzirSoldier")
                    AzirSoldiers.Add(myMin);
            }

        }

        private static void afterAttack(Obj_AI_Base sender, AttackableUnit target)
        {
            _isTryingToAttack = false;
            //Console.WriteLine("Hit is rdy " + Player.AttackDelay * 1000);
            _lastAutoAttackMove = 0;
            FireAfterAttack(sender, target);
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (IsAutoAttackReset(args.SData.Name))
                {
                    if (Player.ChampionName == "Lucian")
                        Core.DelayAction(ResetAutoAttackTimer, 350);
                    else
                        ResetAutoAttackTimer();
                    //
                }
                if (args.SData.IsAutoAttack())
                {
                    /*if(player.IsMelee)
                    Utility.DelayAction.Add((int)(player.AttackDelay * 1000), delegate { afterAttack(sender, (AttackableUnit)args.Target); });
                else*/
                    afterAttack(sender, (AttackableUnit) args.Target);
                }

            }
        }

        private static void OnStopAutoAttack(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsMe && args.DestroyMissile)
            {
                Console.WriteLine("Cancel auto");
                var resetTo = (_orbwalkerMenu["ant"].Cast<CheckBox>().CurrentValue) ? _previousAttack : 0;
                _lastAutoAttack = resetTo;
                _lastAutoAttackMove = resetTo;
            }
        }

        private static void OnStartAutoAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;
            /*foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(args.SData))
        {
            string name = descriptor.Name;
            object value = descriptor.GetValue(args.SData);
            Console.WriteLine("{0}={1}", name, value);
        }*/


            if (IsAutoAttack(args.SData.Name))
            {
                _previousAttack = _lastAutoAttack;
                _lastAutoAttack = Now;
                _lastAutoAttackMove = Now;
            }
            if (IsCantCancel(args.SData.Name))
            {
                _lastAutoAttackMove -= 100;
            }
            //Fire after attack!a
            /*if (sender.IsMelee)
            Utility.DelayAction.Add(
                (int)(sender.AttackCastDelay * 1000 + 40), () => FireAfterAttack(sender, (AttackableUnit)args.Target));*/


        }

        public static void DelayAttackfor(int ms)
        {
            // lastAutoAttack = (lastAutoAttack < now + ms) ? now + ms : lastAutoAttack;
        }

        public static void DisableMovementFor(int ms)
        {
            _cantMoveTill = Now + ms;
        }


        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (args.Source.NetworkId != Player.NetworkId)
                return;
            //Console.WriteLine("dmg: " + sender.Health + "  : " + Player.GetAutoAttackDamage((Obj_AI_Base)sender));


        }

        private static void OnDraw(EventArgs args)
        {
            if (_orbwalkerMenu["pac"].Cast<CheckBox>().CurrentValue)
            {
                new Circle
                {
                    Color = Color.ForestGreen,
                    Radius = _player.GetAutoAttackRange(),
                    BorderWidth = _orbwalkerMenu["liw"].Cast<Slider>().CurrentValue
                }.Draw(_player.Position);
            }

            if (_orbwalkerMenu["eac"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var target in
                    EntityManager.Heroes.Enemies.FindAll(target => target.IsValidTarget(1175)))
                {
                    new Circle
                    {
                        Color = Color.Firebrick,
                        Radius = target.GetAutoAttackRange(),
                        BorderWidth = _orbwalkerMenu["liw"].Cast<Slider>().CurrentValue
                    }.Draw(target.Position);
                }
            }

            if (_orbwalkerMenu["hoz"].Cast<CheckBox>().CurrentValue)
            {
                new Circle
                {
                    Color = Color.DodgerBlue,
                    Radius = _orbwalkerMenu["hol"].Cast<Slider>().CurrentValue,
                    BorderWidth = _orbwalkerMenu["liw"].Cast<Slider>().CurrentValue
                }.Draw(_player.Position);
            }

        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {
                deathWalk(Game.CursorPos,
                    CurrentMode != Mode.None
                        ? GetBestTarget()
                        : ((Azir && Player.HealthPercent > 30) ? GetBestTarget(Azir) : null), CurrentMode == Mode.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void DoAttack(AttackableUnit target)
        {
            FireBeforeAttack(target);
            if (EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, target))
            {
                _playerStoped = false;
                _previousAttack = _lastAutoAttack;
                _lastAutoAttack = Now;
                _lastAutoAttackMove = Now;
                LastAutoAttackUnit = target;
            }
        }

        public static void deathWalk(Vector3 goalPosition)
        {
            if (CurrentMode == Mode.None)
                deathWalk(goalPosition, GetBestTarget());
        }

        public static void deathWalk(Vector3 goalPosition, AttackableUnit target = null, bool noMove = false)
        {
            if (target != null && CanAttack() && inAutoAttackRange(target))
            {
                DoAttack(target);
            }
            if (CanMove() && !noMove)
            {
                if (target != null && (CurrentMode == Mode.Lasthit || CurrentMode == Mode.Harass))
                    _killUnit = target;
                if (_killUnit != null && !(_killUnit is AIHeroClient) && _killUnit.IsValid && !_killUnit.IsDead &&
                    _killUnit.Position.Distance(Player.Position) > getRealAutoAttackRange(_killUnit) - 30)
                    //Get in range
                    MoveTo(_killUnit.Position);
                MoveTo(goalPosition);
            }
        }

        public static AttackableUnit GetBestTarget(bool onlySolider = false)
        {
            bool soliderHit = false;
            if (ForcedTarget != null && !onlySolider)
            {
                if (inAutoAttackRange(ForcedTarget))
                    return ForcedTarget;
                ForcedTarget = null;
            }
            if (Azir)
            {
                EnemiesAround = ObjectManager.Get<Obj_AI_Base>()
                    .Where(targ => targ.IsValid && inAutoAttackRange(targ) && targ.IsEnemy).ToList();
            }
            else
            {
                EnemiesAround = ObjectManager.Get<Obj_AI_Base>()
                    .Where(targ => targ.IsValidTarget(GetTargetSearchDist()) && targ.IsEnemy).ToList();
            }

            Obj_AI_Base best = null;

            //Lat hit
            float bestPredHp = float.MaxValue;

            if (Azir)
            {
                var hero1 = GetBestHeroTarget(out soliderHit);

                if (hero1 != null && (EnemyInAzirRange(hero1) || hero1 is Obj_AI_Minion) && (!onlySolider || soliderHit))
                    return hero1;
            }
            if (!onlySolider)
                //check motherfuckers that are attacked by tower
                if (CurrentMode == Mode.Harass || CurrentMode == Mode.Lasthit || CurrentMode == Mode.LaneClear)
                {

                    foreach (var targ in EnemiesAround)
                    {
                        var towerShot = HealthDeath.AttackedByTurret(targ);
                        if (towerShot == null) continue;
                        var hpOnDmgPred = HealthDeath.GetLaneClearPred(targ, towerShot.HitOn + 10 - Now);

                        var aa = GetRealAaDmg(targ);
                        // Console.WriteLine("AAdmg: " + aa + " Hp after: " + hpOnDmgPred + " hit: " + (towerShot.hitOn - now));
                        if (hpOnDmgPred > aa && hpOnDmgPred <= aa*2f)
                        {
                            //Console.WriteLine("Tower under shoting");
                            //Notifications.AddNotification("Tower shoot");
                            //2x hit tower target

                            return targ;
                        }
                    }
                }
            if (!onlySolider)
                if (CurrentMode == Mode.Harass || CurrentMode == Mode.Lasthit || CurrentMode == Mode.LaneClear)
                {
                    //Last hit
                    foreach (
                        var targ in
                            EnemiesAround.OrderByDescending(
                                min => HealthDeath.GetLastHitPredPeriodic(min, TimeTillDamageOn(min))))
                    {
                        var hpOnDmgPred = HealthDeath.GetLastHitPred(targ, TimeTillDamageOn(targ));
                        if (hpOnDmgPred <= 0 &&
                            (LastAutoAttackUnit == null || LastAutoAttackUnit.NetworkId != targ.NetworkId))
                            FireOnUnkillable(Player, targ, HealthDeath.GetTimeTillDeath(targ));
                        if (hpOnDmgPred <= 0 || hpOnDmgPred > (int) GetRealAaDmg(targ))
                            continue;
                        var cannonBonus = (targ.BaseSkinName == "SRU_ChaosMinionSiege") ? 100 : 0;
                        if (best == null || hpOnDmgPred - cannonBonus < bestPredHp)
                        {
                            best = targ;
                            bestPredHp = hpOnDmgPred;
                        }
                    }
                    if (best != null)
                        return best;
                }
            var hero = GetBestHeroTarget(out soliderHit);

            if (hero != null && (!onlySolider || soliderHit))
                return hero;
            if (!onlySolider)

                if (ShouldWaitAllTogether())
                    return null;
            /* turrets / inhibitors / nexus */
            if (CurrentMode == Mode.LaneClear && 0 == 1)
            {
                /* turrets */
                foreach (var turret in
                    EnemyTowers.Where(t => t.IsValidTarget() && inAutoAttackRange(t)))
                {
                    return turret;
                }

                /* inhibitor */
                foreach (var turret in
                    EnemyBarracs.Where(t => t.IsValidTarget() && inAutoAttackRange(t)))
                {
                    return turret;
                }

                /* nexus */
                foreach (var nexus in
                    EnemyHq.Where(t => t.IsValidTarget() && inAutoAttackRange(t)))
                {
                    return nexus;
                }
            }

            if (!onlySolider)
                //Laneclear
                if (CurrentMode == Mode.LaneClear)
                {
                    best = EnemiesAround.Where(min => !ShouldWaitMinion(min))
                        .OrderByDescending(targ => targ.Health + (EnemyInAzirRange(targ) ? 1000 : 0)).FirstOrDefault();
                }


            return best;
        }

        private static Obj_AI_Base GetBestHeroTarget(out bool soliderHit)
        {
            AIHeroClient killableEnemy = null;
            var hitsToKill = double.MaxValue;

            if (Azir)
            {
                foreach (var ene in AllEnemys.OrderBy(enemy => enemy.Health))
                {
                    if (ene == null || ene.IsDead || !ene.IsTargetable || ene.IsInvulnerable)
                        continue;
                    foreach (var sol in GetActiveSoliders())
                    {
                        if (sol == null || sol.IsDead)
                            continue;
                        var solAarange = 325;
                        solAarange *= solAarange;
                        if (ene.ServerPosition.Distance(sol.ServerPosition, true) < solAarange)
                        {
                            soliderHit = true;
                            return ene;
                        }
                        foreach (
                            var around in
                                EnemiesAround.Where(
                                    arou =>
                                        arou != null && arou.IsValid && !arou.IsDead &&
                                        arou.Position.Distance(sol.Position, true) <=
                                        ((AzirSoliderRange)*(AzirSoliderRange))))
                        {
                            if (around == null || around.IsDead || ene == null)
                                continue;
                            var poly = MathUtils.getPolygonOn(sol, around, 50 + ene.BoundingRadius/2,
                                AzirSoliderRange + ene.BoundingRadius/2);
                            var posi = Prediction.Position.PredictUnitPosition(ene, (int) Player.AttackCastDelay);
                            try
                            {

                                if (posi != null &&
                                    poly.PointInside(posi))
                                {
                                    soliderHit = true;
                                    return around;
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            foreach (var enemy in AllEnemys.Where(hero => hero.IsValid && inAutoAttackRange(hero)))
            {
                var killHits = CountKillhits(enemy);
                if (killableEnemy != null && !(killHits < hitsToKill))
                    continue;
                killableEnemy = enemy;
                hitsToKill = killHits;
            }
            soliderHit = false;
            return hitsToKill < 4
                ? killableEnemy
                : TargetSelector.GetTarget(Player.AttackRange + Player.BoundingRadius, DamageType.Physical);
        }

        private static double CountKillhits(AIHeroClient enemy)
        {
            return enemy.Health/GetRealAaDmg(enemy);
        }

        private static bool ShouldWaitAllTogether()
        {
            /* var cEnemy = getCloestEnemyChamp();
         bool enemySoonInRange = cEnemy != null &&
                                 inAutoAttackRange(cEnemy,
                                     LeagueSharp.Common.Prediction.GetPrediction(cEnemy,player.AttackDelay*1000).UnitPosition.To2D());
         if (enemySoonInRange)
             return true;*/

            foreach (var minion in MinionManager.GetMinions(GetTargetSearchDist(), MinionTypes.All))
            {
                if (minion.IsValidTarget())
                {
                    //var hpKillable = HealthDeath.getLastHitPredPeriodic(minion, timeTillDamageOn(minion));
                    // if(hpKillable<0)
                    //    continue;
                    var dmgAt = TimeTillDamageOn(minion);
                    var hp = HealthDeath.GetLaneClearPred(minion, (int) ((Player.AttackDelay*1000)*1.26f));
                    if (hp <= GetRealAaDmg(minion))
                        return true;
                }
            }
            return false;
        }

        private static bool ShouldWaitMinion(Obj_AI_Base minion)
        {
            var hp = HealthDeath.GetLaneClearPred(minion, (int) ((Player.AttackDelay*1000)*2.26f));
            if (hp <= GetRealAaDmg(minion))
                return true;
            return false;
        }

        public static AIHeroClient GetCloestEnemyChamp()
        {
            return AllEnemys
                .Where(ob => ob.IsValid && !ob.IsDead)
                .OrderBy(ob => ob.Distance(Player, true))
                .FirstOrDefault();
        }

        public static float GetTargetSearchDist()
        {
            return Player.AttackRange + Player.BoundingRadius + _orbwalkerMenu["run"].Cast<Slider>().CurrentValue;
        }

        public static int TimeTillDamageOn(Obj_AI_Base unit)
        {
            var dist = unit.ServerPosition.Distance(Player.ServerPosition);
            int addTime = -_orbwalkerMenu["far"].Cast<Slider>().CurrentValue - ((Azir) ? 100 : 0); //some farm delay
            if (!inAutoAttackRange(unit)) //+ check if want to move to killabel minion and range it wants to
            {
                var realDist = realDistanceTill(unit);
                var aaRange = getRealAutoAttackRange(unit);

                addTime += (int) (((realDist - aaRange)*1000)/Player.MoveSpeed);
            }

            if (Player.IsMelee || Azir)
            {
                return (int) (CanAttackAfter() + Player.AttackCastDelay*1000) + addTime;
            }
            else
            {
                var misDist = dist;
                return
                    (int)
                        (CanAttackAfter() + Player.AttackCastDelay*1000 + (misDist*1000)/Player.BasicAttack.MissileSpeed) +
                    addTime;
            }
        }

        public static bool inAutoAttackRange(AttackableUnit unit)
        {
            if (!unit.IsValidTarget())
            {
                return false;
            }

            if (Azir && unit is Obj_AI_Base && EnemyInAzirRange((Obj_AI_Base) unit))
            {
                return true;
            }

            var myRange = getRealAutoAttackRange(unit);
            return
                Vector2.DistanceSquared(
                    (unit as Obj_AI_Base)!=null ? ((Obj_AI_Base) unit).ServerPosition.To2D() : unit.Position.To2D(),
                    Player.ServerPosition.To2D()) <= myRange*myRange;
        }

        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }

        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) ||
                   Attacks.Contains(name.ToLower());
        }

        public static bool IsCantCancel(string name)
        {
            return AttacksCantCancel.Contains(name.ToLower());
        }


        public static bool inAutoAttackRange(AttackableUnit unit, Vector2 pos)
        {
            if (!unit.IsValidTarget())
            {
                return false;
            }

            if (Azir && unit is Obj_AI_Base && EnemyInAzirRange((Obj_AI_Base) unit))
            {
                return true;
            }

            var myRange = getRealAutoAttackRange(unit);
            return
                Vector2.DistanceSquared(
                    pos,
                    Player.ServerPosition.To2D()) <= myRange*myRange;
        }

        public static bool inAutoAttackRange(Obj_AI_Base source, AttackableUnit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }

            if (source.IsMe && Azir && target is Obj_AI_Base && EnemyInAzirRange((Obj_AI_Base) target))
            {
                return true;
            }

            var myRange = getRealAutoAttackRange(source, target);
            return
                Vector2.DistanceSquared(
                    target.Position.To2D(),
                    source.ServerPosition.To2D()) <= myRange*myRange;
        }

        public static float getRealAutoAttackRange(AttackableUnit unit)
        {
            return getRealAutoAttackRange(Player, unit);
        }

        public static float getRealAutoAttackRange(Obj_AI_Base source, AttackableUnit target)
        {
            var result = source.AttackRange + source.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        public static void MoveTo(Vector3 goalPosition)
        {
            if (Now - _lastmove < 0) //Humanizer
                return;
            if (Player.ServerPosition.Distance(goalPosition) < _orbwalkerMenu["hol"].Cast<Slider>().CurrentValue)
            {
                if (!_playerStoped)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.Stop, Player.ServerPosition);
                    _playerStoped = true;
                }
                return;
            }
            _playerStoped = false;
            if (EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, goalPosition))
                _lastmove = Now;
        }

        public static void SetAttack(bool val)
        {
            _attack = val;
        }

        public static bool CanAttack()
        {
            return CanAttackAfter() == 0 && _attack && _cantMoveTill < Now;
        }

        public static int CanAttackAfter()
        {
            var after = Player.AttackDelay*1000 + _lastAutoAttack - Now +
                        _orbwalkerMenu["att"].Cast<Slider>().CurrentValue;
            return (int) (after > 0 ? after : 0);
        }

        public static bool CanMove()
        {
            return CanMoveAfter() == 0 && _cantMoveTill < Now;
        }

        public static int CanMoveAfter()
        {
            var after = _lastAutoAttackMove + Player.AttackCastDelay*1000 - Now +
                        _orbwalkerMenu["mov"].Cast<Slider>().CurrentValue + ((HyperCharged()) ? 150 : 0);
            var aaBefore = (_isTryingToAttack) ? (_lastAutoAttack + 350) - Now : 0;
            return (int) (after > 0 ? after : (aaBefore > 0) ? aaBefore : 0);
        }

        private static bool HyperCharged()
        {
            return Player.Buffs.Any(buffs => buffs.Name == "jaycehypercharge");
        }

        public static void ResetAutoAttackTimer()
        {
            //Console.WriteLine("Reseet");
            _lastAutoAttack = 0;
            //lastAutoAttackMove = 0;
        }

        public static float realDistanceTill(AttackableUnit unit)
        {
            return realDistanceTill(Player, unit);
        }

        public static float realDistanceTill(Obj_AI_Base source, AttackableUnit target)
        {
            float dist = 0;
            var dists = source.GetPath(target.Position);
            if (dists.Count() == 0)
                return 0;
            Vector3 from = dists[0];
            foreach (var to in dists)
            {
                dist += Vector3.Distance(from, to);
                from = to;
            }
            return dist;
        }

        public class BeforeAttackEventArgs
        {
            public AttackableUnit Target;
            public Obj_AI_Base Unit = ObjectManager.Player;
            private bool _process = true;

            public bool Process
            {
                get { return _process; }
                set
                {
                    _disableNextAttack = !value;
                    _process = value;
                }
            }
        }

        private static void FireBeforeAttack(AttackableUnit target)
        {
            _isTryingToAttack = true;
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs
                {
                    Target = target
                });
            }
            else
            {
                _disableNextAttack = false;
            }
        }

        private static void FireAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            _lastAutoAttackMove = 0;
            //set can move
            if (AfterAttack != null)
            {
                AfterAttack(unit, target);
            }
        }

        private static void FireOnUnkillable(AttackableUnit unit, AttackableUnit target, int msTillDead)
        {
            //set can move
            if (OnUnkillable != null)
            {
                OnUnkillable(unit, target, msTillDead);
            }
        }

        public static void AddToMenu()
        {
            _player = ObjectManager.Player;
            _orbwalkerMenu = MainMenu.AddMenu("VOrbwalker", "orb", "Volatile Orbwalker");

            /*Load the menu*/
            _orbwalkerMenu.Add("las", new KeyBind("Last Hit", false, KeyBind.BindTypes.HoldActive, 'X'));
            _orbwalkerMenu.Add("lan", new KeyBind("Lane Clear", false, KeyBind.BindTypes.HoldActive, 'V'));
            _orbwalkerMenu.Add("mix", new KeyBind("Mixed", false, KeyBind.BindTypes.HoldActive, 'C'));
            _orbwalkerMenu.Add("com", new KeyBind("Combo", false, KeyBind.BindTypes.HoldActive, 32));
            /* Sliders */
            _orbwalkerMenu.Add("hol", new Slider("Hold Position Radius", 100, 0, 250));
            _orbwalkerMenu.Add("mov", new Slider("Movement Delay", 0, -100, 250));
            _orbwalkerMenu.Add("att", new Slider("Attack Delay", 0, -100, 250));
            _orbwalkerMenu.Add("far", new Slider("Farm Delay", 30, 0, 200));
            _orbwalkerMenu.Add("run", new Slider("Run for CS", 25, 0, 500));
            _orbwalkerMenu.Add("ant", new CheckBox("Attempt Anti-Stutter", false));
            /* Drawings submenu */
            _orbwalkerMenu.AddGroupLabel("Drawings");
            _orbwalkerMenu.Add("pac", new CheckBox("Player AA Circle"));
            _orbwalkerMenu.Add("eac", new CheckBox("Enemy AA Circle"));
            _orbwalkerMenu.Add("hoz", new CheckBox("Hold Zone"));
            _orbwalkerMenu.Add("liw", new Slider("Line Width", 1, 1, 6));

            if (_player.ChampionName == "Kalista")
            {
                _orbwalkerMenu.Add("min", new CheckBox("Kalista - Combo Orbwalk with Minions"));
                _orbwalkerMenu.Add("exp", new CheckBox("Kalista - Use Passive Exploit", false));
            }

            Init();

            Drawing.OnDraw += OnDraw;



            Obj_AI_Base.OnProcessSpellCast += OnStartAutoAttack;
            Spellbook.OnStopCast += OnStopAutoAttack;

            Obj_AI_Base.OnSpellCast += OnDoCast;

            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Obj_AI_Minion.OnPlayAnimation += Obj_AI_Minion_OnPlayAnimation;

            Game.OnUpdate += OnUpdate;
        }


        public static float GetRealAaDmg(Obj_AI_Base targ)
        {
            if (!Azir)
                return (float) Player.GetAutoAttackDamage(targ, true);
            var solAround = SolidersAroundEnemy(targ);

            if (solAround == 0)
                return (float) Player.GetAutoAttackDamage(targ);
            int[] solBaseDmg = {50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 110, 120, 130, 140, 150, 160, 170};

            var solDmg = solBaseDmg[Player.Level - 1] + Player.FlatMagicDamageMod*0.6f;

            return (float) Player.CalculateDamageOnUnit(targ, DamageType.Magical, solDmg + (solAround - 1)*solDmg*0.25f);

        }


        //Azir stuff

        public static int AzirSoliderReach = 375;
        public static int AzirSoliderRange = 325;
        //Tnx Kortatu ;)
        private static Dictionary<int, string> _animations = new Dictionary<int, string>();

        public static List<Obj_AI_Minion> AzirSoldiers = new List<Obj_AI_Minion>();
        private static AIHeroClient _player;
        private static Menu _orbwalkerMenu;

        public static List<Obj_AI_Minion> GetUsableSoliders()
        {
            return AzirSoldiers.Where(sol => !sol.IsDead && sol != null).ToList();
        }

        public static List<Obj_AI_Minion> GetActiveSoliders()
        {
            return
                AzirSoldiers.Where(
                    s =>
                        s.IsValid && !s.IsMoving && !s.IsDead && !s.IsMoving &&
                        ObjectManager.Player.Distance(s) <= 875*875
                    /*(!Animations.ContainsKey(s.NetworkId) || Animations[s.NetworkId] != "Inactive")*/).ToList();
        }

        public static bool SolisAreStill()
        {
            List<Obj_AI_Minion> solis = GetActiveSoliders();
            return solis.All(sol => sol.CanAttack);
        }

        public static List<AIHeroClient> GetEnemiesInSolRange()
        {
            List<Obj_AI_Minion> solis = GetActiveSoliders();
            List<AIHeroClient> inRange = new List<AIHeroClient>();

            if (solis.Count == 0)
                return null;
            foreach (var ene in AllEnemys.Where(ene => ene.IsEnemy && ene.IsVisible && !ene.IsDead))
            {
                foreach (var sol in solis)
                {
                    if (ene.Distance(sol, true) < AzirSoliderRange*AzirSoliderRange)
                    {
                        inRange.Add(ene);
                        break;
                    }
                }
            }
            return inRange;
        }

        public static bool EnemyInAzirRange(Obj_AI_Base ene)
        {
            var solis = GetActiveSoliders();

            return !ene.IsDead && solis.Count != 0 &&
                   solis.Where(sol => !sol.IsMoving && !Dash.IsDashing(sol))
                       .Any(sol => ene.Distance(sol, true) < AzirSoliderRange*AzirSoliderRange);
        }

        public static int SolidersAroundEnemy(Obj_AI_Base ene)
        {
            var solis = GetActiveSoliders();

            return solis.Count(sol => ene.Distance(sol, true) < AzirSoliderRange*AzirSoliderRange);
        }

        private static void Obj_AI_Minion_OnPlayAnimation(GameObject sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe && Player.IsMelee && args.Animation.StartsWith("Attack"))
            {
                _isTryingToAttack = false;
                _lastAutoAttackMove = Now;
            }

            if (!Azir) return;
            if (sender.Name == "AzirSoldier" && sender.IsAlly)
            {
                Obj_AI_Minion myMin = sender as Obj_AI_Minion;
                if (myMin.BaseSkinName == "AzirSoldier")
                {
                    _animations[sender.NetworkId] = args.Animation;
                }
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (!Azir)
                return;
            AzirSoldiers.RemoveAll(s => s.NetworkId == sender.NetworkId);
            _animations.Remove(sender.NetworkId);
        }
    }
}