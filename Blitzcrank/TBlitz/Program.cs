using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace TBlitz
{
    //Created by turkey for the EloBuddy Community! =)
    internal class Program
    {
        public static Spell.Skillshot Q;
        public static Spell.Active E;
        public static Spell.Active R;
        public static Menu BlitzMenu, ComboMenu, DrawMenu, MiscMenu, QMenu;
        public static AIHeroClient Me = ObjectManager.Player;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoaded;
        }

        private static void OnLoaded(EventArgs args)
        {
            Bootstrap.Init(null);
            Q = new Spell.Skillshot(SpellSlot.Q, 1000, SkillShotType.Linear, (int) 250f, (int) 1800f, (int) 70f);
            E = new Spell.Active(SpellSlot.E, 150);
            R = new Spell.Active(SpellSlot.R, 550);

            BlitzMenu = MainMenu.AddMenu("T.Blitz", "tblitz");
            BlitzMenu.AddGroupLabel("T.Blitz");
            BlitzMenu.AddSeparator();
            BlitzMenu.AddLabel("An AddOn made by EB user, turkey :)");

            ComboMenu = BlitzMenu.AddSubMenu("Combo", "sbtw");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.AddSeparator();
            ComboMenu.Add("usecomboq", new CheckBox("Use Q"));
            ComboMenu.Add("usecomboe", new CheckBox("Use E"));
            ComboMenu.Add("usecombor", new CheckBox("Use R"));
            ComboMenu.AddSeparator();
            ComboMenu.Add("rslider", new Slider("Minimum people for R", 1, 0, 5));

            QMenu = BlitzMenu.AddSubMenu("Q Settings", "qsettings");
            QMenu.AddGroupLabel("Q Settings");
            QMenu.AddSeparator();
            QMenu.Add("qmin", new Slider("Min Range", 255, 0, (int) Q.Range));
            QMenu.Add("qmax", new Slider("Max Range", (int) Q.Range, 0, (int) Q.Range));
            QMenu.AddSeparator();
            foreach (var obj in ObjectManager.Get<AIHeroClient>().Where(obj => obj.Team != Me.Team))
            {
                QMenu.Add("grab" + obj.ChampionName.ToLower(), new CheckBox("Grab " + obj.ChampionName));
            }

            MiscMenu = BlitzMenu.AddSubMenu("Misc", "misc");
            MiscMenu.AddGroupLabel("KS");
            MiscMenu.AddSeparator();
            MiscMenu.Add("ksq", new CheckBox("KS with Q"));
            MiscMenu.Add("ksr", new CheckBox("KS with R"));
            MiscMenu.AddSeparator();
            MiscMenu.AddGroupLabel("Interrupt");
            MiscMenu.AddSeparator();
            MiscMenu.Add("intq", new CheckBox("Q to Interrupt"));
            MiscMenu.Add("inte", new CheckBox("E to Interrupt"));
            MiscMenu.Add("dashq", new CheckBox("Q on Dashing"));
            MiscMenu.Add("intr", new CheckBox("R to Interrupt"));
            MiscMenu.Add("immoq", new CheckBox("Q on Immobile"));
            MiscMenu.AddSeparator();
            MiscMenu.Add("debug", new CheckBox("Debug", false));

            DrawMenu = BlitzMenu.AddSubMenu("Drawings", "drawings");
            DrawMenu.AddGroupLabel("Drawings");
            DrawMenu.AddSeparator();
            DrawMenu.Add("drawq", new CheckBox("Draw Q"));
            DrawMenu.Add("drawr", new CheckBox("Draw R"));


            Interrupter.OnInterruptableSpell += Interrupt;
            Orbwalker.OnPreAttack += OrbwalkerPreAttack;
            Game.OnTick += Tick;
            Drawing.OnDraw += OnDraw;
        }

        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (MiscMenu["intq"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                if (sender.Distance(Me, true) <= Q.RangeSquared)
                {
                    var pred = Q.GetPrediction(sender);
                    if (pred.HitChance >= HitChance.Low)
                    {
                        Q.Cast(pred.CastPosition);
                        if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                        {
                            Chat.Print("q-int");
                        }
                    }
                }
            }
            if (MiscMenu["intr"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                if (sender.Distance(Me, true) <= R.RangeSquared)
                {
                    R.Cast();

                    if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                    {
                        Chat.Print("r-int");
                    }
                }
            }
            if (MiscMenu["inte"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                if (sender.Distance(Me, true) <= E.RangeSquared)
                {
                    E.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, sender);

                    if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                    {
                        Chat.Print("e-int");
                    }
                }
            }
        }



        private static void OnDraw(EventArgs args)
        {
            if (!Me.IsDead)
            {
                if (DrawMenu["drawq"].Cast<CheckBox>().CurrentValue && Q.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, Q.Range, Color.White);
                }
                if (DrawMenu["drawr"].Cast<CheckBox>().CurrentValue && R.IsLearned)
                {
                    Drawing.DrawCircle(Me.Position, R.Range, Color.White);
                }
            }
        }

        private static void OrbwalkerPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (E.IsReady() && ComboMenu["usecomboe"].Cast<CheckBox>().CurrentValue)
                E.Cast();
        }

        private static void Tick(EventArgs args)
        {
            KillSecure();
            AutoCast();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo(ComboMenu["usecomboq"].Cast<CheckBox>().CurrentValue,
                    ComboMenu["usecombor"].Cast<CheckBox>().CurrentValue);
            }
        }

        private static void AutoCast()
        {
            if (Q.IsReady())
            {
                try
                {
                    foreach (
                        var enemy in
                            ObjectManager.Get<AIHeroClient>()
                                .Where(x => x.IsValidTarget(MiscMenu["qmax"].Cast<Slider>().CurrentValue)))
                    {
                        if (MiscMenu["dashq"].Cast<CheckBox>().CurrentValue &&
                            MiscMenu["grab" + enemy.ChampionName].Cast<CheckBox>().CurrentValue)
                            if (enemy.Distance(Me.ServerPosition) > MiscMenu["qmin"].Cast<Slider>().CurrentValue)
                                if (Q.GetPrediction(enemy).HitChance == HitChance.Dashing)
                                {
                                    Q.Cast(enemy);

                                    if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                                    {
                                        Chat.Print("q-dash");
                                    }
                                }
                        if (MiscMenu["imoq"].Cast<CheckBox>().CurrentValue &&
                            MiscMenu["grab" + enemy.ChampionName].Cast<CheckBox>().CurrentValue)
                            if (enemy.Distance(Me.ServerPosition) > MiscMenu["qmin"].Cast<Slider>().CurrentValue)
                                if (Q.GetPrediction(enemy).HitChance == HitChance.Immobile)
                                {
                                    Q.Cast(enemy);

                                    if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                                    {
                                        Chat.Print("q-immo");
                                    }
                                }
                    }
                }
                catch
                {
                }
            }
        }

        private static void KillSecure()
        {
            if (R.IsReady() && MiscMenu["ksr"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var rtarget in HeroManager.Enemies.Where(hero => hero.IsValidTarget(R.Range) && !hero.IsDead && !hero.IsZombie))
                {
                    if (Me.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                    {
                        R.Cast();
                        if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                        {
                            Chat.Print("r-ks");
                        }
                    }
                }
            }
            if (MiscMenu["ksq"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                try
                {
                    foreach (var qtarget in HeroManager.Enemies.Where(hero => hero.IsValidTarget(Q.Range) && !hero.IsDead && !hero.IsZombie))
                    {
                        if (Me.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health)
                        {
                            var poutput = Q.GetPrediction(qtarget);
                            if (poutput.HitChance >= HitChance.Medium)
                            {
                                Q.Cast(poutput.CastPosition);
                                if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                                {
                                    Chat.Print("q-ks");
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void Combo(bool shoulduseQ, bool shoulduseR)
        {

            if (shoulduseQ && Q.IsReady())
            {
                try
                {
                    var grabTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                    if (Q.GetPrediction(grabTarget).HitChance >= HitChance.High)
                    {
                        if (grabTarget.Distance(Me.ServerPosition) > QMenu["qmin"].Cast<Slider>().CurrentValue)
                        {
                            if (QMenu["grab" + grabTarget.ChampionName].Cast<CheckBox>().CurrentValue)
                            {
                                Q.Cast(grabTarget);

                                if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                                {
                                    Chat.Print("q-combo");
                                }
                            }
                        }
                    }

                }
                catch
                {
                }
                if (shoulduseR && R.IsReady() &&
                    Me.CountEnemiesInRange(R.Range) >= ComboMenu["rslider"].Cast<Slider>().CurrentValue)
                {
                    R.Cast();

                    if (MiscMenu["debug"].Cast<CheckBox>().CurrentValue)
                    {
                        Chat.Print("r-combo");
                    }
                }
            }

        }
    }
}
