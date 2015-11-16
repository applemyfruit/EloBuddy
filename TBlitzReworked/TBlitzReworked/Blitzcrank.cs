using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using Color = System.Drawing.Color;

namespace TBlitzReworked
{
    internal class Blitzcrank
    {
        public static int CastCount;
        public static int HitCount;
        public static float LastGrab;
        public static float LastHit;

        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active E;
        public static Spell.Active R;

        public static Menu BlitzMenu;
        public static Menu SpellMenu;
        public static Menu DrawingsMenu;

        public static AIHeroClient Player = ObjectManager.Player;

        public Blitzcrank()
        {
            InitializeMenu();
            InitializeSpells();
            InitializeEvents();
            CastCount = 0;
            HitCount = 0;
        }

        private static void InitializeEvents()
        {
            Game.OnTick += OnTick;
            Orbwalker.OnPostAttack += OnPostAttack;
            Spellbook.OnCastSpell += OnSpellCast;
            Drawing.OnEndScene += Drawings;
        }

        private static void InitializeMenu()
        {
            BlitzMenu = MainMenu.AddMenu("T.Blitz Reworked", "blitz", "T.Blitz Reworked");
            BlitzMenu.AddGroupLabel("T.Blitz Reworked is a whole new blitz experience compared to old T.Blitz");
            BlitzMenu.AddLabel("Any and all suggestions are welcome.");
            BlitzMenu.AddSeparator(400);
            BlitzMenu.AddLabel("Rework was originally developed for my aio which doesnt exist and cant be found in my github.");

            SpellMenu = BlitzMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("Q Settings");
            SpellMenu.Add("qtc", new CheckBox("Use Q in Combo"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qontop", new CheckBox("Auto Q top priority target(common ts)"));
            SpellMenu.Add("qonimmo", new CheckBox("Auto Q on Immobile Targets"));
            SpellMenu.AddLabel("Never Grab");
            foreach (var hero in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("dontgrab" + hero.ChampionName.ToLower(),
                    TargetSelector.GetPriority(hero) <= 2
                        ? new CheckBox(hero.ChampionName)
                        : new CheckBox(hero.ChampionName, false));
            }

            SpellMenu.AddGroupLabel("W Settings");
            SpellMenu.Add("wtc", new CheckBox("Use W in Combo", false));
            SpellMenu.Add("wtpush", new CheckBox("Use W to push towers and clear wards faster"));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("etc", new CheckBox("Use E in Combo"));
            SpellMenu.Add("eth", new CheckBox("Use E in Harass"));
            SpellMenu.Add("etpush", new CheckBox("Use E to push towers and clear wards faster"));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("rtks", new CheckBox("Use R to KS", false));
            SpellMenu.Add("rcmode", new Slider("R Mode - Q->E->R", 0, 0, 2)).OnValueChange +=
                Blitzcrank_OnComboModeChanged;
            SpellMenu.Add("ramount", new Slider("Enemies in range", 2, 1, 5)).IsVisible = false;
            UpdateSliders(1);

            DrawingsMenu = BlitzMenu.AddSubMenu("Drawings Menu", "drawmenu");
            DrawingsMenu.AddGroupLabel("Q Settings");
            DrawingsMenu.Add("drawmode", new Slider("Mode - Rangeline", 1, 1, 3)).OnValueChange += Blitzcrank_OnQDrawModeChanged;
            DrawingsMenu.Add("drawhc", new CheckBox("Draw Hitchance"));
            DrawingsMenu.Add("drawhc2", new CheckBox("Draw Hitcount"));
            UpdateSliders(2);
        }
        
        public void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 980, SkillShotType.Linear, (int)250f, (int)1800f, (int)70f);
            W = new Spell.Active(SpellSlot.W, 0);
            E = new Spell.Active(SpellSlot.E, 150);
            R = new Spell.Active(SpellSlot.R, 550);
        }

        private static void Blitzcrank_OnQDrawModeChanged(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSliders(2);
        }

        private static void Blitzcrank_OnComboModeChanged(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSliders(1);
        }

