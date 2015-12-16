using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;
using VolatileAIO.Organs.Brain.Utils;

namespace VolatileAIO.Extensions.Top
{
    internal class Vladimir : Heart
    {
        private static Spell.Active E, W;
        private static Spell.Targeted Q;
        private static Spell.Skillshot R;
        private static bool _avoidSpam;
        private static int _lastE;
        public static Menu SpellMenu;

        public Vladimir()
        {
            InitializeMenu();
            InitializeSpells();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q to Harass"));
            SpellMenu.Add("qath", new CheckBox("Use Q in Auto Harass"));
            SpellMenu.Add("qtl", new CheckBox("Use Q to LaneClear"));
            SpellMenu.Add("qtlh", new CheckBox("Use Q to Lasthit"));
            SpellMenu.Add("qtks", new CheckBox("Use Q to Killsteal"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtd", new CheckBox("Use W to Dodge"));
            SpellMenu.Add("wtdhp", new Slider("Minimum Health % to W Dodge", 15));
            SpellMenu.Add("wgptc", new CheckBox("Use W to AntiGapclose"));
            SpellMenu.Add("wgphp", new Slider("Minimum Health % to W Antigapcloser", 10));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E to Harass"));
            SpellMenu.Add("etl", new CheckBox("Use E to Laneclear"));
            SpellMenu.Add("eath", new CheckBox("Use E in Auto Harass"));
            SpellMenu.Add("etks", new CheckBox("Use E to Killsteal"));
            SpellMenu.Add("autostack", new CheckBox("Auto Stack", false));
            SpellMenu.Add("autostackhp", new Slider("Minimum Automatic E Stack HP", 25));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtc", new CheckBox("Use R in Combo"));
            SpellMenu.Add("ramount", new Slider("Minimum enemies hit to Combo R", 2, 1, 5));
            SpellMenu.AddSeparator();
            SpellMenu.Add("autor", new CheckBox("Auto R"));
            SpellMenu.Add("aramount", new Slider("Minimum enemies hit to Auto R", 3, 1, 5));

            SpellMenu.AddGroupLabel("Other Settings");
            SpellMenu.Add("ahs", new CheckBox("Auto Harass Toggle"));
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Targeted, Initialize.Type.Active,
                Initialize.Type.Active, Initialize.Type.Skillshot);
            Q = (Spell.Targeted) PlayerData.Spells[0];
            W = (Spell.Active) PlayerData.Spells[1];
            E = (Spell.Active) PlayerData.Spells[2];
            R = (Spell.Skillshot) PlayerData.Spells[3];
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (!gapcloser.Sender.IsEnemy || (!SpellMenu["wgptc"].Cast<CheckBox>().CurrentValue))
                return;
            if (ObjectManager.Player.Distance(gapcloser.Sender, true) <
                W.Range && sender.IsValidTarget() && W.IsReady() &&
                Player.HealthPercent <= SpellMenu["wgphp"].Cast<Slider>().CurrentValue)
            {
                W.Cast();
            }
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            Killsteal();
            if (SpellMenu["ahs"].Cast<CheckBox>().CurrentValue) AutoHarass();
            if (SpellMenu["autostack"].Cast<CheckBox>().CurrentValue) KeepEUp();
            if (SpellMenu["autor"].Cast<CheckBox>().CurrentValue) AutoR();
            if (ComboActive()) Combo();
            if (HarassActive()) Harass();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Flee) Flee();
            if (LastHitActive() || LaneClearActive()) Farm();
        }

        public static Obj_AI_Base GetEnemy(float range, GameObjectType t)
        {
            switch (t)
            {
                case GameObjectType.AIHeroClient:
                    return EntityManager.Heroes.Enemies.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
                default:
                    return EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(a => a.Health).FirstOrDefault(
                        a => a.Distance(EloBuddy.Player.Instance) < range && !a.IsDead && !a.IsInvulnerable);
            }
        }

