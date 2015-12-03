using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;
#pragma warning disable 169

namespace VolatileAIO.Organs.Brain.Test2
{
    /// <summary>
    ///     This class offers everything related to auto-attacks and orbwalking.
    /// </summary>
    public static class Volkswagen
    {
        private const float HeathDebuffer = 15f;
        private static readonly string OrbwalkerName = "Volkswagen";
        private static Menu _orbwalkerMenu, _misc, _drawings;

        /// <summary>
        /// Delegate AfterAttackEvenH
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);

        /// <summary>
        /// Delegate BeforeAttackEvenH
        /// </summary>
        /// <param name="args">The <see cref="BeforeAttackEventArgs"/> instance containing the event data.</param>
        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        /// <summary>
        /// Delegate OnAttackEvenH
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        public delegate void OnAttackEvenH(AttackableUnit unit, AttackableUnit target);

        /// <summary>
        /// Delegate OnNonKillableMinionH
        /// </summary>
        /// <param name="minion">The minion.</param>
        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        /// <summary>
        /// Delegate OnTargetChangeH
        /// </summary>
        /// <param name="oldTarget">The old target.</param>
        /// <param name="newTarget">The new target.</param>
        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        /// <summary>
        /// The orbwalking mode.
        /// </summary>
        public enum OrbwalkingMode
        {
            /// <summary>
            /// The orbalker will only last hit minions.
            /// </summary>
            LastHit,

            /// <summary>
            /// The orbwalker will alternate between last hitting and auto attacking champions.
            /// </summary>
            Mixed,

            /// <summary>
            /// The orbwalker will clear the lane of minions as fast as possible while attempting to get the last hit.
            /// </summary>
            LaneClear,

            /// <summary>
            /// The orbwalker will only attack the target.
            /// </summary>
            Combo,

            /// <summary>
            /// The orbwalker will only move.
            /// </summary>
            CustomMode,

            /// <summary>
            /// The orbwalker does nothing.
            /// </summary>
            None
        }