        private static void UpdateSliders(int i)
        {
            switch (i)
            {
                case 1:
                    switch (SpellMenu["rcmode"].Cast<Slider>().CurrentValue)
                    {
                        case 0:
                            SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - Q->E->R";
                            SpellMenu["ramount"].Cast<Slider>().IsVisible = false;
                            break;
                        case 1:
                            SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - R if ";
                            SpellMenu["ramount"].Cast<Slider>().IsVisible = true;
                            break;
                        case 2:
                            SpellMenu["rcmode"].Cast<Slider>().DisplayName = "ComboMode - Dont use R in combo";
                            SpellMenu["ramount"].Cast<Slider>().IsVisible = false;
                            break;
                    }
                    break;
                case 2:
                    switch (DrawingsMenu["drawmode"].Cast<Slider>().CurrentValue)
                    {
                        case 1:
                            DrawingsMenu["drawmode"].Cast<Slider>().DisplayName = "Mode - Rangeline";
                            break;
                        case 2:
                            DrawingsMenu["drawmode"].Cast<Slider>().DisplayName = "Mode - Rangecircle";
                            break;
                        case 3:
                            DrawingsMenu["drawmode"].Cast<Slider>().DisplayName = "Mode - Disabled";
                            break;
                    }
                    break;
            }
            
        }

        private static string HitchanceString(AIHeroClient t)
        {
            var hc = Q.GetPrediction(t).HitChance;
            return "Hitchance - " + hc;
        }

        private static void OnTick(EventArgs args)
        {
            AutoCast();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass) Harass();
            if (!Q.IsReady() && (Game.Time - LastHit) > 2)
            {
                foreach (var e in EntityManager.Heroes.Enemies.Where(t => t.HasBuff("rocketgrab2")))
                {
                    HitCount++;
                    LastHit = Game.Time;
                }
            }
        }

        private static void Drawings(EventArgs args)
        {
            switch (DrawingsMenu["drawmode"].Cast<Slider>().CurrentValue)
            {
                case 1:
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(e=>Player.Distance(e)<Q.Range))
                    {
                        if(SpellMenu["dontgrab"+enemy.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue)
                            Drawing.DrawLine(Player.Position.WorldToScreen(), enemy.Position.WorldToScreen(), 1,
                            Color.Maroon);
                        else
                        Drawing.DrawLine(Player.Position.WorldToScreen(), enemy.Position.WorldToScreen(), 1,
                            enemy == TargetSelector.GetTarget(Q.Range, DamageType.Magical) ? Color.Green : Color.Gray);
                    }
                    break;
                case 2:
                    Drawing.DrawCircle(Player.Position, Q.Range, Color.LawnGreen);
                    break;
            }
            if (DrawingsMenu["drawhc"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => Player.Distance(e) <= Q.Range))
                {
                    if (SpellMenu["dontgrab" + enemy.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue)
                        Drawing.DrawText(enemy.Position.WorldToScreen().X - 20, enemy.Position.WorldToScreen().Y + 20,
                        Color.Red, HitchanceString(enemy));
                    else
                    Drawing.DrawText(enemy.Position.WorldToScreen().X - 20, enemy.Position.WorldToScreen().Y + 20,
                        enemy == TargetSelector.GetTarget(Q.Range, DamageType.Magical) ? Color.Chartreuse : Color.Gray,
                        HitchanceString(enemy));
                }
            }
            if (DrawingsMenu["drawhc2"].Cast<CheckBox>().CurrentValue)
            {
                var w = Drawing.Width - 200;
                var h = (Drawing.Height / (float) 3) * 1.5;
                Drawing.DrawText(w, (float)h, Color.Red,
                    String.Format("Casted Spell.Q Count: {0}", CastCount));
                Drawing.DrawText(w, (float)h + 20, Color.Red,
                    String.Format("Hit Spell.Q Count: {0}", HitCount));
                Drawing.DrawText(w, (float)h + 40, Color.Red,
                    String.Format("Hitchance (%): {0}%",
                        CastCount > 0
                            ? (((float)HitCount / CastCount) * 100).ToString("00.00")
                            : "n/a"));
            }
        }

