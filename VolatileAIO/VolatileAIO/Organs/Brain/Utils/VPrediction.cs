using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using SharpDX;
using VolatileAIO.Organs.Brain.Test.TopSecret;

namespace VolatileAIO.Organs.Brain.Utils
{
    public enum HitChance
    {
        Immobile = 8,
        Dashing = 7,
        VeryHigh = 6,
        High = 5,
        Medium = 4,
        Low = 3,
        Impossible = 2,
        OutOfRange = 1,
        Collision = 0
    }

    public enum SkillshotType
    {
        SkillshotLine,
        SkillshotCircle,
        SkillshotCone
    }

    public enum CollisionableObjects
    {
        Minions,
        Heroes,
        YasuoWall,
        Walls
    }

    public class PredictionInput
    {
        private Vector3 _from;
        private Vector3 _rangeCheckFrom;

        /// <summary>
        ///     Set to true make the prediction hit as many enemy heroes as posible.
        /// </summary>
        public bool Aoe = false;

        /// <summary>
        ///     Set to true if the unit collides with units.
        /// </summary>
        public bool Collision = false;

        /// <summary>
        ///     Array that contains the unit types that the skillshot can collide with.
        /// </summary>
        public CollisionableObjects[] CollisionObjects =
        {
            CollisionableObjects.Minions, CollisionableObjects.YasuoWall
        };

        /// <summary>
        ///     The skillshot delay in seconds.
        /// </summary>
        public float Delay;

        /// <summary>
        ///     The skillshot width's radius or the angle in case of the cone skillshots.
        /// </summary>
        public float Radius = 1f;

        /// <summary>
        ///     The skillshot range in units.
        /// </summary>
        public float Range = float.MaxValue;

        /// <summary>
        ///     The skillshot speed in units per second.
        /// </summary>
        public float Speed = float.MaxValue;

        /// <summary>
        ///     The skillshot type.
        /// </summary>
        public SkillshotType Type = SkillshotType.SkillshotLine;

        /// <summary>
        ///     The unit that the prediction will made for.
        /// </summary>
        public Obj_AI_Base Unit = ObjectManager.Player;

        /// <summary>
        ///     Source unit for the prediction 
        /// </summary>
        public Obj_AI_Base Source = ObjectManager.Player;

        /// <summary>
        ///     Set to true to increase the prediction radius by the unit bounding radius.
        /// </summary>
        public bool UseBoundingRadius = true;

        /// <summary>
        ///     The position from where the skillshot missile gets fired.
        /// </summary>
        public Vector3 From
        {
            get { return _from.To2D().IsValid() ? _from : ObjectManager.Player.ServerPosition; }
            set { _from = value; }
        }

        /// <summary>
        ///     The position from where the range is checked.
        /// </summary>
        public Vector3 RangeCheckFrom
        {
            get
            {
                return _rangeCheckFrom.To2D().IsValid()
                    ? _rangeCheckFrom
                    : (From.To2D().IsValid() ? From : ObjectManager.Player.ServerPosition);
            }
            set { _rangeCheckFrom = value; }
        }

        internal float RealRadius
        {
            get { return UseBoundingRadius ? Radius + Unit.BoundingRadius / 2 : Radius; }
        }
    }

    public class PredictionOutput
    {
        internal int _aoeTargetsHitCount;
        private Vector3 _castPosition;
        private Vector3 _unitPosition;

        /// <summary>
        ///     The list of the targets that the spell will hit (only if aoe was enabled).
        /// </summary>
        public List<AIHeroClient> AoeTargetsHit = new List<AIHeroClient>();

        /// <summary>
        ///     The list of the units that the skillshot will collide with.
        /// </summary>
        public List<Obj_AI_Base> CollisionObjects = new List<Obj_AI_Base>();

        /// <summary>
        ///     Returns the hitchance.
        /// </summary>
        public HitChance Hitchance = HitChance.Impossible;

        internal PredictionInput Input;

        /// <summary>
        ///     The position where the skillshot should be casted to increase the accuracy.
        /// </summary>
        public Vector3 CastPosition
        {
            get
            {
                return _castPosition.IsValid() && _castPosition.To2D().IsValid()
                    ? _castPosition.SetZ()
                    : Input.Unit.ServerPosition;
            }
            set { _castPosition = value; }
        }

        /// <summary>
        ///     The number of targets the skillshot will hit (only if aoe was enabled).
        /// </summary>
        public int AoeTargetsHitCount
        {
            get { return Math.Max(_aoeTargetsHitCount, AoeTargetsHit.Count); }
        }

        /// <summary>
        ///     The position where the unit is going to be when the skillshot reaches his position.
        /// </summary>
        public Vector3 UnitPosition
        {
            get { return _unitPosition.To2D().IsValid() ? _unitPosition.SetZ() : Input.Unit.ServerPosition; }
            set { _unitPosition = value; }
        }
    }

