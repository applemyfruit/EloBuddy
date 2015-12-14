using System;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs.Brain.Data;
using VolatileAIO.Organs.Brain.Utils;

namespace VolatileAIO.Extensions.Support
{
    internal class Alistar : Heart
    {
        private static Spell.Active _q;
        private static Spell.Targeted _w;
        private static Spell.Active _e;
        private static Spell.Active _r;
        private static SpellDataInst _flash;
        private static bool _avoidSpam;

        public static Menu SpellMenu;

        public Alistar()
        {
            InitializeMenu();
            InitializeSpells();
            OnDraw += InitiateDrawMenu;
        }

        private void InitiateDrawMenu()
        {
            DrawManager.DrawMenu.AddGroupLabel(Player.ChampionName);
            DrawManager.DrawMenu.Add("dwq", new CheckBox("Draw W/Q hitamount on enemies"));
            DrawManager.DrawMenu.Add("dfq", new CheckBox("Draw FlashQ (HAP) hitamount"));
        }

        private static void InitializeMenu()
        {
            SpellMenu = VolatileMenu.AddSubMenu("Spell Menu", "spellmenu");

            SpellMenu.AddGroupLabel("W/Q Settings");
            SpellMenu.Add("wqtc", new CheckBox("Use W/Q in Combo"));
            SpellMenu.Add("wqhap", new CheckBox("W/Q Highest Amount Priority"));
            SpellMenu.Add("awq", new CheckBox("Auto W/Q if can hit:", false));
            SpellMenu.Add("awqa", new Slider("At Least", 4, 1, 5));
            SpellMenu.Add("qcd", new CheckBox("Use Q if W is on CD"));
            SpellMenu.Add("qth", new CheckBox("Use Q in Harass"));
            SpellMenu.Add("qthcc", new CheckBox("Use Q in Harass only to chain cc", false));
            SpellMenu.AddSeparator();
            SpellMenu.Add("flashq", new KeyBind("Flash Q (Target Priority)", false, KeyBind.BindTypes.HoldActive, 'T'));
            SpellMenu.Add("flashqhap",
                new KeyBind("Flash Q (Highest Amount Priority)", false, KeyBind.BindTypes.HoldActive, 'Z'));
            SpellMenu.Add("fqamount", new Slider("Minimum enemies hit to Flash Q (HAP)", 2, 1, 5));

            SpellMenu.AddGroupLabel("E Settings");
            SpellMenu.Add("autoe", new CheckBox("Auto E", false));
            SpellMenu.Add("minhp", new Slider("E when %HP less than", 70));
            SpellMenu.Add("minmana", new Slider("E when %MP more than", 60));

            SpellMenu.AddGroupLabel("R Settings");
            SpellMenu.Add("user", new CheckBox("Use R", false));
            SpellMenu.Add("ramount", new Slider("Enemies in range", 3, 1, 5));
            SpellMenu.Add("rhealth", new Slider("Max %HP to ult", 50));
        }

        public void InitializeSpells()
        {
            PlayerData.Spells = new Initialize().Spells(Initialize.Type.Active, Initialize.Type.Targeted,
                Initialize.Type.Active, Initialize.Type.Active);
            _q = (Spell.Active) PlayerData.Spells[0];
            _w = (Spell.Targeted) PlayerData.Spells[1];
            _e = (Spell.Active) PlayerData.Spells[2];
            _r = (Spell.Active) PlayerData.Spells[3];
            _flash = EloBuddy.Player.Spells.FirstOrDefault(f => f.Name.ToLower() == "summonerflash");
        }

        protected override void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if ((Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Combo ||
                    (SpellMenu["awq"].Cast<CheckBox>().CurrentValue &&
                     QHits((AIHeroClient)args.Target) > SpellMenu["awqa"].Cast<Slider>().CurrentValue)) || !_q.IsReady() ||
                   !sender.Owner.IsMe ||
                   args.Slot != SpellSlot.W || args.Process == false)
                return;