        /// <summary>
        /// Spells that reset the attack timer.
        /// </summary>
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack",
            "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq",
            "xenzhaocombotarget", "yorickspectral", "reksaiq", "itemtitanichydracleave"
        };


        /// <summary>
        /// Spells that are not attacks even if they have the "attack" word in their name.
        /// </summary>
        private static readonly string[] NoAttacks =
        {
            "volleyattack", "volleyattackwithsound", "jarvanivcataclysmattack",
            "monkeykingdoubleattack", "shyvanadoubleattack",
            "shyvanadoubleattackdragon", "zyragraspingplantattack",
            "zyragraspingplantattack2", "zyragraspingplantattackfire",
            "zyragraspingplantattack2fire", "viktorpowertransfer",
            "sivirwattackbounce", "asheqattacknoonhit",
            "elisespiderlingbasicattack", "heimertyellowbasicattack",
            "heimertyellowbasicattack2", "heimertbluebasicattack",
            "annietibbersbasicattack", "annietibbersbasicattack2",
            "yorickdecayedghoulbasicattack", "yorickravenousghoulbasicattack",
            "yorickspectralghoulbasicattack", "malzaharvoidlingbasicattack",
            "malzaharvoidlingbasicattack2", "malzaharvoidlingbasicattack3",
            "kindredwolfbasicattack", "kindredbasicattackoverridelightbombfinal"
        };


        /// <summary>
        /// Spells that are attacks even if they dont have the "attack" word in their name.
        /// </summary>
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute",
            "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2",
            "xenzhaothrust3", "viktorqbuff"
        };

        /// <summary>
        /// Champs whose auto attacks can't be cancelled
        /// </summary>
        private static readonly string[] NoCancelChamps = {"Kalista"};

        /// <summary>
        /// The last auto attack tick
        /// </summary>
        public static int LastAaTick;

        /// <summary>
        /// <c>true</c> if the orbwalker will attack.
        /// </summary>
        public static bool Attack = true;

        /// <summary>
        /// <c>true</c> if the orbwalker will skip the next attack.
        /// </summary>
        public static bool DisableNextAttack;

        /// <summary>
        /// <c>true</c> if the orbwalker will move.
        /// </summary>
        public static bool Move = true;

        /// <summary>
        /// The tick the most recent move command was sent.
        /// </summary>
        public static int LastMoveCommandT;

        /// <summary>
        /// The last move command position
        /// </summary>
        public static Vector3 LastMoveCommandPosition = Vector3.Zero;

        /// <summary>
        /// The last target
        /// </summary>
        private static AttackableUnit _lastTarget;

        /// <summary>
        /// The player
        /// </summary>
        private static AIHeroClient _player;


        /// <summary>
        /// The delay
        /// </summary>
        private static int _delay;

        /// <summary>
        /// The minimum distance
        /// </summary>
        private static float _minDistance = 400;

        /// <summary>
        /// <c>true</c> if the auto attack missile was launched from the player.
        /// </summary>
        private static bool _missileLaunched;

        /// <summary>
        /// The champion name
        /// </summary>
        private static string _championName;

        /// <summary>
        /// The random
        /// </summary>
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Initializes static members of the <see cref="Orbwalking"/> class.
        /// </summary>
        static Volkswagen()
        {
            _player = ObjectManager.Player;
            _championName = _player.ChampionName;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnDoCast;
            Spellbook.OnStopCast += SpellbookOnStopCast;
        }

        /// <summary>
        /// This event is fired before the player auto attacks.
        /// </summary>
        public static event BeforeAttackEvenH BeforeAttack;

        /// <summary>
        /// This event is fired when a unit is about to auto-attack another unit.
        /// </summary>
        public static event OnAttackEvenH OnAttack;

        /// <summary>
        /// This event is fired after a unit finishes auto-attacking another unit (Only works with player for now).
        /// </summary>
        public static event AfterAttackEvenH AfterAttack;

        /// <summary>
        /// Gets called on target changes
        /// </summary>
        public static event OnTargetChangeH OnTargetChange;

        ///<summary>
        /// Occurs when a minion is not killable by an auto attack.
        /// </summary>
        public static event OnNonKillableMinionH OnNonKillableMinion;

        /// <summary>
        /// Fires the before attack event.
        /// </summary>
        /// <param name="target">The target.</param>
        private static void FireBeforeAttack(AttackableUnit target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs {Target = target});
            }
            else
            {
                DisableNextAttack = false;
            }
        }

        /// <summary>
        /// Fires the on attack event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        private static void FireOnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (OnAttack != null)
            {
                OnAttack(unit, target);
            }
        }

        /// <summary>
        /// Fires the after attack event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        private static void FireAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (AfterAttack != null && target.IsValidTarget())
            {
                AfterAttack(unit, target);
            }
        }

        /// <summary>
        /// Fires the on target switch event.
        /// </summary>
        /// <param name="newTarget">The new target.</param>
        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.IsValidTarget() || _lastTarget != newTarget))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        /// <summary>
        /// Fires the on non killable minion event.
        /// </summary>
        /// <param name="minion">The minion.</param>
        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }

        /// <summary>
        /// Returns true if the spellname resets the attack timer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name is an auto attack reset; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }

        /// <summary>
        /// Returns true if the unit is melee
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns><c>true</c> if the specified unit is melee; otherwise, <c>false</c>.</returns>
        public static bool IsMelee(this Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }

        /// <summary>
        /// Returns true if the spellname is an auto-attack.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the name is an auto attack; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) ||
                   Attacks.Contains(name.ToLower());
        }

        /// <summary>
        /// Returns the auto-attack range of local player with respect to the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetRealAutoAttackRange(AttackableUnit target)
        {
            var result = _player.AttackRange + _player.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        /// <summary>
        /// Returns the auto-attack range of the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetAttackRange(AIHeroClient target)
        {
            var result = target.AttackRange + target.BoundingRadius;
            return result;
        }

        /// <summary>
        /// Returns true if the target is in auto-attack range.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }
            var myRange = GetRealAutoAttackRange(target);
            float? distanceSquared = null;
            try
            {
                var objAiBase = target as Obj_AI_Base;
                distanceSquared = Vector2.DistanceSquared(
                    objAiBase.ServerPosition.To2D() != null ? ((Obj_AI_Base) target).ServerPosition.To2D() : target.Position.To2D(),
                    _player.ServerPosition.To2D());
            }
            catch (Exception e)
            {
                Chat.Print(e);
            }
            if (distanceSquared != null)
                return
                    distanceSquared <= myRange*myRange;
            return false;
        }

        /// <summary>
        /// Returns player auto-attack missile speed.
        /// </summary>
        /// <returns>System.Single.</returns>
        public static float GetMyProjectileSpeed()
        {
            return IsMelee(_player) || _championName == "Azir" || _championName == "Velkoz" ||
                   _championName == "Viktor" && _player.HasBuff("ViktorPowerTransferReturn")
                ? float.MaxValue
                : _player.BasicAttack.MissileSpeed;
        }

        /// <summary>
        /// Returns if the player's auto-attack is ready.
        /// </summary>
        /// <returns><c>true</c> if this instance can attack; otherwise, <c>false</c>.</returns>
        public static bool CanAttack()
        {
            if (Orbwalker.PassiveExploit && Environment.TickCount*1000 >= EloBuddy.SDK.Orbwalker.LastAutoAttack + 2)
                return Environment.TickCount >= LastAaTick + _player.AttackDelay*1000 && Attack;

            return Environment.TickCount + Game.Ping/2 + 25 >= LastAaTick + _player.AttackDelay*1000 && Attack;
        }

        /// <summary>
        /// Returns true if moving won't cancel the auto-attack.
        /// </summary>
        /// <param name="extraWindup">The extra windup.</param>
        /// <returns><c>true</c> if this instance can move the specified extra windup; otherwise, <c>false</c>.</returns>
        public static bool CanMove(float extraWindup)
        {
            if (!Move)
            {
                return false;
            }

            if (_missileLaunched && Orbwalker.MissileCheck)
            {
                return true;
            }

            var localExtraWindup = 0;
            if (_championName == "Rengar" && (_player.HasBuff("rengarqbase") || _player.HasBuff("rengarqemp")))
            {
                localExtraWindup = 200;
            }

            return NoCancelChamps.Contains(_championName) ||
                   (Environment.TickCount + Game.Ping/2 >=
                    LastAaTick + _player.AttackCastDelay*1000 + extraWindup + localExtraWindup);
        }

        /// <summary>
        /// Sets the movement delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public static void SetMovementDelay(int delay)
        {
            _delay = delay;
        }

        /// <summary>
        /// Sets the minimum orbwalk distance.
        /// </summary>
        /// <param name="d">The d.</param>
        public static void SetMinimumOrbwalkDistance(float d)
        {
            _minDistance = d;
        }

        /// <summary>
        /// Gets the last move time.
        /// </summary>
        /// <returns>System.Single.</returns>
        public static float GetLastMoveTime()
        {
            return LastMoveCommandT;
        }

        /// <summary>
        /// Gets the last move position.
        /// </summary>
        /// <returns>Vector3.</returns>
        public static Vector3 GetLastMovePosition()
        {
            return LastMoveCommandPosition;
        }

        /// <summary>
        /// Moves to the position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="holdAreaRadius">The hold area radius.</param>
        /// <param name="overrideTimer">if set to <c>true</c> [override timer].</param>
        /// <param name="useFixedDistance">if set to <c>true</c> [use fixed distance].</param>
        /// <param name="randomizeMinDistance">if set to <c>true</c> [randomize minimum distance].</param>
        public static void MoveTo(Vector3 position,
            float holdAreaRadius = 0,
            bool overrideTimer = false,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            var playerPosition = _player.ServerPosition;

            if (playerPosition.Distance(position, true) < holdAreaRadius*holdAreaRadius)
            {
                if (_player.Path.Length <= 0) return;
                EloBuddy.Player.IssueOrder(GameObjectOrder.Stop, playerPosition);
                LastMoveCommandPosition = playerPosition;
                LastMoveCommandT = Environment.TickCount - 70;
                return;
            }

            var point = position;

            if (_player.Distance(point, true) < 150*150)
            {
                point =
                    playerPosition.Extend(position,
                        (randomizeMinDistance ? (Random.NextFloat(0.6f, 1) + 0.2f)*_minDistance : _minDistance)).To3D();
            }
            var angle = 0f;
            var currentPath = _player.GetWaypoints();
            if (currentPath.Count > 1 && currentPath.GetPathLength() > 100)
            {
                var movePath = _player.GetPath(point);

                if (movePath.Length > 1)
                {
                    var v1 = currentPath[1] - currentPath[0];
                    var v2 = movePath[1] - movePath[0];
                    angle = v1.AngleBetween(v2.To2D());
                    var distance = movePath.Last().To2D().Distance(currentPath.Last(), true);

                    if ((angle < 10 && distance < 500*500) || distance < 50*50)
                    {
                        return;
                    }
                }
            }

            if (Orbwalker.PassiveExploit && Environment.TickCount*1000 >= EloBuddy.SDK.Orbwalker.LastAutoAttack + 2)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, point);
                LastMoveCommandPosition = point;
                LastMoveCommandT = Environment.TickCount;
                return;
            }

            else if (Environment.TickCount - LastMoveCommandT < (70 + Math.Min(60, Game.Ping)) && !overrideTimer &&
                     angle < 60)
            {
                return;
            }

            else if (angle >= 60 && Environment.TickCount - LastMoveCommandT < 60)
            {
                return;
            }

            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, point);
            LastMoveCommandPosition = point;
            LastMoveCommandT = Environment.TickCount;
        }

        /// <summary>
        /// Orbwalks a target while moving to Position.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="position">The position.</param>
        /// <param name="extraWindup">The extra windup.</param>
        /// <param name="holdAreaRadius">The hold area radius.</param>
        /// <param name="useFixedDistance">if set to <c>true</c> [use fixed distance].</param>
        /// <param name="randomizeMinDistance">if set to <c>true</c> [randomize minimum distance].</param>
        public static void Orbwalk(AttackableUnit target,
            Vector3 position,
            float extraWindup = 90,
            float holdAreaRadius = 0,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {
            try
            {
                if (target.IsValidTarget() && CanAttack())
                {
                    DisableNextAttack = false;
                    FireBeforeAttack(target);

                    if (!DisableNextAttack)
                    {
                        if (!NoCancelChamps.Contains(_championName))
                        {
                            if (Orbwalker.PassiveExploit)
                                LastAaTick = Environment.TickCount - (int) (ObjectManager.Player.AttackCastDelay*1000f) -
                                             100;
                            else
                                LastAaTick = Environment.TickCount + Game.Ping + 100 -
                                             (int) (ObjectManager.Player.AttackCastDelay*1000f);

                            _missileLaunched = false;

                            var d = GetRealAutoAttackRange(target) - 65;
                            if (_player.Distance(target, true) > d*d && !_player.IsMelee)
                            {
                                if (Orbwalker.PassiveExploit || 1/_player.AttackDelay < 1.65)
                                    LastAaTick =
                                        (int) (Environment.TickCount + ObjectManager.Player.AttackCastDelay*1000f - 155);

                                else
                                    LastAaTick = Environment.TickCount + Game.Ping + 400 -
                                                 (int) (ObjectManager.Player.AttackCastDelay*1000f);

                            }
                        }

                        if (!EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, target))
                        {
                            ResetAutoAttackTimer();
                        }

                        LastMoveCommandT = 0;
                        _lastTarget = target;
                        return;
                    }
                }

                if (CanMove(extraWindup))
                {
                    MoveTo(position, holdAreaRadius, false, useFixedDistance, randomizeMinDistance);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Resets the Auto-Attack timer.
        /// </summary>
        public static void ResetAutoAttackTimer()
        {
            LastAaTick = 0;
        }

        /// <summary>
        /// Fired when the spellbook stops casting a spell.
        /// </summary>
        /// <param name="spellbook">The spellbook.</param>
        /// <param name="args">The <see cref="SpellbookStopCastEventArgs"/> instance containing the event data.</param>
        private static void SpellbookOnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsValid && sender.IsMe && args.DestroyMissile && args.StopAnimation)
            {
                ResetAutoAttackTimer();
            }
        }

        /// <summary>
        /// Fired when an auto attack is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.</param>
        private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && IsAutoAttack(args.SData.Name))
            {
                if (Game.Ping <= 30) //First world problems kappa
                {
                    Core.DelayAction(() => Obj_AI_Base_OnDoCast_Delayed(sender, args), 30);
                    return;
                }

                Obj_AI_Base_OnDoCast_Delayed(sender, args);
            }
        }

        /// <summary>
        /// Fired 30ms after an auto attack is launched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.</param>
        private static void Obj_AI_Base_OnDoCast_Delayed(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            FireAfterAttack(sender, args.Target as AttackableUnit);
            _missileLaunched = true;
        }

        /// <summary>
        /// Handles the <see cref="E:ProcessSpell" /> event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="spell">The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.</param>
        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            try
            {
                var spellName = spell.SData.Name;

                if (IsAutoAttackReset(spellName) && unit.IsMe)
                {
                    Core.DelayAction(ResetAutoAttackTimer, 250);
                }

                if (!IsAutoAttack(spellName))
                {
                    return;
                }

                if (unit.IsMe &&
                    (spell.Target is Obj_AI_Base || spell.Target is Obj_BarracksDampener || spell.Target is Obj_HQ))
                {
                    LastAaTick = Environment.TickCount - Game.Ping/2;
                    _missileLaunched = false;

                    if (spell.Target is Obj_AI_Base)
                    {
                        var target = (Obj_AI_Base) spell.Target;
                        if (target.IsValid)
                        {
                            FireOnTargetSwitch(target);
                            _lastTarget = target;
                        }
                    }
                }

                FireOnAttack(unit, _lastTarget);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// The before attack event arguments.
        /// </summary>
        public class BeforeAttackEventArgs : EventArgs
        {
            /// <summary>
            /// <c>true</c> if the orbwalker should continue with the attack.
            /// </summary>
            private bool _process = true;

            /// <summary>
            /// The target
            /// </summary>
            public AttackableUnit Target;

            /// <summary>
            /// The unit
            /// </summary>
            public Obj_AI_Base Unit = ObjectManager.Player;

            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="BeforeAttackEventArgs"/> should continue with the attack.
            /// </summary>
            /// <value><c>true</c> if the orbwalker should continue with the attack; otherwise, <c>false</c>.</value>
            public bool Process
            {
                get { return _process; }
                set
                {
                    DisableNextAttack = !value;
                    _process = value;
                }
            }
        }

        /// <summary>
        /// This class allows you to add an instance of "Orbwalker" to your assembly in order to control the orbwalking in an
        /// easy way.
        /// </summary>
        public class Orbwalker
        {
            /// <summary>
            /// The lane clear wait time modifier.
            /// </summary>
            private const float LaneClearWaitTimeMod = 2f;

            private bool _enabled;

            /// <summary>
            /// The configuration
            /// </summary>
            private static Menu _config;

            /// <summary>
            /// The player
            /// </summary>
            private static AIHeroClient _player;

            /// <summary>
            /// The forced target
            /// </summary>
            private Obj_AI_Base _forcedTarget;

            /// <summary>
            /// The orbalker mode
            /// </summary>
            private OrbwalkingMode _mode = OrbwalkingMode.None;

            /// <summary>
            /// The orbwalking point
            /// </summary>
            private Vector3 _orbwalkingPoint;

            /// <summary>
            /// The previous minion the orbwalker was targeting.
            /// </summary>
            private Obj_AI_Minion _prevMinion;

            /// <summary>
            /// The instances of the orbwalker.
            /// </summary>
            public static List<Orbwalker> Instances = new List<Orbwalker>();

            /// <summary>
            /// Initializes a new instance of the <see cref="Orbwalker"/> class.
            /// </summary>
            /// <param name="attachToMenu">The menu the orbwalker should attach to.</param>
            public Orbwalker()
            {
                _player = ObjectManager.Player;
                _orbwalkerMenu = MainMenu.AddMenu("VOrbwalker", "orb", "Volatile Orbwalker");
                /* Drawings submenu */

                _drawings = _orbwalkerMenu.AddSubMenu("Drawings", "draw");
                _drawings.Add("pac", new CheckBox("Player AA Circle"));
                _drawings.Add("eac", new CheckBox("Enemy AA Circle"));
                _drawings.Add("hoz", new CheckBox("Hold Zone"));
                _drawings.Add("liw", new Slider("Line Width", 1, 1, 6));



                /* Misc options */
                _misc = _orbwalkerMenu.AddSubMenu("Misc", "misc");
                _misc.Add("hol", new Slider("Hold Position Radius", 50, 0, 250));
                _misc.Add("far", new CheckBox("Prioritize farm over Harrass"));
                _misc.Add("war", new CheckBox("Auto Attack Wards", false));
                _misc.Add("pet", new CheckBox("Auto Attack Pets and Traps"));
                _misc.Add("jun", new CheckBox("JungleClear small first"));
                if (_player.ChampionName == "Kalista")
                {
                    _misc.Add("min", new CheckBox("Kalista - Combo Orbwalk with Minions"));
                    _misc.Add("exp", new CheckBox("Kalista - Use Passive Exploit", false));
                }

                /* Missile check */
                _orbwalkerMenu.Add("mis", new CheckBox("Use Missile Check"));

                /* Delay sliders */
                _orbwalkerMenu.Add("ext", new Slider("Extra Windup Time", 80, 0, 200));
                _orbwalkerMenu.Add("far", new Slider("Farm Delay", 30, 0, 200));
                /*Load the menu*/
                _orbwalkerMenu.Add("las", new KeyBind("Last Hit", false, KeyBind.BindTypes.HoldActive, 'X'));
                _orbwalkerMenu.Add("lan", new KeyBind("Lane Clear", false, KeyBind.BindTypes.HoldActive, 'V'));
                _orbwalkerMenu.Add("mix", new KeyBind("Mixed", false, KeyBind.BindTypes.HoldActive, 'X'));
                _orbwalkerMenu.Add("com", new KeyBind("Combo", false, KeyBind.BindTypes.HoldActive));
                Game.OnUpdate += GameOnOnGameUpdate;
                Drawing.OnDraw += DrawingOnOnDraw;
                Instances.Add(this);
            }

            /// <summary>
            /// Determines if a target is in auto attack range.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <returns><c>true</c> if a target is in auto attack range, <c>false</c> otherwise.</returns>
            public virtual bool InAutoAttackRange(AttackableUnit target)
            {
                return target.Distance(_player) < _player.GetAutoAttackRange();
            }


            public static bool PassiveExploit
            {
                get { return _player.ChampionName == "Kalista" && _misc["exp"].Cast<CheckBox>().CurrentValue; }
            }

            /// <summary>
            /// Gets the farm delay.
            /// </summary>
            /// <value>The farm delay.</value>
            private int FarmDelay
            {
                get { return _orbwalkerMenu["far"].Cast<Slider>().CurrentValue; }
            }

            /// <summary>
            /// Gets a value indicating whether the orbwalker is orbwalking by checking the missiles.
            /// </summary>
            /// <value><c>true</c> if the orbwalker is orbwalking by checking the missiles; otherwise, <c>false</c>.</value>
            public static bool MissileCheck
            {
                get { return _orbwalkerMenu["mis"].Cast<CheckBox>().CurrentValue; }
            }

            /*
            /// <summary>
            /// Registers the Custom Mode of the Orbwalker. Useful for adding a flee mode and such.
            /// </summary>
            /// <param name="name">The name of the mode in the menu. Ex. Flee</param>
            /// <param name="key">The default key for this mode.</param>
            public virtual void RegisterCustomMode(string name, uint key)
            {
                if (_config.Item($"{_orbwalkerName}.CustomMode") == null)
                {
                    _config.AddItem(
                        new MenuItem($"{_orbwalkerName}.CustomMode", name).SetShared().SetValue(new KeyBind(key, KeyBindType.Press)));
                }
            }*/

            public void Enabled(bool enabled)
            {
                _enabled = enabled;
            }

            /// <summary>
            /// Gets or sets the active mode.
            /// </summary>
            /// <value>The active mode.</value>
            public OrbwalkingMode ActiveMode
            {
                get
                {
                    if (_mode != OrbwalkingMode.None)
                    {
                        return _mode;
                    }

                    if (_orbwalkerMenu["com"].Cast<KeyBind>().CurrentValue)
                    {
                        return OrbwalkingMode.Combo;
                    }

                    if (_orbwalkerMenu["lan"].Cast<KeyBind>().CurrentValue)
                    {
                        return OrbwalkingMode.LaneClear;
                    }

                    if (_orbwalkerMenu["mix"].Cast<KeyBind>().CurrentValue)
                    {
                        return OrbwalkingMode.Mixed;
                    }

                    if (_orbwalkerMenu["las"].Cast<KeyBind>().CurrentValue)
                    {
                        return OrbwalkingMode.LastHit;
                    }

                    /*if (_config.Item($"{_orbwalkerName}.CustomMode") != null && _config.Item($"{_orbwalkerName}.CustomMode").GetValue<KeyBind>().Active)
                    {
                        return OrbwalkingMode.CustomMode;
                    }*/

                    return OrbwalkingMode.None;
                }
                set { _mode = value; }
            }

            /// <summary>
            /// Enables or disables the auto-attacks.
            /// </summary>
            /// <param name="b">if set to <c>true</c> the orbwalker will attack units.</param>
            public void SetAttack(bool b)
            {
                Attack = b;
            }

            /// <summary>
            /// Enables or disables the movement.
            /// </summary>
            /// <param name="b">if set to <c>true</c> the orbwalker will move.</param>
            public void SetMovement(bool b)
            {
                Move = b;
            }

            /// <summary>
            /// Forces the orbwalker to attack the set target if valid and in range.
            /// </summary>
            /// <param name="target">The target.</param>
            public void ForceTarget(Obj_AI_Base target)
            {
                _forcedTarget = target;
            }

            /// <summary>
            /// Forces the orbwalker to move to that point while orbwalking (Game.CursorPos by default).
            /// </summary>
            /// <param name="point">The point.</param>
            public void SetOrbwalkingPoint(Vector3 point)
            {
                _orbwalkingPoint = point;
            }

            /// <summary>
            /// Determines if the orbwalker should wait before attacking a minion.
            /// </summary>
            /// <returns><c>true</c> if the orbwalker should wait before attacking a minion, <c>false</c> otherwise.</returns>
            private bool ShouldWait()
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            minion =>
                                minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                                InAutoAttackRange(minion) && MinionManager.IsMinion(minion, false) &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int) ((_player.AttackDelay*1000)*LaneClearWaitTimeMod), FarmDelay) <=
                                _player.GetAutoAttackDamage(minion) - HeathDebuffer);
            }

            /// <summary>
            /// Gets the target.
            /// </summary>
            /// <returns>AttackableUnit.</returns>
            public virtual AttackableUnit GetTarget()
            {
                AttackableUnit result = null;

                if ((ActiveMode == OrbwalkingMode.Mixed || ActiveMode == OrbwalkingMode.LaneClear) &&
                    !_misc["far"].Cast<CheckBox>().CurrentValue)
                {
                    var target = TargetSelector.GetTarget(-1, DamageType.Physical);
                    if (target != null && InAutoAttackRange(target))
                    {
                        return target;
                    }
                }

                /*Killable Minion*/
                if (ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed ||
                    ActiveMode == OrbwalkingMode.LastHit ||
                    (_player.ChampionName == "Kalista" && _misc["min"].Cast<CheckBox>().CurrentValue &&
                     ActiveMode == OrbwalkingMode.Combo))
                {
                    var minionList =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                minion =>
                                    minion.IsValidTarget() && InAutoAttackRange(minion))
                            .OrderByDescending(minion => minion.BaseSkinName.Contains("Siege"))
                            .ThenBy(minion => minion.BaseSkinName.Contains("Super"))
                            .ThenBy(minion => minion.Health)
                            .ThenByDescending(minion => minion.MaxHealth);

                    foreach (var minion in minionList)
                    {
                        var t = (int) (_player.AttackCastDelay*1000) - 100 + Game.Ping/2 +
                                1000*(int) Math.Max(0, _player.Distance(minion) - _player.BoundingRadius)/
                                (int) GetMyProjectileSpeed();
                        var predHealth = HealthPrediction.GetHealthPrediction(minion, t, FarmDelay);

                        if (minion.Team != GameObjectTeam.Neutral && _misc["pet"].Cast<CheckBox>().CurrentValue &&
                            minion.BaseSkinName != "jarvanivstandard"
                            || MinionManager.IsMinion(minion, _misc["war"].Cast<CheckBox>().CurrentValue))
                        {
                            if (predHealth <= 0)
                            {
                                FireOnNonKillableMinion(minion);
                            }

                            if (predHealth > 0 && predHealth <= _player.GetAutoAttackDamage(minion, true) - HeathDebuffer)
                            {
                                return minion;
                            }
                        }
                    }
                }

                //Forced target
                if (_forcedTarget.IsValidTarget() && InAutoAttackRange(_forcedTarget))
                {
                    return _forcedTarget;
                }

                /* turrets / inhibitors / nexus */
                if (ActiveMode == OrbwalkingMode.LaneClear)
                {
                    /* turrets */
                    foreach (var turret in
                        ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* inhibitor */
                    foreach (var turret in
                        ObjectManager.Get<Obj_BarracksDampener>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* nexus */
                    foreach (var nexus in
                        ObjectManager.Get<Obj_HQ>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
                    {
                        return nexus;
                    }
                }

                /*Champions*/
                if (ActiveMode != OrbwalkingMode.LastHit)
                {
                    var target = TargetSelector.GetTarget(-1, DamageType.Physical);
                    if (target.IsValidTarget() && InAutoAttackRange(target))
                    {
                        return target;
                    }
                }

                /*Jungle minions*/
                if (ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed)
                {
                    var jminions =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                mob =>
                                    mob.IsValidTarget() && mob.Team == GameObjectTeam.Neutral &&
                                    this.InAutoAttackRange(mob)
                                    && mob.BaseSkinName != "gangplankbarrel");

                    if (_misc["jun"].Cast<CheckBox>().CurrentValue)
                    {
                        result = jminions
                            .OrderBy(m => m.MaxHealth)
                            .FirstOrDefault();
                    }
                    else
                    {
                        result = jminions
                            .OrderByDescending(m => m.MaxHealth)
                            .FirstOrDefault();
                    }

                    if (result != null)
                    {
                        return result;
                    }
                }

                /*Lane Clear minions*/
                if (ActiveMode == OrbwalkingMode.LaneClear ||
                    (_player.ChampionName == "Kalista" && _misc["min"].Cast<CheckBox>().CurrentValue &&
                     ActiveMode == OrbwalkingMode.Combo))
                {
                    if (!ShouldWait())
                    {
                        if (_prevMinion.IsValidTarget() && InAutoAttackRange(_prevMinion))
                        {
                            var predHealth = HealthPrediction.LaneClearHealthPrediction(
                                _prevMinion, (int) ((_player.AttackDelay*1000)*LaneClearWaitTimeMod), FarmDelay);
                            if (predHealth >= 2*_player.GetAutoAttackDamage(_prevMinion) - HeathDebuffer ||
                                Math.Abs(predHealth - _prevMinion.Health) < Single.Epsilon)
                            {
                                return _prevMinion;
                            }
                        }

                        result = (from minion in
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion) &&
                                                 (_misc["war"].Cast<CheckBox>().CurrentValue ||
                                                  !MinionManager.IsWard(minion.BaseSkinName.ToLower())) &&
                                                 (_misc["pet"].Cast<CheckBox>().CurrentValue &&
                                                  minion.BaseSkinName != "jarvanivstandard" ||
                                                  MinionManager.IsMinion(minion,
                                                      _misc["war"].Cast<CheckBox>().CurrentValue)) &&
                                                 minion.BaseSkinName != "gangplankbarrel")
                            let predHealth =
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int) ((_player.AttackDelay*1000)*LaneClearWaitTimeMod), FarmDelay)
                            where
                                predHealth >= 2*_player.GetAutoAttackDamage(minion) - HeathDebuffer ||
                                Math.Abs(predHealth - minion.Health + HeathDebuffer) < Single.Epsilon
                            select minion).OrderByDescending(
                                m => !MinionManager.IsMinion(m, true) ? Single.MaxValue : m.Health).FirstOrDefault();

                        if (result != null)
                        {
                            _prevMinion = (Obj_AI_Minion) result;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Fired when the game is updated.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
            private void GameOnOnGameUpdate(EventArgs args)
            { 
                if (_enabled)
                try
                {
                    if (ActiveMode == OrbwalkingMode.None)
                    {
                        return;
                    }

                    //Prevent canceling important spells
                    /*if (Player.IsCastingInterruptableSpell(true))
                    {
                        return;
                    }*/

                    var target = GetTarget();
                    Orbwalk(
                        target, (_orbwalkingPoint.To2D().IsValid()) ? _orbwalkingPoint : Game.CursorPos,
                        _orbwalkerMenu["ext"].Cast<Slider>().CurrentValue,
                        _misc["hol"].Cast<Slider>().CurrentValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }


            /// <summary>
            /// Fired when the game is drawn.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
            private void DrawingOnOnDraw(EventArgs args)
            {
                if (_drawings["pac"].Cast<CheckBox>().CurrentValue)
                {
                    new Circle
                    {
                        Color = Color.ForestGreen,
                        Radius = _player.GetAutoAttackRange(),
                        BorderWidth = _drawings["liw"].Cast<Slider>().CurrentValue
                    }.Draw(_player.Position);
                }

                if (_drawings["eac"].Cast<CheckBox>().CurrentValue)
                {
                    foreach (var target in
                        EntityManager.Heroes.Enemies.FindAll(target => target.IsValidTarget(1175)))
                    {
                        new Circle
                        {
                            Color = Color.Firebrick,
                            Radius = target.GetAutoAttackRange(),
                            BorderWidth = _drawings["liw"].Cast<Slider>().CurrentValue
                        }.Draw(target.Position);
                    }
                }

                if (_drawings["hoz"].Cast<CheckBox>().CurrentValue)
                {
                    new Circle
                    {
                        Color = Color.DodgerBlue,
                        Radius = _misc["hol"].Cast<Slider>().CurrentValue,
                        BorderWidth = _drawings["liw"].Cast<Slider>().CurrentValue
                    }.Draw(_player.Position);
                }

            }
        }
    }
}