        private static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!target.IsWard() && !target.IsStructure()) return;
            if (SpellMenu["etpush"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                E.Cast();
            }
            if (SpellMenu["wtpush"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                W.Cast();
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target !=null && SpellMenu["qth"].Cast<CheckBox>().CurrentValue &&
                !SpellMenu["dontgrab" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                Player.Distance(target) > Player.GetAutoAttackRange())
                QLogic(target);

            if (SpellMenu["eth"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 300);
                if (enemy != null)
                {
                    Orbwalker.DisableMovement = true;
                    Orbwalker.DisableAttacking = true;
                    E.Cast();
                    EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, enemy);
                    Orbwalker.DisableMovement = false;
                    Orbwalker.DisableAttacking = false;
                }
            }
        }

        private static void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe) return;
            if (args.Slot == SpellSlot.E) Orbwalker.ResetAutoAttack();
            if (args.Slot == SpellSlot.Q && (Game.Time - LastGrab) > 2)
            {
                CastCount++;
                LastGrab = Game.Time;
            }
        }

        private static void AutoCast()
        {
            if (SpellMenu["qontop"].Cast<CheckBox>().CurrentValue)
            {
                var max = EntityManager.Heroes.Enemies.Max(t => TargetSelector.GetPriority(t));
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            e =>
                                e.IsValidTarget(Q.Range) &&
                                !SpellMenu["dontgrab" + e.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue)
                            .Where(enemy => TargetSelector.GetPriority(enemy) == max))
                {
                    QLogic(enemy);
                }
            }
            if (SpellMenu["qonimmo"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e => !SpellMenu["dontgrab" + e.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                             e.IsValidTarget(Q.Range) &&
                             (Q.GetPrediction(e).HitChance == HitChance.Dashing ||
                              Q.GetPrediction(e).HitChance == HitChance.Immobile));
                if (enemy != null)
                    QLogic(enemy);
            }
            if (SpellMenu["rtks"].Cast<CheckBox>().CurrentValue)
            {
                var enemy =
                    EntityManager.Heroes.Enemies.Find(
                        e =>
                            e.IsValidTarget(R.Range) &&
                            Player.GetSpellDamage(e, SpellSlot.R) >= Prediction.Health.GetPrediction(e, R.CastDelay));
                if (enemy != null)
                    R.Cast();
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target != null)
                if (SpellMenu["qtc"].Cast<CheckBox>().CurrentValue &&
                    !SpellMenu["dontgrab" + target.ChampionName.ToLower()].Cast<CheckBox>().CurrentValue &&
                    Player.Distance(target) > Player.GetAutoAttackRange() && Player.Distance(target)<Q.Range)
                    QLogic(target);

            if (SpellMenu["wtc"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 400);
                if (enemy != null)
                {
                    W.Cast();
                }
            }

            if (SpellMenu["etc"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 300);
                if (enemy != null)
                {
                    Orbwalker.DisableMovement = true;
                    Orbwalker.DisableAttacking = true;

                    E.Cast();
                    EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, enemy);
                    Orbwalker.DisableMovement = false;
                    Orbwalker.DisableAttacking = false;
                }
            }
            if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 0 &&
                EntityManager.Heroes.Enemies.Exists(
                    e => Player.Distance(e) < R.Range && e.HasBuffOfType(BuffType.Knockup)) && R.IsReady())
            {
                R.Cast();
            }
            if (SpellMenu["rcmode"].Cast<Slider>().CurrentValue == 1 &&
                EntityManager.Heroes.Enemies.Count(e => Player.Distance(e) < R.Range) >=
                SpellMenu["ramount"].Cast<Slider>().CurrentValue && R.IsReady())
            {
                R.Cast();
            }
        }

        private static void QLogic(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget(Q.Range) || Q.GetPrediction(target).HitChance < HitChance.Medium) return;
            Q.Cast(Q.GetPrediction(target).CastPosition);
        }
    }
}