            var qdelay = Math.Max(0, Player.Distance(args.Target) - 500) * 10 / 25;
            //qdelay = Math.Max(0, Player.Distance(args.Target) - 365) / 1.5f;
            Core.DelayAction(() => _q.Cast(), (int)qdelay);
        }
        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            FlashQ();
            if (ComboActive()) Combo();
            if (HarassActive()) Harass();
            if (LaneClearActive()) JungleClear();
            AutoCast();
        }

        private static void JungleClear()
        {
            if (
                MinionManager.GetMinions(Player.Position, _q.Range*(float) 1.5, MinionTypes.All, MinionTeam.Neutral)
                    .Any() &&
                MinionManager.GetMinions(Player.Position, _q.Range, MinionTypes.All, MinionTeam.Neutral).Count ==
                MinionManager.GetMinions(Player.Position, _q.Range*(float) 1.5, MinionTypes.All, MinionTeam.Neutral)
                    .Count && !CastManager.IsAutoAttacking)
            {
                _q.Cast();
            }
        }

        private static void FlashQ()
        {
            if (SpellMenu["flashq"].Cast<KeyBind>().CurrentValue)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var target = TargetManager.Target(450, DamageType.Magical);
                if (target == null || !target.IsValidTarget((int) _flash.SData.CastRange) || target.Distance(Player)<450|| !TickManager.NoLag(1) ||
                    !_q.IsReady() || _flash == null) return;
                var champs =
                    EntityManager.Heroes.Enemies.Where(e => e.Distance(target) < _q.Range)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, _q.CastDelay))
                        .ToList();
                var location = CastManager.GetOptimizedCircleLocation(champs, _q.Range, _flash.SData.CastRange);
                Player.Spellbook.CastSpell(_flash.Slot,
                    location.ChampsHit > 1 ? location.Position.To3DWorld() : target.Position);
                _q.Cast();
            }
            else if (SpellMenu["flashqhap"].Cast<KeyBind>().CurrentValue)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if (EntityManager.Heroes.Enemies.Count(e => e.Distance(Player) < _flash.SData.CastRange) <
                    SpellMenu["fqamount"].Cast<Slider>().CurrentValue || !TickManager.NoLag(1) ||
                    !_q.IsReady() || _flash == null)
                    return;
                var champs =
                    EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < _flash.SData.CastRange)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, _q.CastDelay))
                        .ToList();
                var location = CastManager.GetOptimizedCircleLocation(champs, _q.Range, _flash.SData.CastRange);
                if (location.ChampsHit < SpellMenu["fqamount"].Cast<Slider>().CurrentValue) return;
                Player.Spellbook.CastSpell(_flash.Slot, location.Position.To3DWorld());
                _q.Cast();
            }
        }

        private static void AutoCast()
        {
            if (SpellMenu["user"].Cast<CheckBox>().CurrentValue &&
                Player.CountEnemiesInRange(_w.Range) >= SpellMenu["ramount"].Cast<Slider>().CurrentValue
                && (Player.Health/Player.MaxHealth)*100 >= SpellMenu["rhealth"].Cast<Slider>().CurrentValue &&
                TickManager.NoLag(4))
            {
                _r.Cast();
            }
            if (SpellMenu["autoe"].Cast<CheckBox>().CurrentValue &&
                Player.HealthPercent < SpellMenu["minhp"].Cast<Slider>().CurrentValue &&
                Player.ManaPercent > SpellMenu["minmana"].Cast<Slider>().CurrentValue &&
                Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Combo && !Player.IsRecalling() &&
                !InFountain(Player) && TickManager.NoLag(3))
                _e.Cast();
            if (SpellMenu["awq"].Cast<CheckBox>().CurrentValue && _w.IsReady() && _q.IsReady())
            {
                var enemies = EntityManager.Heroes.Enemies;
                var target = enemies.OrderByDescending(QHits).First();
                if (QHits(target) > SpellMenu["awqa"].Cast<Slider>().CurrentValue && target.IsValidTarget(_w.Range * (float)0.95) &&
                    Player.Mana > (ManaManager.GetMana(SpellSlot.Q) + ManaManager.GetMana(SpellSlot.W))
                    && TickManager.NoLag(2))
                {
                    _w.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetManager.Target(_q, DamageType.Magical);
            if (!_q.IsReady() || !TickManager.NoLag(1) || !SpellMenu["qth"].Cast<CheckBox>().CurrentValue ||
                target == null || !target.IsValidTarget(_q.Range)) return;
            if (SpellMenu["qthcc"].Cast<CheckBox>().CurrentValue && !_avoidSpam)
            {
                var targetcc = target.Buffs.Where(b => b.IsKnockup || b.IsRoot || b.IsStunOrSuppressed).ToList();
                if (!targetcc.Any()) return;
                var longest = (int) targetcc.Max(cc => cc.EndTime);
                Core.DelayAction(() => StackCC_Part2(target), longest - (int) Game.Time);
                _avoidSpam = true;
            }
            else
            {
                _q.Cast();
            }
        }

        private static void StackCC_Part2(AIHeroClient target)
        {
            if (!_q.IsReady() || !target.IsValidTarget(_q.Range)) return;
            _q.Cast();
            _avoidSpam = false;
        }

        private static void Combo()
        {
            if (_w.IsReady() && _q.IsReady() && SpellMenu["wqtc"].Cast<CheckBox>().CurrentValue)
            {
                if (SpellMenu["wqhap"].Cast<CheckBox>().CurrentValue)
                {
                    var enemies = EntityManager.Heroes.Enemies;
                    var target = enemies.OrderByDescending(QHits).First();
                    if (QHits(target) < 2) target = TargetManager.Target(_w, DamageType.Magical);
                    if (target.IsValidTarget(_w.Range*(float) 0.95) && ValidSpell(target) &&
                        Player.Mana > (ManaManager.GetMana(SpellSlot.Q) + ManaManager.GetMana(SpellSlot.W))
                        && TickManager.NoLag(2))
                    {
                        _w.Cast(target);
                    }
                }
                else
                {
                    var target = TargetManager.Target(_w, DamageType.Magical);
                    if (target != null && ValidSpell(target) && target.IsValidTarget(_w.Range*(float) 0.95) &&
                        Player.Mana > (ManaManager.GetMana(SpellSlot.Q) + ManaManager.GetMana(SpellSlot.W))
                        && TickManager.NoLag(2))
                    {
                        _w.Cast(target);
                    }
                }
            }
            else if (SpellMenu["qcd"].Cast<CheckBox>().CurrentValue && _q.IsReady() && TickManager.NoLag(1))
            {
                var target = TargetManager.Target(_q, DamageType.Magical);
                if (target != null && target.IsValidTarget(_q.Range))
                    _q.Cast();
            }
        }

        private static int QHits(AIHeroClient target)
        {
            return EntityManager.Heroes.Enemies.Count(e => e.Distance(target) < 340 && ValidSpell(e));
        }

        protected override void Volative_OnDraw(EventArgs args)
        {
            if (Drawinit && DrawManager.DrawMenu["dwq"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(e => e.Distance(Player) < _w.Range))
                {
                    Drawing.DrawText(enemy.Position.WorldToScreen().X, enemy.Position.WorldToScreen().Y + 40,
                        Color.White,
                        "W/Q hits: : " + QHits(enemy));
                }
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            e => e.Distance(Player) < _w.Range*1.6 && e.Distance(Player) > _w.Range))
                {
                    Drawing.DrawText(enemy.Position.WorldToScreen().X, enemy.Position.WorldToScreen().Y + 40, Color.Red,
                        "W/Q hits: " + QHits(enemy));
                }
            }
            if (Drawinit && DrawManager.DrawMenu["dfq"].Cast<CheckBox>().CurrentValue)
            Drawing.DrawText(Player.Position.WorldToScreen().X - 75, Player.Position.WorldToScreen().Y + 40, Color.White,
                "FlashQ (HAP) can hit: " +
                CastManager.GetOptimizedCircleLocation(
                    EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget() && e.IsValid && e.Distance(Player) < 450)
                        .Select(champ => Prediction.Position.PredictUnitPosition(champ, _q.CastDelay))
                        .ToList(), _q.Range, _flash.SData.CastRange).ChampsHit);
        }
    }
}