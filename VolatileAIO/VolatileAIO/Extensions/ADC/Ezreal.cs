using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using System.Drawing;
using System.Linq;
using EloBuddy.SDK.Events;

namespace VolatileAIO.Extensions.ADC
{
    internal class Ezreal : Heart
    {
        #region Spell and Menu Declaration

        public static int Mode;
        public static bool IsAutoAttacking;

        public static ManaManager ManaManager = new ManaManager();

        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Skillshot E;
        public static Spell.Skillshot R;

        public static Menu SpellMenu;
        public static Menu ModeMenu;
        
        #endregion

        #region Spell and Menu Loading

        public Ezreal()
        {
            InitializeSpells();
            InitializeMenu();
            InitializeIndicator();
        }

        private static void InitializeMenu()
        {
            //Modes
            ModeMenu = VolatileMenu.AddSubMenu("Modes", "ModeMenu");

            //Combo Menu
            
            ModeMenu.AddGroupLabel("Combo");
            ModeMenu.Add("useQTC", new CheckBox("Use Q"));
            ModeMenu.Add("useWTC", new CheckBox("Use W"));
            ModeMenu.Add("useETC", new CheckBox("Use E"));
            ModeMenu.Add("useRTC", new CheckBox("Use R"));

            //Harrass Menu
            ModeMenu.AddGroupLabel("Harass");
            ModeMenu.Add("useQTH", new CheckBox("Use Q"));
            ModeMenu.Add("useWTH", new CheckBox("Use W"));

            //Laneclear Menu
            ModeMenu.AddGroupLabel("Laneclear");
            ModeMenu.Add("useQTL", new CheckBox("Use Q"));

            //Spell Menu
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qmaxrange", new Slider("Max Range to Q", 1050,500,1200));
            SpellMenu.Add("qonslow", new CheckBox("Q on Slowed Targets"));
            SpellMenu.Add("qonimmo", new CheckBox("Q on Immobile Targets"));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wmaxrange", new Slider("Max Range to W", 900, 500, 1050));
            SpellMenu.Add("wtopush", new CheckBox("W Ally to push tower"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etokill", new CheckBox("E if Enemy Killable"));
            SpellMenu.Add("etosafe", new CheckBox("Anti-Gapcloser E"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rminrange", new Slider("Minimum Range to R", 300, 0, 1000));
            SpellMenu.Add("rmaxrange", new Slider("Maximum Range to R", 2000, 0, 3000));
            SpellMenu.AddSeparator();
            SpellMenu.AddLabel("Don't Use R On: ");
            SpellMenu.AddSeparator();
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("r"+enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
        }

        public static void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1190, SkillShotType.Linear, (int) 250f, (int) 2000f, (int) 60f)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Skillshot(SpellSlot.W, 1050, SkillShotType.Linear, (int) 250f, (int) 1600f, (int) 80f)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Circular, (int) 250f, (int) 1600f, (int) 80f)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, 3000, SkillShotType.Linear, (int) 1000f, (int) 2000f, (int) 160f)
            {
                AllowedCollisionCount = int.MaxValue
            };
        }

        private static void InitializeIndicator()
        {
        }

        #endregion

        protected override void Volative_OnDraw(EventArgs args)
        {
            var target = new TargetManager().Target(Q, DamageType.Physical);
            if (target != null)
            {
                Drawing.DrawCircle(target.Position, 200, Color.Red);
            }
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            if (Player.IsDead) return;
            AutoCastSpells();
            ManaManager.SetMana();
            Stack();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
        }

        private static void Combo()
        {
            //Use Spells
            CastSpellLogic(ModeMenu["useQTC"].Cast<CheckBox>().CurrentValue, ModeMenu["useWTC"].Cast<CheckBox>().CurrentValue,
                ModeMenu["useETC"].Cast<CheckBox>().CurrentValue, ModeMenu["useRTC"].Cast<CheckBox>().CurrentValue);
            //Use Items
            var itemTarget = TargetSelector.GetTarget(750, DamageType.Physical);
        }

        private static void AutoCastSpells()
        {
            if (Q.IsReady())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                enemy =>
                                    enemy.IsEnemy && enemy.IsValid && !enemy.IsDead && enemy.Distance(Player) < Q.Range))
                {
                    if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                        Player.CalculateDamageOnUnit(enemy, DamageType.Magical, Player.GetSpellDamage(Player, SpellSlot.Q)) * 2)
                    {
                        CastSpellLogic(true, false, false, false, enemy);
                    }
                    if (Q.GetPrediction(enemy).HitChance == HitChance.Immobile)
                    {
                        CastSpellLogic(true, false, false, false, enemy);
                    }
                }
            }
            if (W.IsReady())
            {
                
            }
        }

        private static void Stack()
        {
            if (!Item.HasItem(3070) && !Item.HasItem(3004) && !Q.IsReady() && !TickManager.NoLag(1)) return;
            if (Player.Mana > 0.95*Player.MaxMana)
            {
                if (TargetSelector.GetTarget(Q.Range, DamageType.Physical)!=null)
                {
                    var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                    Q.Cast(t.ServerPosition);
                }
                Q.Cast(Player);
            }
            if (InFountain(Player))
            {
                Q.Cast(Player);
            }
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (SpellMenu["etosafe"].Cast<CheckBox>().CurrentValue && E.IsReady() && Player.Mana > ManaManager.ManaQ + ManaManager.ManaR && Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld());
                }
            }
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = 0;

            if (Q.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.W);

            if (E.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.E);

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R);
            

            return (float)(comboDamage + Player.GetAutoAttackDamage(target) * 2);
        }

        private static void CastSpellLogic(bool useQ = false, bool useW = false, bool useE = false, bool useR = false, Obj_AI_Base target = null)
        {
            if (useQ)
            {
                if (target != null)
                    new CastManager().CastSkillShot(Q, DamageType.Physical, HitChance.Medium, target);
                else
                    new CastManager().CastSkillShot(Q, DamageType.Physical);
            }
            if (useW)
            {
                if (target != null)
                    new CastManager().CastSkillShot(W, DamageType.Magical, HitChance.Medium, target);
                else
                    new CastManager().CastSkillShot(W, DamageType.Magical);
            }
        }
    }
}