    /// <summary>
    ///     Class used for calculating the position of the given unit after a delay.
    /// </summary>
    public static class VPrediction
    {
        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit,
            float delay,
            float radius,
            float speed,
            CollisionableObjects[] collisionable)
        {
            return
                GetPrediction(
                    new PredictionInput
                    {
                        Unit = unit,
                        Delay = delay,
                        Radius = radius,
                        Speed = speed,
                        CollisionObjects = collisionable
                    });
        }

        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return GetPrediction(input, true, true);
        }

        internal static PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = null;

            if (!input.Unit.IsValidTarget(float.MaxValue))
            {
                return new PredictionOutput();
            }

            if (ft)
            {
                //Increase the delay due to the latency and server tick:
                input.Delay += Game.Ping / 2000f + 0.07f;

                if (input.Aoe)
                {
                    return AoePrediction.GetPrediction(input);
                }
            }

            //Target too far away.
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon &&
                input.Unit.Distance(input.RangeCheckFrom, true) > Math.Pow(input.Range * 1.5, 2))
            {
                return new PredictionOutput { Input = input };
            }

            //Unit is dashing.
            if (input.Unit.IsDashing())
            {
                result = GetDashingPrediction(input);
            }
            else
            {
                //Unit is immobile.
                var remainingImmobileT = UnitIsImmobileUntil(input.Unit);
                if (remainingImmobileT >= 0d)
                {
                    result = GetImmobilePrediction(input, remainingImmobileT);
                }
            }

            //Normal prediction
            if (result == null)
            {
                result = GetStandardPrediction(input);
            }

            //Check if the unit position is in range
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.Hitchance >= HitChance.High &&
                    input.RangeCheckFrom.Distance(input.Unit.Position, true) >
                    Math.Pow(input.Range + input.RealRadius * 3 / 4, 2))
                {
                    result.Hitchance = HitChance.Medium;
                }

                if (input.RangeCheckFrom.Distance(result.UnitPosition, true) >
                    Math.Pow(input.Range + (input.Type == SkillshotType.SkillshotCircle ? input.RealRadius : 0), 2))
                {
                    result.Hitchance = HitChance.OutOfRange;
                }

                /* This does not need to be handled for the updated predictions, but left as a reference.*/
                if (input.RangeCheckFrom.Distance(result.CastPosition, true) > Math.Pow(input.Range, 2))
                {
                    if (result.Hitchance != HitChance.OutOfRange)
                    {
                        result.CastPosition = input.RangeCheckFrom +
                                              input.Range *
                                              (result.UnitPosition - input.RangeCheckFrom).To2D().Normalized().To3D();
                    }
                    else
                    {
                        result.Hitchance = HitChance.OutOfRange;
                    }
                }
            }

            //Set hit chance
            if (result.Hitchance == HitChance.High || result.Hitchance == HitChance.VeryHigh)
            {
                result = WayPointAnalysis(result, input);
                //.debug(input.Unit.BaseSkinName + result.Hitchance);
            }

            //Check for collision
            if (checkCollision && input.Collision && result.Hitchance > HitChance.Impossible)
            {
                var positions = new List<Vector3> { result.CastPosition, result.UnitPosition };

                result.CollisionObjects = Collision.GetCollision(positions, input);
                result.Hitchance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.Hitchance;
            }


            return result;
        }

        internal static PredictionOutput WayPointAnalysis(PredictionOutput result, PredictionInput input)
        {

            if (!input.Unit.IsValid() || !(input.Unit is AIHeroClient) || Math.Abs(input.Radius - 1) < 0.01)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            //Program.debug("PRED: FOR CHAMPION " + input.Unit.BaseSkinName);

            // CAN'T MOVE SPELLS ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.GetSpecialSpellEndTime(input.Unit) > 0 || input.Unit.HasBuff("Recall"))
            {

                result.Hitchance = HitChance.VeryHigh;
                return result;

            }

            // PREPARE MATH ///////////////////////////////////////////////////////////////////////////////////

            result.Hitchance = HitChance.Medium;

            var lastWaypiont = input.Unit.GetWaypoints().Last().To3D();
            var distanceUnitToWaypoint = lastWaypiont.Distance(input.Unit.ServerPosition);
            var distanceFromToUnit = input.From.Distance(input.Unit.ServerPosition);
            var distanceFromToWaypoint = lastWaypiont.Distance(input.From);

            float speedDelay;

            if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
                speedDelay = 0;
            else
                speedDelay = distanceFromToUnit / input.Speed;

            float totalDelay = speedDelay + input.Delay;
            float moveArea = input.Unit.MoveSpeed * totalDelay;
            float fixRange = moveArea * 0.5f;
            double angleMove = 30 + (input.Radius / 13) - (totalDelay * 2);
            float backToFront = moveArea * 1.5f;
            float pathMinLen = 1000f;

            if (UnitTracker.GetLastNewPathTime(input.Unit) < 0.1d)
            {
                pathMinLen = 700f + backToFront;
                result.Hitchance = HitChance.High;
            }

            if (input.Type == SkillshotType.SkillshotCircle)
            {
                fixRange -= input.Radius / 2;
            }

            // SPAM CLICK ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.PathCalc(input.Unit))
            {
                //Program.debug("PRED: SPAM CLICK");
                if (distanceFromToUnit < input.Range - fixRange)
                {
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }

                result.Hitchance = HitChance.High;
                return result;
            }

            // NEW Visible ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.GetLastVisibleTime(input.Unit) < 0.08d)
            {
                //Program.debug("PRED: NEW Visible");
                result.Hitchance = HitChance.Medium;
                return result;
            }

            // SPECIAL CASES ///////////////////////////////////////////////////////////////////////////////////

            if (distanceFromToUnit < 300 || distanceFromToWaypoint < 200)
            {
                //Program.debug("PRED: SPECIAL CASES");
                result.Hitchance = HitChance.VeryHigh;
                return result;

            }

            // LONG CLICK DETECTION ///////////////////////////////////////////////////////////////////////////////////

            if (distanceUnitToWaypoint > pathMinLen)
            {
                //Program.debug("PRED: LONG CLICK DETECTION");
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // RUN IN LANE DETECTION ///////////////////////////////////////////////////////////////////////////////////

            if (distanceFromToWaypoint > distanceFromToUnit + fixRange && GetAngle(input.From, input.Unit) < angleMove)
            {
                //Program.debug(GetAngle(input.From, input.Unit) + " PRED: RUN IN LANE DETECTION " + angleMove);
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }

            // FIX RANGE ///////////////////////////////////////////////////////////////////////////////////

            if (distanceFromToWaypoint <= input.Unit.Distance(input.From) && distanceFromToUnit > input.Range - fixRange)
            {
                //debug("PRED: FIX RANGE");
                result.Hitchance = HitChance.Medium;
                return result;
            }

            // AUTO ATTACK LOGIC ///////////////////////////////////////////////////////////////////////////////////

            if (UnitTracker.GetLastAutoAttackTime(input.Unit) < 0.1d)
            {
                if (input.Type == SkillshotType.SkillshotLine && totalDelay < 0.6 + (input.Radius * 0.001))
                    result.Hitchance = HitChance.VeryHigh;
                else if (input.Type == SkillshotType.SkillshotCircle && totalDelay < 0.7 + (input.Radius * 0.001))
                    result.Hitchance = HitChance.VeryHigh;
                else
                    result.Hitchance = HitChance.High;

                //Program.debug("PRED: AUTO ATTACK DETECTION");
                return result;
            }

            // STOP LOGIC ///////////////////////////////////////////////////////////////////////////////////

            else
            {
                if (input.Unit.IsAttackingPlayer)
                {
                    result.Hitchance = HitChance.High;
                    return result;
                }
                else if (input.Unit.Path.Count() == 0 && !input.Unit.IsMoving)
                {
                    if (distanceFromToUnit > input.Range - fixRange)
                        result.Hitchance = HitChance.Medium;
                    else if (UnitTracker.GetLastStopMoveTime(input.Unit) < 0.8d)
                        result.Hitchance = HitChance.High;
                    else
                        result.Hitchance = HitChance.VeryHigh;

                    //Program.debug("PRED: STOP LOGIC");
                    return result;
                }
            }

            // ANGLE HIT CHANCE ///////////////////////////////////////////////////////////////////////////////////

            if (input.Type == SkillshotType.SkillshotLine && input.Unit.Path.Count() > 0 && input.Unit.IsMoving)
            {
                if (GetAngle(input.From, input.Unit) < angleMove)
                {
                    //Program.debug("PRED: ANGLE HIT CHANCE");
                    result.Hitchance = HitChance.VeryHigh;
                    return result;

                }
            }

            // CIRCLE NEW PATH ///////////////////////////////////////////////////////////////////////////////////

            if (input.Type == SkillshotType.SkillshotCircle)
            {
                if (UnitTracker.GetLastNewPathTime(input.Unit) < 0.1d && distanceFromToUnit < input.Range - fixRange && distanceUnitToWaypoint > fixRange)
                {
                    //Program.debug("PRED: CIRCLE NEW PATH");
                    result.Hitchance = HitChance.VeryHigh;
                    return result;
                }
            }
            //Program.debug("PRED: NO DETECTION");

            return result;
        }

        internal static PredictionOutput GetDashingPrediction(PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            //Normal dashes.
            if (!dashData.IsBlink)
            {
                //Mid air:
                var endP = dashData.Path.Last();
                var dashPred = GetPositionOnPath(
                    input, new List<Vector2> { input.Unit.ServerPosition.To2D(), endP }, dashData.Speed);
                if (dashPred.Hitchance >= HitChance.High && dashPred.UnitPosition.To2D().Distance(input.Unit.Position.To2D(), endP, true) < 200)
                {
                    dashPred.CastPosition = dashPred.UnitPosition;
                    dashPred.Hitchance = HitChance.Dashing;
                    return dashPred;
                }

                //At the end of the dash:
                if (dashData.Path.GetPathLength() > 200)
                {
                    var timeToPoint = input.Delay / 2f + input.From.To2D().Distance(endP) / input.Speed - 0.25f;
                    if (timeToPoint <=
                        input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        return new PredictionOutput
                        {
                            CastPosition = endP.To3D(),
                            UnitPosition = endP.To3D(),
                            Hitchance = HitChance.Dashing
                        };
                    }
                }
                result.CastPosition = dashData.Path.Last().To3D();
                result.UnitPosition = result.CastPosition;

                //Figure out where the unit is going.
            }

            return result;
        }

        internal static PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;

            if (timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                return new PredictionOutput
                {
                    CastPosition = input.Unit.ServerPosition,
                    UnitPosition = input.Unit.Position,
                    Hitchance = HitChance.Immobile
                };
            }

            return new PredictionOutput
            {
                Input = input,
                CastPosition = input.Unit.ServerPosition,
                UnitPosition = input.Unit.ServerPosition,
                Hitchance = HitChance.High
                /*timeToReachTargetPosition - remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed < 0.4d ? HitChance.High : HitChance.Medium*/
            };
        }

        internal static PredictionOutput GetStandardPrediction(PredictionInput input)
        {
            var speed = input.Unit.MoveSpeed;

            if (input.Unit.Distance(input.From, true) < 200 * 200)
            {
                //input.Delay /= 2;
                speed /= 1.5f;
            }

            if (input.Unit.IsValid() && input.Unit is AIHeroClient && UnitTracker.PathCalc(input.Unit))
            {

                return GetPositionOnPath(input, UnitTracker.GetPathWayCalc(input.Unit), speed);

            }
            else
                return GetPositionOnPath(input, input.Unit.GetWaypoints(), speed);
        }

        internal static double GetAngle(Vector3 from, Obj_AI_Base target)
        {
            var c = target.ServerPosition.To2D();
            var a = target.GetWaypoints().Last();

            if (c == a)
                return 60;

            var b = from.To2D();

            var ab = Math.Pow(a.X - (double)b.X, 2) + Math.Pow(a.Y - (double)b.Y, 2);
            var bc = Math.Pow(b.X - (double)c.X, 2) + Math.Pow(b.Y - (double)c.Y, 2);
            var ac = Math.Pow(a.X - (double)c.X, 2) + Math.Pow(a.Y - (double)c.Y, 2);

            return Math.Cos((ab + bc - ac) / (2 * Math.Sqrt(ab) * Math.Sqrt(bc))) * 180 / Math.PI;
        }

        internal static double UnitIsImmobileUntil(Obj_AI_Base unit)
        {
            var result =
                unit.Buffs.Where(
                    buff =>
                        buff.IsActive && Game.Time <= buff.EndTime &&
                        (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup || buff.Type == BuffType.Stun ||
                         buff.Type == BuffType.Suppression || buff.Type == BuffType.Snare || buff.Type == BuffType.Fear
                         || buff.Type == BuffType.Taunt || buff.Type == BuffType.Knockback))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return (result - Game.Time);
        }

        internal static PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
        {
            speed = (Math.Abs(speed - (-1)) < float.Epsilon) ? input.Unit.MoveSpeed : speed;

            if (path.Count <= 1)
            {
                return new PredictionOutput
                {
                    Input = input,
                    UnitPosition = input.Unit.ServerPosition,
                    CastPosition = input.Unit.ServerPosition,
                    Hitchance = HitChance.VeryHigh
                };
            }

            var pLength = path.GetPathLength();

            //Skillshots with only a delay
            if (pLength >= input.Delay * speed - input.RealRadius && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var tDistance = input.Delay * speed - input.RealRadius;

                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.Distance(b);

                    if (d >= tDistance)
                    {
                        var direction = (b - a).Normalized();

                        var cp = a + direction * tDistance;
                        var p = a +
                                direction *
                                ((i == path.Count - 2)
                                    ? Math.Min(tDistance + input.RealRadius, d)
                                    : (tDistance + input.RealRadius));

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = cp.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance = HitChance.High
                        };
                    }

                    tDistance -= d;
                }
            }

            //Skillshot with a delay and speed.
            if (pLength >= input.Delay * speed - input.RealRadius &&
                Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = input.Delay * speed - input.RealRadius;
                if (input.Type == SkillshotType.SkillshotLine || input.Type == SkillshotType.SkillshotCone)
                {
                    if (input.From.Distance(input.Unit.ServerPosition, true) < 200 * 200)
                    {
                        d = input.Delay * speed;
                    }
                }

                path = path.CutPath(d);
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.Distance(b) / speed;
                    var direction = (b - a).Normalized();
                    a = a - speed * tT * direction;
                    var sol = Geometry.VectorMovementCollision(a, b, speed, input.From.To2D(), input.Speed, tT);
                    var t = (float)sol[0];
                    var pos = (Vector2)sol[1];

                    if (pos.IsValid() && t >= tT && t <= tT + tB)
                    {
                        if (pos.Distance(b, true) < 20)
                            break;
                        var p = pos + input.RealRadius * direction;

                        if (input.Type == SkillshotType.SkillshotLine)
                        {
                            var alpha = (input.From.To2D() - p).AngleBetween(a - b);
                            if (alpha > 30 && alpha < 180 - 30)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / p.Distance(input.From));
                                var cp1 = input.From.To2D() + (p - input.From.To2D()).Rotated(beta);
                                var cp2 = input.From.To2D() + (p - input.From.To2D()).Rotated(-beta);

                                pos = cp1.Distance(pos, true) < cp2.Distance(pos, true) ? cp1 : cp2;
                            }
                        }

                        return new PredictionOutput
                        {
                            Input = input,
                            CastPosition = pos.To3D(),
                            UnitPosition = p.To3D(),
                            Hitchance = HitChance.High
                        };
                    }
                    tT += tB;
                }
            }

            var position = path.Last();
            return new PredictionOutput
            {
                Input = input,
                CastPosition = position.To3D(),
                UnitPosition = position.To3D(),
                Hitchance = HitChance.Medium
            };
        }


    }

    internal static class AoePrediction
    {
        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            switch (input.Type)
            {
                case SkillshotType.SkillshotCircle:
                    return Circle.GetPrediction(input);
                case SkillshotType.SkillshotCone:
                    return Cone.GetPrediction(input);
                case SkillshotType.SkillshotLine:
                    return Line.GetPrediction(input);
            }
            return new PredictionOutput();
        }

        internal static List<PossibleTarget> GetPossibleTargets(PredictionInput input)
        {
            var result = new List<PossibleTarget>();
            var originalUnit = input.Unit;
            foreach (var enemy in
                EntityManager.Heroes.Enemies.FindAll(
                    h =>
                        h.NetworkId != originalUnit.NetworkId &&
                        h.IsValidTarget() &&
                        h.Distance(input.RangeCheckFrom) <= (input.Range + 200 + input.RealRadius)))
            {
                input.Unit = enemy;
                var prediction = VPrediction.GetPrediction(input, false, false);
                if (prediction.Hitchance >= HitChance.High)
                {
                    result.Add(new PossibleTarget { Position = prediction.UnitPosition.To2D(), Unit = enemy });
                }
            }
            return result;
        }

        public static class Circle
        {
            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = VPrediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                while (posibleTargets.Count > 1)
                {
                    var mecCircle = MEC.GetMec(posibleTargets.Select(h => h.Position).ToList());

                    if (mecCircle.Radius <= input.RealRadius - 10 &&
                        Vector2.DistanceSquared(mecCircle.Center, input.RangeCheckFrom.To2D()) <
                        input.Range * input.Range)
                    {
                        return new PredictionOutput
                        {
                            AoeTargetsHit = posibleTargets.Select(h => (AIHeroClient)h.Unit).ToList(),
                            CastPosition = mecCircle.Center.To3D(),
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            Hitchance = mainTargetPrediction.Hitchance,
                            Input = input,
                            _aoeTargetsHitCount = posibleTargets.Count
                        };
                    }

                    float maxdist = -1;
                    var maxdistindex = 1;
                    for (var i = 1; i < posibleTargets.Count; i++)
                    {
                        var distance = Vector2.DistanceSquared(posibleTargets[i].Position, posibleTargets[0].Position);
                        if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                        {
                            maxdistindex = i;
                            maxdist = distance;
                        }
                    }
                    posibleTargets.RemoveAt(maxdistindex);
                }

                return mainTargetPrediction;
            }
        }

        public static class Cone
        {
            internal static int GetHits(Vector2 end, double range, float angle, List<Vector2> points)
            {
                return (from point in points
                        let edge1 = end.Rotated(-angle / 2)
                        let edge2 = edge1.Rotated(angle)
                        where
                            point.Distance(new Vector2(), true) < range * range && edge1.CrossProduct(point) > 0 &&
                            point.CrossProduct(edge2) > 0
                        select point).Count();
            }

            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = VPrediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };

                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();

                    foreach (var target in posibleTargets)
                    {
                        target.Position = target.Position - input.From.To2D();
                    }

                    for (var i = 0; i < posibleTargets.Count; i++)
                    {
                        for (var j = 0; j < posibleTargets.Count; j++)
                        {
                            if (i != j)
                            {
                                var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
                                if (!candidates.Contains(p))
                                {
                                    candidates.Add(p);
                                }
                            }
                        }
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        var hits = GetHits(candidate, input.Range, input.Radius, positionsList);
                        if (hits > bestCandidateHits)
                        {
                            bestCandidate = candidate;
                            bestCandidateHits = hits;
                        }
                    }

                    bestCandidate = bestCandidate + input.From.To2D();

                    if (bestCandidateHits > 1 && input.From.To2D().Distance(bestCandidate, true) > 50 * 50)
                    {
                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = bestCandidate.To3D(),
                            Input = input
                        };
                    }
                }
                return mainTargetPrediction;
            }
        }

        public static class Line
        {
            internal static IEnumerable<Vector2> GetHits(Vector2 start, Vector2 end, double radius, List<Vector2> points)
            {
                return points.Where(p => p.Distance(start, end, true, true) <= radius * radius);
            }

            internal static Vector2[] GetCandidates(Vector2 from, Vector2 to, float radius, float range)
            {
                var middlePoint = (from + to) / 2;
                var intersections = from.CircleCircleIntersection(
                    middlePoint, radius, from.Distance(middlePoint));

                if (intersections.Length > 1)
                {
                    var c1 = intersections[0];
                    var c2 = intersections[1];

                    c1 = from + range * (to - c1).Normalized();
                    c2 = from + range * (to - c2).Normalized();

                    return new[] { c1, c2 };
                }

                return new Vector2[] { };
            }

            public static PredictionOutput GetPrediction(PredictionInput input)
            {
                var mainTargetPrediction = VPrediction.GetPrediction(input, false, true);
                var posibleTargets = new List<PossibleTarget>
                {
                    new PossibleTarget { Position = mainTargetPrediction.UnitPosition.To2D(), Unit = input.Unit }
                };
                if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                {
                    //Add the posible targets  in range:
                    posibleTargets.AddRange(GetPossibleTargets(input));
                }

                if (posibleTargets.Count > 1)
                {
                    var candidates = new List<Vector2>();
                    foreach (var target in posibleTargets)
                    {
                        var targetCandidates = GetCandidates(
                            input.From.To2D(), target.Position, (input.Radius), input.Range);
                        candidates.AddRange(targetCandidates);
                    }

                    var bestCandidateHits = -1;
                    var bestCandidate = new Vector2();
                    var bestCandidateHitPoints = new List<Vector2>();
                    var positionsList = posibleTargets.Select(t => t.Position).ToList();

                    foreach (var candidate in candidates)
                    {
                        if (
                            GetHits(
                                input.From.To2D(), candidate, (input.Radius + input.Unit.BoundingRadius / 3 - 10),
                                new List<Vector2> { posibleTargets[0].Position }).Count() == 1)
                        {
                            var hits = GetHits(input.From.To2D(), candidate, input.Radius, positionsList).ToList();
                            var hitsCount = hits.Count;
                            if (hitsCount >= bestCandidateHits)
                            {
                                bestCandidateHits = hitsCount;
                                bestCandidate = candidate;
                                bestCandidateHitPoints = hits.ToList();
                            }
                        }
                    }

                    if (bestCandidateHits > 1)
                    {
                        float maxDistance = -1;
                        Vector2 p1 = new Vector2(), p2 = new Vector2();

                        //Center the position
                        for (var i = 0; i < bestCandidateHitPoints.Count; i++)
                        {
                            for (var j = 0; j < bestCandidateHitPoints.Count; j++)
                            {
                                var startP = input.From.To2D();
                                var endP = bestCandidate;
                                var proj1 = positionsList[i].ProjectOn(startP, endP);
                                var proj2 = positionsList[j].ProjectOn(startP, endP);
                                var dist = Vector2.DistanceSquared(bestCandidateHitPoints[i], proj1.LinePoint) +
                                           Vector2.DistanceSquared(bestCandidateHitPoints[j], proj2.LinePoint);
                                if (dist >= maxDistance &&
                                    (proj1.LinePoint - positionsList[i]).AngleBetween(
                                        proj2.LinePoint - positionsList[j]) > 90)
                                {
                                    maxDistance = dist;
                                    p1 = positionsList[i];
                                    p2 = positionsList[j];
                                }
                            }
                        }

                        return new PredictionOutput
                        {
                            Hitchance = mainTargetPrediction.Hitchance,
                            _aoeTargetsHitCount = bestCandidateHits,
                            UnitPosition = mainTargetPrediction.UnitPosition,
                            CastPosition = ((p1 + p2) * 0.5f).To3D(),
                            Input = input
                        };
                    }
                }

                return mainTargetPrediction;
            }
        }

        internal class PossibleTarget
        {
            public Vector2 Position;
            public Obj_AI_Base Unit;
        }
    }

    public static class Collision
    {
        private static int _wallCastT;
        private static Vector2 _yasuoWallCastedPos;

        static Collision()
        {
            Obj_AI_Base.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
        }

        private static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsValid && sender.Team != ObjectManager.Player.Team && args.SData.Name == "YasuoWMovingWall")
            {
                _wallCastT = (int) (Game.Time*1000);
                _yasuoWallCastedPos = sender.ServerPosition.To2D();
            }
        }

        /// <summary>
        ///     Returns the list of the units that the skillshot will hit before reaching the set positions.
        /// </summary>
        public static List<Obj_AI_Base> GetCollision(List<Vector3> positions, PredictionInput input)
        {
            var result = new List<Obj_AI_Base>();
            foreach (var position in positions)
            {
                foreach (var objectType in input.CollisionObjects)
                {
                    switch (objectType)
                    {
                        case CollisionableObjects.Minions:
                            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion =>
                                            minion.IsValidTarget() &&
                                            minion.Distance(input.From) <= Math.Min(input.Range + input.Radius + 100, 2000)))
                            {
                                input.Unit = minion;
                                if (minion.Path.Count() > 0)
                                {
                                    var minionPrediction = VPrediction.GetPrediction(input, true, false);

                                    if (minionPrediction.CastPosition.To2D().Distance(input.From.To2D(), position.To2D(), true, true) <= Math.Pow((input.Radius + 20 + minion.Path.Count() * minion.BoundingRadius), 2))
                                    {
                                        result.Add(minion);
                                    }
                                }
                                else
                                {
                                    var bonus = 30;
                                    if (minion.ServerPosition.To2D().Distance(input.From.To2D()) < input.Radius)
                                        result.Add(minion);
                                    else if (minion.ServerPosition.To2D().Distance(input.From.To2D(), position.To2D(), true, true) <=
                                        Math.Pow((input.Radius + bonus + minion.BoundingRadius), 2))
                                    {
                                        result.Add(minion);
                                    }
                                }
                            }
                            break;
                        case CollisionableObjects.Heroes:
                            foreach (var hero in
                                EntityManager.Heroes.Enemies.FindAll(
                                    hero =>
                                        hero.IsValidTarget() &&
                                        hero.Distance(input.RangeCheckFrom) <= Math.Min(input.Range + input.Radius + 100, 2000))
                                )
                            {
                                input.Unit = hero;
                                var prediction = VPrediction.GetPrediction(input, false, false);
                                if (
                                    prediction.UnitPosition.To2D()
                                        .Distance(input.From.To2D(), position.To2D(), true, true) <=
                                    Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2))
                                {
                                    result.Add(hero);
                                }
                            }
                            break;

                        case CollisionableObjects.Walls:
                            var step = position.Distance(input.From) / 20;
                            for (var i = 0; i < 20; i++)
                            {
                                var p = input.From.To2D().Extend(position.To2D(), step * i);
                                if (NavMesh.GetCollisionFlags(p.X, p.Y).HasFlag(CollisionFlags.Wall))
                                {
                                    result.Add(ObjectManager.Player);
                                }
                            }
                            break;
                    }
                }
            }
            return result.Distinct().ToList();
        }
    }

    internal class PathInfo
    {
        public Vector2 Position { get; set; }
        public float Time { get; set; }
    }

    internal class Spells
    {
        public string Name { get; set; }
        public double Duration { get; set; }
    }

    internal class UnitTrackerInfo
    {
        public int NetworkId { get; set; }
        public int AaTick { get; set; }
        public int NewPathTick { get; set; }
        public int StopMoveTick { get; set; }
        public int LastInVisibleTick { get; set; }
        public int SpecialSpellFinishTick { get; set; }
        public List<PathInfo> PathBank = new List<PathInfo>();
    }

    internal static class UnitTracker
    {
        public static List<UnitTrackerInfo> UnitTrackerInfoList = new List<UnitTrackerInfo>();
        private static List<AIHeroClient> _champion = new List<AIHeroClient>();
        private static List<Spells> _spells = new List<Spells>();
        private static List<PathInfo> _pathBank = new List<PathInfo>();
        static UnitTracker()
        {
            _spells.Add(new Spells() { Name = "katarinar", Duration = 1 }); //Katarinas R
            _spells.Add(new Spells() { Name = "drain", Duration = 1 }); //Fiddle W
            _spells.Add(new Spells() { Name = "crowstorm", Duration = 1 }); //Fiddle R
            _spells.Add(new Spells() { Name = "consume", Duration = 0.5 }); //Nunu Q
            _spells.Add(new Spells() { Name = "absolutezero", Duration = 1 }); //Nunu R
            _spells.Add(new Spells() { Name = "staticfield", Duration = 0.5 }); //Blitzcrank R
            _spells.Add(new Spells() { Name = "cassiopeiapetrifyinggaze", Duration = 0.5 }); //Cassio's R
            _spells.Add(new Spells() { Name = "ezrealtrueshotbarrage", Duration = 1 }); //Ezreal's R
            _spells.Add(new Spells() { Name = "galioidolofdurand", Duration = 1 }); //Ezreal's R                                                                   
            _spells.Add(new Spells() { Name = "luxmalicecannon", Duration = 1 }); //Lux R
            _spells.Add(new Spells() { Name = "reapthewhirlwind", Duration = 1 }); //Jannas R
            _spells.Add(new Spells() { Name = "jinxw", Duration = 0.6 }); //jinxW
            _spells.Add(new Spells() { Name = "jinxr", Duration = 0.6 }); //jinxR
            _spells.Add(new Spells() { Name = "missfortunebullettime", Duration = 1 }); //MissFortuneR
            _spells.Add(new Spells() { Name = "shenstandunited", Duration = 1 }); //ShenR
            _spells.Add(new Spells() { Name = "threshe", Duration = 0.4 }); //ThreshE
            _spells.Add(new Spells() { Name = "threshrpenta", Duration = 0.75 }); //ThreshR
            _spells.Add(new Spells() { Name = "threshq", Duration = 0.75 }); //ThreshQ
            _spells.Add(new Spells() { Name = "infiniteduress", Duration = 1 }); //Warwick R
            _spells.Add(new Spells() { Name = "meditate", Duration = 1 }); //yi W
            _spells.Add(new Spells() { Name = "alzaharnethergrasp", Duration = 1 }); //Malza R
            _spells.Add(new Spells() { Name = "lucianq", Duration = 0.5 }); //Lucian Q
            _spells.Add(new Spells() { Name = "caitlynpiltoverpeacemaker", Duration = 0.5 }); //Caitlyn Q
            _spells.Add(new Spells() { Name = "velkozr", Duration = 0.5 }); //Velkoz R 

            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                _champion.Add(hero);
                UnitTrackerInfoList.Add(new UnitTrackerInfo() { NetworkId = hero.NetworkId, AaTick = (int) (Game.Time*1000), StopMoveTick = (int)(Game.Time * 1000), NewPathTick = (int)(Game.Time * 1000), SpecialSpellFinishTick = (int)(Game.Time * 1000), LastInVisibleTick = (int)(Game.Time * 1000) });
            }
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnNewPath += AIHeroClient_OnNewPath;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            foreach (var hero in _champion)
            {
                if (hero.IsVisible)
                {
                    if (hero.Path.Count() > 0)
                        UnitTrackerInfoList.Find(x => x.NetworkId == hero.NetworkId).StopMoveTick = (int)(Game.Time * 1000);
                }
                else
                {
                    UnitTrackerInfoList.Find(x => x.NetworkId == hero.NetworkId).LastInVisibleTick = (int)(Game.Time * 1000);
                }
            }
        }

        private static void AIHeroClient_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (sender.IsMinion || !(sender is AIHeroClient)) return;

            var info = UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId);
            info.NewPathTick = (int)(Game.Time * 1000);
            if (args.Path.Last() != sender.ServerPosition)
                info.PathBank.Add(new PathInfo() { Position = args.Path.Last().To2D(), Time = Game.Time });

            if (info.PathBank.Count > 3)
                info.PathBank.Remove(info.PathBank.First());
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMinion || !sender.IsValid() || !(sender is AIHeroClient)) return;

            if (args.SData.IsAutoAttack())
                UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).AaTick = (int)(Game.Time * 1000);
            else
            {
                var foundSpell = _spells.Find(x => args.SData.Name.ToLower() == x.Name.ToLower());
                if (foundSpell != null)
                {
                    UnitTrackerInfoList.Find(x => x.NetworkId == sender.NetworkId).SpecialSpellFinishTick = (int)(Game.Time * 1000) + (int)(foundSpell.Duration * 1000);
                }
            }
        }

        public static bool PathCalc(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            if (trackerUnit.PathBank.Count < 3)
                return false;

            if (trackerUnit.PathBank[2].Time - trackerUnit.PathBank[0].Time < 0.40f && trackerUnit.PathBank[2].Time + 0.1f < Game.Time && trackerUnit.PathBank[2].Time + 0.2f > Game.Time && trackerUnit.PathBank[1].Position.Distance(trackerUnit.PathBank[2].Position) > unit.Distance(trackerUnit.PathBank[2].Position))
            {
                return true;
            }
            else
                return false;
        }

        public static List<Vector2> GetPathWayCalc(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            Vector2 sr;
            sr.X = (trackerUnit.PathBank[0].Position.X + trackerUnit.PathBank[1].Position.X + trackerUnit.PathBank[2].Position.X) / 3;
            sr.Y = (trackerUnit.PathBank[0].Position.Y + trackerUnit.PathBank[1].Position.Y + trackerUnit.PathBank[2].Position.Y) / 3;
            List<Vector2> points = new List<Vector2> {sr};
            return points;
        }

        public static double GetSpecialSpellEndTime(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            return (trackerUnit.SpecialSpellFinishTick - (int)(Game.Time * 1000)) / 1000d;
        }

        public static double GetLastAutoAttackTime(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            return ((int)(Game.Time * 1000) - trackerUnit.AaTick) / 1000d;
        }

        public static double GetLastNewPathTime(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);
            return ((int)(Game.Time * 1000) - trackerUnit.NewPathTick) / 1000d;
        }

        public static double GetLastVisibleTime(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

            return ((int)(Game.Time * 1000) - trackerUnit.LastInVisibleTick) / 1000d;
        }

        public static double GetLastStopMoveTime(Obj_AI_Base unit)
        {
            var trackerUnit = UnitTrackerInfoList.Find(x => x.NetworkId == unit.NetworkId);

            return ((int)(Game.Time * 1000) - trackerUnit.StopMoveTick) / 1000d;
        }
    }
}