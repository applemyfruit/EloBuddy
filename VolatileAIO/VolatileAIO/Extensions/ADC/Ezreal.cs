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
using VolatileAIO.Organs._Test;

namespace VolatileAIO.Extensions.ADC
{
    internal class Ezreal : Heart
    {
        #region Spell and Menu Declaration

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
            DrawManager.UpdateValues(Q, W, E, R);
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
            var qdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.Q);
            var wdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.W);
            var rdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.R);

            Q = new Spell.Skillshot(SpellSlot.Q, (uint)qdata.Range, qdata.Type, qdata.Delay, qdata.MissileSpeed, qdata.Radius)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Skillshot(SpellSlot.W, (uint)wdata.Range, wdata.Type, wdata.Delay, wdata.MissileSpeed, wdata.Radius)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 475, SkillShotType.Circular, 250, 1600, 80)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, (uint)rdata.Range, rdata.Type, rdata.Delay, rdata.MissileSpeed, rdata.Radius)
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
                SpellMenu["wth"].Cast<CheckBox>().CurrentValue);
        }

        private static void Combo()
        {
            //Use Spells
            CastSpellLogic(SpellMenu["qtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["wtc"].Cast<CheckBox>().CurrentValue,
                SpellMenu["eks"].Cast<CheckBox>().CurrentValue);
        }

        private static void AutoCastSpells()
        {
            if (Q.IsReady() && TickManager.NoLag(0))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(Q.Range)))
                {
                    if (enemy.IsEnemy && enemy.IsValidTarget(SpellMenu["qmaxrange"].Cast<Slider>().CurrentValue) &&
                        !enemy.IsDead)
                    {
                        if (W.IsReady())
                        {
                            if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.Q) +
                                    Player.GetSpellDamage(Player, SpellSlot.W)))
                            {
                                CastSpellLogic(true, true, false, enemy);
                            }
                        }
                        else
                        {
                            if (Prediction.Health.GetPrediction(enemy, Q.CastDelay) <
                                Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                    Player.GetSpellDamage(Player, SpellSlot.Q)))
                            {
                                CastSpellLogic(true, false, false, enemy);
                            }
                        }

                        if ((Q.GetPrediction(enemy).HitChance == HitChance.Immobile &&
                             SpellMenu["qonimmo"].Cast<CheckBox>().CurrentValue) ||
                            (Q.GetPrediction(enemy).HitChance == HitChance.Dashing &&
                             SpellMenu["qonvery"].Cast<CheckBox>().CurrentValue))
                        {
                            Q.Cast(enemy.Position);
                        }
                    }
                }
            }
            if (W.IsReady() && TickManager.NoLag(2))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(W.Range)))
                {
                    if (enemy.IsEnemy && enemy.IsValidTarget(SpellMenu["wmaxrange"].Cast<Slider>().CurrentValue) &&
                        !enemy.IsDead)
                    {
                        if (Prediction.Health.GetPrediction(enemy, W.CastDelay) <
                            Player.CalculateDamageOnUnit(enemy, DamageType.Magical,
                                Player.GetSpellDamage(Player, SpellSlot.W))*1.4)
                        {
                            CastSpellLogic(false, true, false, enemy);
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
                if (InFountain(Player) && SpellMenu["qstack1"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(Player);
                }
                else if (Player.Mana > 0.95*Player.MaxMana && SpellMenu["qstack2"].Cast<CheckBox>().CurrentValue)
                {
                    if (TargetSelector.GetTarget(Q.Range, DamageType.Physical) != null)
                    {
                        var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                        Q.Cast(t.ServerPosition);
                    }
                    Q.Cast(Player);
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
                }
            }
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!W.IsReady() || !SpellMenu["wtopush"].Cast<CheckBox>().CurrentValue || !target.IsStructure()) return;
            foreach (var ally in EntityManager.Heroes.Allies.Where(ally => !ally.IsMe && ally.IsAlly && ally.Distance(Player.Position) < W.Range))
            {
                W.Cast(ally);
            }
        }

        private static void CastSpellLogic(bool useQ = false, bool useW = false, bool useE = false,
            AIHeroClient target = null)
        {
            if (useQ)
            {
                if (target != null)
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Physical, SpellMenu["qmaxrange"].Cast<Slider>().CurrentValue, HitChance.Medium, target);
                else
                    CastManager.Cast.Line.SingleTargetHero(Q, DamageType.Physical, SpellMenu["qmaxrange"].Cast<Slider>().CurrentValue);
            }
            if (useW)
            {
                if (target != null)
                    CastManager.Cast.Line.SingleTargetHero(W, DamageType.Magical, SpellMenu["wmaxrange"].Cast<Slider>().CurrentValue, HitChance.Medium, target);
                else
                    CastManager.Cast.Line.SingleTargetHero(W, DamageType.Magical, SpellMenu["wmaxrange"].Cast<Slider>().CurrentValue);
            }
            if (useE && E.IsReady() && TickManager.NoLag(3))
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (Player.Distance(enemy) > Player.AttackRange &&
                        enemy.Distance(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld()) < Player.AttackRange &&
                        enemy.Health < (Player.GetAutoAttackDamage(enemy)*2) + Player.GetSpellDamage(enemy, SpellSlot.E))
                    {
                        E.Cast(Player.Position.Extend(Game.CursorPos, E.Range).To3DWorld());
                    }
                }
            }
        }

    }
}