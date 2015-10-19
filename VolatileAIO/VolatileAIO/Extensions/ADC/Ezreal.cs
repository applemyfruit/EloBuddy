using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;

namespace VolatileAIO.Extensions.ADC
{
    internal class Ezreal : Heart
    {
        #region Spell and Menu Declaration

        public static int Mode;
        public static bool IsAutoAttacking;

        public static ManaManager ManaManager = new ManaManager();
        public static DrawManager DrawManager = new DrawManager();

        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Skillshot E;
        public static Spell.Skillshot R;

        public static Menu SpellMenu;

        #endregion

        #region Spell and Menu Loading

        public Ezreal()
        {
            InitializeSpells();
            InitializeMenu();
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("useQTL", new CheckBox("Use Q in farm"));
            SpellMenu.Add("qonvery", new CheckBox("Auto Q on Very High Hitchance"));
            SpellMenu.Add("qonimmo", new CheckBox("Auto Q on Immobile Targets"));
            SpellMenu.Add("qstack1", new CheckBox("Stack Tear in Fountain"));
            SpellMenu.Add("qstack2", new CheckBox("Stack Tear when Full Mana"));
            SpellMenu.Add("qmaxrange", new Slider("Max Range to Q", 1050, 500, 1200));

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo"));
            SpellMenu.Add("wth", new CheckBox("Use W in Harass"));
            SpellMenu.Add("wtopush", new CheckBox("W Ally to push tower"));
            SpellMenu.Add("wmaxrange", new Slider("Max Range to W", 900, 500, 1050));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("eks", new CheckBox("SmartFinisher E"));
            SpellMenu.Add("etosafe", new CheckBox("Anti-Gapcloser E"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rks", new CheckBox("SmartFinisher R"));
            SpellMenu.Add("rsafe", new CheckBox("Only use R when its safe"));
            SpellMenu.AddSeparator();
            SpellMenu.AddLabel("Use R On: ");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("r" + enemy.ChampionName, new CheckBox(enemy.ChampionName));
            }
        }

        public static void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1190, SkillShotType.Linear, (int)250f, (int)2000f, (int)60f)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Skillshot(SpellSlot.W, 1050, SkillShotType.Linear, (int)250f, (int)1600f, (int)80f)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Circular, (int)250f, (int)1600f, (int)80f)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, 3000, SkillShotType.Linear, (int)1000f, (int)2000f, (int)160f)
            {
                AllowedCollisionCount = int.MaxValue
            };
        }

        #endregion

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            if (Player.IsDead) return;
            AutoCastSpells();
            ManaManager.SetMana();
            Stack();
            DrawManager.UpdateValues(Q, W, E, R);
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                Harass();
            }
        }

        private static void Harass()
        {
            //Use Spells
            CastSpellLogic(SpellMenu["qth"].Cast<CheckBox>().CurrentValue,
                SpellMenu["wth"].Cast<CheckBox>().CurrentValue,
                false);
        }

        private static void Combo()
        {
            //Use Spells
            CastSpellLogic(SpellMenu["qtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["wtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["eks"].Cast<CheckBox>().CurrentValue);
            //Use Items
            var itemTarget = TargetSelector.GetTarget(750, DamageType.Physical);
        }

        private static void AutoCastSpells()
        {
            if (Q.IsReady() && TickManager.NoLag(0))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(Q.Range)))
                {
                    if (enemy.IsEnemy && enemy.IsValid && !enemy.IsDead)
                    {
                        if (W.IsReady())
                        {
                            if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.Q) +
                                    Player.GetSpellDamage(Player, SpellSlot.W)))
                            {
                                CastSpellLogic(true, true, false, enemy);
                                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("TRY KS Q+W");
                            }
                        }
                        else
                        {
                            if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.Q)))
                            {
                                CastSpellLogic(true, false, false, enemy);
                                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("TRY KS Q NO W");
                            }
                        }

                        if ((Q.GetPrediction(enemy).HitChance == HitChance.Immobile &&
                             SpellMenu["qonimmo"].Cast<CheckBox>().CurrentValue) ||
                            (Q.GetPrediction(enemy).HitChance == HitChance.High &&
                             SpellMenu["qonvery"].Cast<CheckBox>().CurrentValue))
                        {
                            CastSpellLogic(true, false, false, enemy);
                            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("AUTOQ");
                        }
                    }
                }
            }
            if (W.IsReady() && TickManager.NoLag(2))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(W.Range)))
                {
                    if (enemy.IsEnemy && enemy.IsValid && !enemy.IsDead)
                    {
                        if (Prediction.Health.GetPrediction(enemy, W.CastDelay) <
                            Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                Player.GetSpellDamage(Player, SpellSlot.W))*1.4)
                        {
                            CastSpellLogic(false, true, false, enemy);
                            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("USED W KS");
                        }
                    }
                }
            }
            if (R.IsReady() && TickManager.NoLag(4))
            {
                if (SpellMenu["rks"].Cast<CheckBox>().CurrentValue)
                {
                    if (SpellMenu["rsafe"].Cast<CheckBox>().CurrentValue)
                    {
                        if (Player.CountEnemiesInRange(850) != 0) return;
                        foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(2800)))
                        {
                            if (Prediction.Health.GetPrediction(enemy, R.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.R)))
                            {
                                R.Cast(enemy);
                                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("USED R SAFELY");
                            }
                        }
                    }
                    else
                    {
                        foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(2800)))
                        {
                            if (Prediction.Health.GetPrediction(enemy, (int) GetRTravel(enemy.Position)) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.R)))
                            {
                                R.Cast(enemy);
                                if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                                    Chat.Print("USED R NO SAFECHECK");
                            }
                        }
                    }
                }
            }
        }

        private static float GetRTravel(Vector3 targetpos)
        {
            var distance = Vector3.Distance(Player.ServerPosition, targetpos);
            return (distance/R.Speed + R.CastDelay);
        }

        private static void Stack()
        {
            if ((Item.HasItem(3070) || Item.HasItem(3004)) && Q.IsReady() && TickManager.NoLag(1) &&
                !Player.IsRecalling())
            {
                if (InFountain(Player))
                {
                    Q.Cast(Player);
                    if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("STACKQ: FOUNTAIN");
                }
                else if (Player.Mana > 0.95*Player.MaxMana)
                {
                    if (TargetSelector.GetTarget(Q.Range, DamageType.Physical) != null)
                    {
                        var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                        Q.Cast(t.ServerPosition);
                    }
                    Q.Cast(Player);
                    if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("STACKQ: MAXMANA 95%");
                }
            }
        }

        protected override void Volatile_AntiGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (SpellMenu["etosafe"].Cast<CheckBox>().CurrentValue && E.IsReady() && sender.IsEnemy &&
                Player.Mana > ManaManager.ManaQ + ManaManager.ManaR &&
                Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld());
                    if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
                        Chat.Print("AGC E: " + sender.ChampionName.ToUpper());
                }
            }
        }

        private static void CastSpellLogic(bool useQ = false, bool useW = false, bool useE = false,
            Obj_AI_Base target = null)
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
            if (useE && E.IsReady() && TickManager.NoLag(3))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (Player.Distance(enemy) > Player.AttackRange &&
                        enemy.Distance(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld()) < Player.AttackRange &&
                        enemy.Health < Player.GetAutoAttackDamage(enemy)*2)
                    {
                        E.Cast(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld());
                        if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue) Chat.Print("E SAFE FINISHER");
                    }
                }
            }
        }

    }
}