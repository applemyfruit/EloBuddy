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
    internal class Brand : Heart
    {
        #region Spell and Menu Declaration

        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;

        public static Menu SpellMenu;

        #endregion

        #region Spell and Menu Loading

        public Brand()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Skillshot, Initialize.Type.Skillshot,
                Initialize.Type.Targeted,
                Initialize.Type.Targeted);
            Q = (Spell.Skillshot) PlayerData.Spells[0];
            W = (Spell.Skillshot) PlayerData.Spells[1];
            E = (Spell.Targeted) PlayerData.Spells[2];
            R = (Spell.Targeted) PlayerData.Spells[3];
            W.AllowedCollisionCount = int.MaxValue;
            InitializeMenu();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qstun", new CheckBox("Only Q if target Ablazed"));
            SpellMenu.Add("qtj", new CheckBox("Use Q in Jungleclear"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wth", new CheckBox("Use W in Harass"));
            SpellMenu.Add("wtl", new CheckBox("Use W in Laneclear"));
            SpellMenu.Add("wtj", new CheckBox("Use W in Jungleclear"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("etl", new CheckBox("Use E in Laneclear"));
            SpellMenu.Add("etj", new CheckBox("Use E in Jungleclear"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo if:"));
            SpellMenu.Add("ramount", new Slider("Minimum enemies in range", 2, 2, 5));
            SpellMenu.Add("rks", new CheckBox("SmartFinisher R"));
        }

        #endregion

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            AutoSpells();

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;
                default:
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                    {
                        LaneClear();
                    }
                    else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                    {
                        JungleClear();
                    }
                    break;
            }
        }

        private static void AutoSpells()
        {
            if (SpellMenu["rks"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(4))
            {
                if (
                    EntityManager.Heroes.Enemies.Any(
                        e =>
                            e.Distance(Player) < R.Range &&
                            Prediction.Health.GetPrediction(e,
                                R.CastDelay +
                                ((int) Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.R).SData.MissileSpeed/
                                 (int) Player.Distance(e))) < Player.GetSpellDamage(e, SpellSlot.R)))
                {
                    R.Cast(EntityManager.Heroes.Enemies.First(
                        e =>
                            e.Distance(Player) < R.Range &&
                            Prediction.Health.GetPrediction(e,
                                R.CastDelay +
                                ((int) Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.R).SData.MissileSpeed/
                                 (int) Player.Distance(e))) < Player.GetSpellDamage(e, SpellSlot.R)));
                }
            }
        }

        private static void JungleClear()
        {
            if (SpellMenu["qtj"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(1))
            {
                if (
                    EntityManager.MinionsAndMonsters.Monsters.Any(
                        m => m.Distance(Player) < Q.Range && m.HasBuff("brandablaze")))
                {
                    Q.Cast(EntityManager.MinionsAndMonsters.Monsters.Where(
                        m =>
                            m.Distance(Player) < Q.Range && m.HasBuff("brandablaze") &&
                            Q.GetPrediction(m).HitChance > HitChance.High).OrderByDescending(m => m.MinionLevel).First());
                }
            }
            if (SpellMenu["wtj"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(2))
            {
                if (EntityManager.MinionsAndMonsters.Monsters.Any(
                    m => m.Distance(Player) < W.Range + W.Radius))
                {
                    var pos = MinionManager.GetBestCircularFarmLocation(EntityManager.MinionsAndMonsters.Monsters.Where(
                        m => m.Distance(Player) < W.Range).Select(m => m.Position.To2D()).ToList(), W.Radius, W.Range);
                    W.Cast(pos.Position.To3D());
                }
            }
            if (SpellMenu["etj"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                if (EntityManager.MinionsAndMonsters.Monsters.Any(
                    m => m.Distance(Player) < E.Range))
                {
                    E.Cast(EntityManager.MinionsAndMonsters.Monsters.Where(
                       m =>
                           m.Distance(Player) < E.Range).OrderByDescending(m => m.MinionLevel).First());
                }
            }
        }

        private static void LaneClear()
        {
            if (SpellMenu["wtl"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Farm(W, 3);
            }
            if (SpellMenu["eth"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                var minionlist = EntityManager.MinionsAndMonsters.EnemyMinions.Where(
                    m => m.Distance(Player) < E.Range && m.HasBuff("brandablaze")).ToList();
                if (
                    minionlist.Count() > 3)
                {
                    var avgpos = new Vector3((from minion in minionlist select minion.Position.X).Average(),
                        (from minion in minionlist select minion.Position.Y).Average(),
                        (from minion in minionlist select minion.Position.Z).Average());
                    var etarget =
                        minionlist
                            .OrderBy(m => m.Distance(avgpos))
                            .First();
                    E.Cast(etarget);
                }
            }
        }

        private static void Harass()
        {
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue)
            {
                if (SpellMenu["qstun"].Cast<CheckBox>().CurrentValue)
                {
                    var target = TargetManager.Target(Q, DamageType.Magical);
                    if (target.HasBuff("brandablaze"))
                        CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
                }
                else CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
            }
            if (SpellMenu["wth"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Optimized(W, DamageType.Magical);
            }
            if (SpellMenu["eth"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                var target = TargetManager.Target(E, DamageType.Magical);
                if (target.IsValidTarget())
                    E.Cast(target);
            }
        }

        private static void Combo()
        {
            if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue)
            {
                if (SpellMenu["qstun"].Cast<CheckBox>().CurrentValue)
                {
                    var target = TargetManager.Target(Q, DamageType.Magical);
                    if (target.HasBuff("brandablaze"))
                        CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
                }
                else CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Magical);
            }
            if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue)
            {
                CastManager.Cast.Circle.Optimized(W, DamageType.Magical);
            }
            if (SpellMenu["etc"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(3))
            {
                var target = TargetManager.Target(E, DamageType.Magical);
                if (target.IsValidTarget())
                    E.Cast(target);
            }
            if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue && TickManager.NoLag(4))
            {
                var target = TargetManager.Target(R, DamageType.Magical);
                if (target.IsValidTarget() &&
                    EntityManager.Heroes.Enemies.Count(e => e.Distance(target) < 460) >=
                    SpellMenu["ramount"].Cast<Slider>().CurrentValue)
                {
                    R.Cast(target);
                }
            }
        }
    }
}