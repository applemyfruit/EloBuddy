using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;

namespace VolatileAIO.Extensions.Mid
{
    internal class Annie : Heart
    {
        private static Spell.Active E;
        private static Spell.Targeted Q;
        private static Spell.Skillshot W, R;
        private static SpellDataInst _flash;
        private static bool _avoidSpam;
        public static List<Obj_AI_Turret> Turrets = new List<Obj_AI_Turret>();
        public static Menu SpellMenu;

        public Annie()
        {
            InitializeMenu();
            InitializeSpells();
        }

        private static GameObject TibbersObject { get; set; }

        private static Vector3 MousePos
        {
            get { return Game.CursorPos; }
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Combo Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("pilot", new CheckBox("Auto Pilot Tibbers"));
            SpellMenu.AddSeparator();
            SpellMenu.Add("flashr", new KeyBind("Flash R (Target Priority)", false, KeyBind.BindTypes.HoldActive, 'T'));
            SpellMenu.Add("flashrhap",
                new KeyBind("Flash R (Highest Amount Priority)", false, KeyBind.BindTypes.HoldActive, 'Z'));
            SpellMenu.Add("framount", new Slider("Minimum enemies hit to Flash R (HAP)", 2, 1, 5));

            SpellMenu.AddGroupLabel("Other Settings");
            SpellMenu.Add("LHQ", new CheckBox("Last Hit with Q"));
            SpellMenu.Add("autoe", new CheckBox("Auto E", false));
            SpellMenu.Add("autostack", new CheckBox("Auto Stack", false));
            SpellMenu.Add("estack", new CheckBox("Use E to Stack"));
            SpellMenu.Add("wstack", new CheckBox("Use W to Stack"));
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Targeted, Initialize.Type.Skillshot,
                Initialize.Type.Active, Initialize.Type.Skillshot);
            Q = (Spell.Targeted) PlayerData.Spells[0];
            W = (Spell.Skillshot) PlayerData.Spells[1];
            E = (Spell.Active) PlayerData.Spells[2];
            R = (Spell.Skillshot) PlayerData.Spells[3];
            _flash = EloBuddy.Player.Spells.FirstOrDefault(f => f.Name.ToLower() == "summonerflash");
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            FlashR();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
            MoveTibbers();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Flee) Flee();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit) LastHitB();
            if (SpellMenu["autostack"].Cast<CheckBox>().CurrentValue) Pyromania();
        }

        private static void FlashR()
        {
            if (SpellMenu["flashr"].Cast<KeyBind>().CurrentValue)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var target = TargetManager.Target(1025, DamageType.Magical);
                if (target == null || !target.IsValidTarget(1025) || !TickManager.NoLag(4) ||
                    !R.IsReady() || _flash == null)
                    return;
                var champs =
                    EntityManager.Heroes.Enemies.Where(e => e.Distance(target) < R.Radius)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                        .ToList();
                var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 1025);
                if (location.Position.Distance(Player.Position) > 600)
                {
                    Player.Spellbook.CastSpell(_flash.Slot, Player.Position.Extend(location.Position.To3D(), 450).To3D());
                }
                R.Cast(location.Position.To3D());
            }
            else if (SpellMenu["flashrhap"].Cast<KeyBind>().CurrentValue)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if (EntityManager.Heroes.Enemies.Count(e => e.Distance(Player) < 1025) <
                    SpellMenu["framount"].Cast<Slider>().CurrentValue || !TickManager.NoLag(4) ||
                    !R.IsReady() || _flash == null)
                    return;
                var champs =
                    EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < 1025)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                        .ToList();
                var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 1025);
                if (location.ChampsHit < SpellMenu["framount"].Cast<Slider>().CurrentValue) return;
                if (location.Position.Distance(Player.Position) > 600)
                {
                    Player.Spellbook.CastSpell(_flash.Slot, Player.Position.Extend(location.Position.To3D(), 450).To3D());
                }
                R.Cast(location.Position.To3D());
            }
        }

        private static void Pyromania()
        {
            var stacke = SpellMenu["estack"].Cast<CheckBox>().CurrentValue;
            var stackw = SpellMenu["wstack"].Cast<CheckBox>().CurrentValue;

            if (Player.HasBuff("pyromania_particle") || Player.CountEnemiesInRange(650) >= 1)
                return;
            if (stacke && E.IsReady())
            {
                E.Cast();
            }

            if (stackw && W.IsReady())
            {
                W.Cast(MousePos);
            }
        }

        private static void Flee()
        {
            Orbwalker.MoveTo(MousePos);
            E.Cast();
        }

        private static void Harass()
        {
            var target = TargetManager.Target(Q, DamageType.Magical);
            if (!Q.IsReady() || !TickManager.NoLag(1) || !SpellMenu["qth"].Cast<CheckBox>().CurrentValue ||
                target == null || !target.IsValidTarget(Q.Range))
                return;
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue && !_avoidSpam)
            {
                _avoidSpam = true;
            }
            else
            {
                Q.Cast(target);
            }
        }

        protected override void Volatile_ProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (SpellMenu["autoe"].Cast<CheckBox>().CurrentValue &&
                sender.IsEnemy
                && args.SData.IsAutoAttack()
                && args.Target.IsMe)
            {
                E.Cast();
            }
        }

        private static Obj_AI_Turret GetTurrets()
        {
            var turret =
                EntityManager.Turrets.Enemies.OrderBy(
                    x => x.Distance(TibbersObject.Position) <= 500 && !x.IsAlly && !x.IsDead)
                    .FirstOrDefault();
            return turret;
        }

        private static float Qcalc(Obj_AI_Base target)
        {
            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] {0, 80, 115, 150, 185, 220}[Q.Level] +
                 (0.80f*Player.FlatMagicDamageMod)));
        }


        private static Obj_AI_Base MinionLh(GameObjectType type, AttackSpell spell)
        {
            return ObjectManager.Get<Obj_AI_Base>().OrderBy(a => a.Health).FirstOrDefault(a => a.IsEnemy
                                                                                               && a.Type == type
                                                                                               &&
                                                                                               a.Distance(Player) <=
                                                                                               Q.Range
                                                                                               && !a.IsDead
                                                                                               && !a.IsInvulnerable
                                                                                               &&
                                                                                               a.IsValidTarget(
                                                                                                   Q.Range)
                                                                                               &&
                                                                                               a.Health <= Qcalc(a));
        }

        public static void LastHitB()
        {
            var qcheck = SpellMenu["LHQ"].Cast<CheckBox>().CurrentValue;
            var qready = Q.IsReady();
            if (!qcheck || !qready) return;
            var minion = (Obj_AI_Minion) MinionLh(GameObjectType.obj_AI_Minion, AttackSpell.Q);
            if (minion != null)
            {
                Q.Cast(minion);
            }
        }

        private static void MoveTibbers()
        {
            if (!SpellMenu["pilot"].Cast<CheckBox>().CurrentValue)
                return;

            var target = TargetSelector.GetTarget(2000, DamageType.Magical);

            if (Player.HasBuff("infernalguardiantime"))
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MovePet,
                    target.IsValidTarget(1500) ? target.Position : GetTurrets().Position);
            }

        }

        private static void Combo()
        {
            if (W.IsReady() && SpellMenu["wtc"].Cast<CheckBox>().CurrentValue)
            {
                var target = TargetManager.Target(W, DamageType.Magical);
                if (target != null && target.IsValidTarget(W.Range) &&
                    Player.Mana > (ManaManager.GetMana(SpellSlot.W))
                    && TickManager.NoLag(2))
                {
                    W.Cast(target.ServerPosition);
                }
            }
            else if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue && Q.IsReady() && TickManager.NoLag(1))
            {
                var target = TargetManager.Target(Q, DamageType.Magical);
                if (target != null && target.IsValidTarget(Q.Range))
                    Q.Cast(target);
            }
            else if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue && R.IsReady() && TickManager.NoLag(4))
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e =>
                            e.IsValidTarget(R.Range) &&
                            Player.GetSpellDamage(e, SpellSlot.R) >= Prediction.Health.GetPrediction(e, R.CastDelay));
                var emergency =
                    EntityManager.Heroes.Enemies.Find(
                        e =>
                            e.IsValidTarget(R.Range));
                if (enemy != null)
                    CastManager.Cast.Circle.Optimized(R, DamageType.Magical, (int) R.Range, 1, HitChance.High, enemy);
                else if (Player.HealthPercent <= 15)
                    if (emergency != null)
                        CastManager.Cast.Circle.Optimized(R, DamageType.Magical, (int) R.Range, 1, HitChance.High,
                            emergency);
            }
        }

        private enum AttackSpell
        {
            Q
        };
    }
}