        public static void AutoR()
        {
            if (SpellMenu["autor"].Cast<CheckBox>().CurrentValue)
            {
                if (EntityManager.Heroes.Enemies.Count(e => e.Distance(Player) < 700) <
                    SpellMenu["aramount"].Cast<Slider>().CurrentValue || !TickManager.NoLag(4) ||
                    !R.IsReady())
                    return;
                var champs =
                    EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < 700)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                        .ToList();
                var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 700);
                if (location.ChampsHit < SpellMenu["aramount"].Cast<Slider>().CurrentValue) return;
                R.Cast(location.Position.To3D());
            }
        }

        private static void Flee()
        {
            Orbwalker.MoveTo(Game.CursorPos);
            W.Cast();
        }

        private static void Harass()
        {
            var qtarget = TargetManager.Target(Q, DamageType.Magical);
            if (!Q.IsReady() || !TickManager.NoLag(1) || !SpellMenu["qth"].Cast<CheckBox>().CurrentValue ||
                qtarget == null || !qtarget.IsValidTarget(Q.Range))
                return;
            if (SpellMenu["qth"].Cast<CheckBox>().CurrentValue && !_avoidSpam)
            {
                _avoidSpam = true;
            }
            {
                Q.Cast(qtarget);
            }
            var etarget = TargetManager.Target(E, DamageType.Magical);
            if (!E.IsReady() || !TickManager.NoLag(3) || !SpellMenu["eth"].Cast<CheckBox>().CurrentValue)
                return;
            {
                if (etarget != null)
                    E.Cast();
            }
        }

        public static void AutoHarass()
        {
            if (SpellMenu["eath"].Cast<CheckBox>().CurrentValue)
            {
                var enemy = TargetManager.Target(E, DamageType.Magical);

                if (enemy != null)
                    E.Cast();
            }
            if (SpellMenu["qath"].Cast<CheckBox>().CurrentValue)
            {
                var qtarget = TargetManager.Target(Q, DamageType.Magical);

                if (qtarget != null)
                    Q.Cast(qtarget);
            }
        }

        private static float Qcalc(Obj_AI_Base target)
        {
            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                (new float[] {0, 90, 125, 160, 195, 230}[Q.Level] + (0.6f*Player.FlatMagicDamageMod)));
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
        private static Obj_AI_Base MinionELh(GameObjectType type, AttackSpell spell)
        {
            return ObjectManager.Get<Obj_AI_Base>().OrderBy(a => a.Health).FirstOrDefault(a => a.IsEnemy
                                                                                               && a.Type == type
                                                                                               &&
                                                                                               a.Distance(Player) <=
                                                                                               E.Range
                                                                                               && !a.IsDead
                                                                                               && !a.IsInvulnerable
                                                                                               &&
                                                                                               a.IsValidTarget(
                                                                                                   E.Range));
        }

        public static void Farm()
        {
            var qcheck = (SpellMenu["qtlh"].Cast<CheckBox>().CurrentValue && LastHitActive()) ||
                         (SpellMenu["qtl"].Cast<CheckBox>().CurrentValue && LaneClearActive());
            var echeck =  (SpellMenu["etl"].Cast<CheckBox>().CurrentValue && LaneClearActive());
            var qready = Q.IsReady();
            var eready = E.IsReady();
            if (!qcheck || !qready) return;
            var minion = (Obj_AI_Minion) MinionLh(GameObjectType.obj_AI_Minion, AttackSpell.Q);
            if (minion != null)
            {
                Q.Cast(minion);
            }
            if (!echeck || !eready) return;
            var eminion = (Obj_AI_Minion)MinionELh(GameObjectType.obj_AI_Minion, AttackSpell.E);
            if (eminion != null)
            {
                E.Cast();
            }
        }

        protected override void Volatile_ProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Type != Player.Type || !W.IsReady() || !sender.IsEnemy ||
                !SpellMenu["wtd"].Cast<CheckBox>().CurrentValue)
                return;

            if (!args.SData.IsAutoAttack())
            {
                if (CCDataBase.IsCC_SkillShot(args.SData.Name))
                {
                    if (Player.HealthPercent < SpellMenu["wtdhp"].Cast<Slider>().CurrentValue)
                    {
                        W.Cast();
                    }
                }
                if (CCDataBase.IsCC_NonSkillShot(args.SData.Name))
                {
                    if (Player.HealthPercent < SpellMenu["wtdhp"].Cast<Slider>().CurrentValue)
                    {
                        W.Cast();
                    }
                }
            }
        }

        public static int Now
        {
            get { return (int)DateTime.Now.TimeOfDay.TotalMilliseconds; }
        }
        public  void KeepEUp()
        {
            if (Player.IsRecalling() || MenuGUI.IsChatOpen)
            {
                return;
            }
              // Now - LastCast
              var stackHp = SpellMenu["autostackhp"].Cast<Slider>().CurrentValue;
            if (Now - _lastE >= 9900 && E.IsReady() &&
                (Player.Health/Player.MaxHealth) * 100 >= stackHp)
            {
                Chat.Print("Now: " + Now);
                Chat.Print("Last E: " + _lastE);
                E.Cast();
            }
        }

        protected override void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner == Player && args.Slot == SpellSlot.E)
                _lastE = Now;

        }

        private static void Killsteal()
        {
            if (!SpellMenu["qtks"].Cast<CheckBox>().CurrentValue || !Q.IsReady()) return;
            foreach (
                var qtarget in
                    EntityManager.Heroes.Enemies.Where(
                        hero => hero.IsValidTarget(Q.Range) && !hero.IsDead && !hero.IsZombie))
            {
                if (Player.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health && qtarget.Distance(Player) <= Q.Range)
                {
                    Q.Cast(qtarget);
                }
                if (!SpellMenu["etks"].Cast<CheckBox>().CurrentValue || !E.IsReady()) return;
                {
                    foreach (var etarget in EntityManager.Heroes.Enemies.Where(
                        hero => hero.IsValidTarget(E.Range) && !hero.IsDead && !hero.IsZombie).Where(etarget => Player.GetSpellDamage(etarget, SpellSlot.E) >= etarget.Health && etarget.Distance(Player) <= E.Range))

                        if (Player.GetSpellDamage(etarget, SpellSlot.E) >= etarget.Health && etarget.Distance(Player) <= E.Range)
                        {
                            E.Cast();
                        }
                }
                {
                }
            }
        }

        private static void Combo()
        {
            if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue && Q.IsReady() && TickManager.NoLag(1))
            {
                var target = TargetManager.Target(Q, DamageType.Magical);
                if (target != null && target.IsValidTarget(Q.Range))
                    Q.Cast(target);
            }
            else if (SpellMenu["etc"].Cast<CheckBox>().CurrentValue && E.IsReady() && TickManager.NoLag(3))
            {
                var target = TargetManager.Target(E, DamageType.Magical);
                if (target != null && target.IsValidTarget(E.Range))
                    E.Cast();
            }
            else if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue && R.IsReady() && TickManager.NoLag(4))
                foreach (
                    var target in
                        EntityManager.Heroes.Enemies.Where(
                            hero => hero.IsValidTarget(Q.Range) && !hero.IsDead && !hero.IsZombie))
                {
                    if (Q.IsReady() && Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health ||
                        E.IsReady() && Player.GetSpellDamage(target, SpellSlot.E) >= target.Health)
                        return;
                    if (SpellMenu["rtc"].Cast<CheckBox>().CurrentValue)
                    {
                        var champs =
                            EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < 700)
                                .Select(champ => Prediction.Position.PredictUnitPosition(champ, R.CastDelay))
                                .ToList();
                        var location = CastManager.GetOptimizedCircleLocation(champs, R.Radius, 700);

                        if (location.ChampsHit < SpellMenu["ramount"].Cast<Slider>().CurrentValue) return;
                        R.Cast(location.Position.To3D());
                    }
                }
        }

        private enum AttackSpell
        {
            Q,E
        };
    }
}