using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using VolatileAIO.Organs.Brain.Data;
using System.Windows;
using EloBuddy.SDK.Menu;
using Color = System.Drawing.Color;

namespace VolatileAIO.Organs.Brain
{
    internal class DrawManager : Heart
    {
        private bool _initialized;
        private Spell.SpellBase _q, _w, _e, _r;
        private Menu DrawMenu;

        public DrawManager()
        {
            DrawMenu = VolatileMenu.AddSubMenu("Drawings", "drawings", "Volatile Drawings");
            DrawMenu.Add("dmg", new CheckBox("Draw Volatile DamageIndicator"));
            DrawMenu.Add("rl", new CheckBox("Draw Volatile RangeLines"));
            DrawMenu.Add("recall", new CheckBox("Draw Recalls"));
        }

        internal void UpdateValues()
        {
            if (!TickManager.NoLag(0) || PlayerData.Spells.Count == 0) return;
            if (!_initialized) _initialized = true;
            _q = PlayerData.Spells.Find(s => s.Slot == SpellSlot.Q);
            _w = PlayerData.Spells.Find(s => s.Slot == SpellSlot.W);
            _e = PlayerData.Spells.Find(s => s.Slot == SpellSlot.E);
            _r = PlayerData.Spells.Find(s => s.Slot == SpellSlot.R);
        }

        private void DrawDamageIndicator()
        {
            var target = TargetManager.Target(1000, DamageType.Physical);
            if (target != null)
            {
                new Circle {Color = Color.Cyan, Radius = 100, BorderWidth = 2f}.Draw(target.Position);
            }

            const int width = 103;
            const int height = 9;
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                var xOffset = enemy.HPBarXOffset;
                if (!enemy.IsHPBarRendered) continue;
                var barPosition = enemy.HPBarPosition;
                float drawDamage = 0, drawQ = 0, drawW = 0, drawE = 0, drawR = 0;

                if (_q.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.Q);
                if (_w.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.W);
                if (_e.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.E);
                if (_r.IsReady()) drawDamage += Player.GetSpellDamage(enemy, SpellSlot.R);

                if (_q.IsReady()) drawQ = (Player.GetSpellDamage(enemy, SpellSlot.Q)/drawDamage);
                if (_w.IsReady()) drawW = (Player.GetSpellDamage(enemy, SpellSlot.W)/drawDamage);
                if (_e.IsReady()) drawE = (Player.GetSpellDamage(enemy, SpellSlot.E)/drawDamage);
                if (_r.IsReady()) drawR = (Player.GetSpellDamage(enemy, SpellSlot.R)/drawDamage);

                var hpleft = Math.Max(0, enemy.Health - drawDamage)/enemy.MaxHealth;
                var yPos = barPosition.Y + enemy.HPBarYOffset + 5;
                var xPosDamage = barPosition.X + xOffset + width*hpleft;
                var xPosCurrentHp = barPosition.X + xOffset + width*enemy.Health/enemy.MaxHealth;
                var differenceInHp = xPosCurrentHp - xPosDamage;
                var pos1 = barPosition.X + xOffset + (107*hpleft);
                for (var i = 0; i < differenceInHp; i++)
                {
                    if (_q.IsReady() && i < drawQ*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Turquoise);
                    else if (_w.IsReady() && i < (drawQ + drawW)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Chartreuse);
                    else if (_e.IsReady() && i < (drawQ + drawW + drawE)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Gold);
                    else if (_r.IsReady() && i < (drawQ + drawW + drawE + drawR)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.OrangeRed);
                }
            }
        }

        private static void DrawDebug()
        {
            var angle = Math.Atan2(Game.CursorPos.Y - Player.Position.Y, Game.CursorPos.X - Player.Position.X);
            var sin = (Math.Sin(angle)*300) + Player.Position.Y;
            var cosin = (Math.Cos(angle)*300) + Player.Position.X;
            var cursor = new Vector3((float) cosin, (float) sin, Player.Position.Z);
            Drawing.DrawText(Game.CursorPos2D.X, Game.CursorPos2D.Y - 20, Color.Red,
                Game.CursorPos.X + "," + Game.CursorPos.Y);
            Drawing.DrawLine(Player.Position.WorldToScreen(), Game.CursorPos2D, 1, Color.DarkRed);
            Drawing.DrawText(Player.Position.WorldToScreen().X, Player.Position.WorldToScreen().Y + 10, Color.Red,
                Player.Position.X + "," + Player.Position.Y + Environment.NewLine +
                angle*(180.0/Math.PI));
            Drawing.DrawCircle(cursor, 40, Color.Aqua);
        }

        protected override void Volative_OnDrawEnd(EventArgs args)
        {
            if (!_initialized)
            {
                UpdateValues();
                return;
            }
            if (DrawMenu["dmg"].Cast<CheckBox>().CurrentValue)
                DrawDamageIndicator();
            if (DrawMenu["recall"].Cast<CheckBox>().CurrentValue)
                DrawRecalls();
            if (DrawMenu["rl"].Cast<CheckBox>().CurrentValue)
                DrawRangeLines();
            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
            {
                DrawDebug();
            }
        }

        private static void DrawRangeLines()
        {
            if (!PlayerData.Spells.Any(s=>s.IsLearned)) return;
            uint[] maxrange = {PlayerData.Spells.Where(s => s.IsLearned).Max(s => s.Range)};
            if (maxrange[0] == 0)
                try
                {
                    foreach (var ss in PlayerData.Spells.Where(s => s.IsLearned).Cast<Spell.Skillshot>().Where(ss => ss.Width > maxrange[0]))
                    {
                        maxrange[0] = (uint)ss.Width;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            foreach (
                var hero in
                    EntityManager.Heroes.Enemies.Where(
                        e => e.Distance(Player) < maxrange[0] && !e.IsDead && e.IsValid && e.IsVisible && e.IsValidTarget(maxrange[0])))
            {
                var spellranges = new Dictionary<uint, List<SpellSlot>>();
                var s = from spell in PlayerData.Spells where spell.IsLearned select spell.Range;
                var spells = s.ToList();
                spells.Sort();

                foreach (var spell in spells)
                {
                    var range = (uint) 0;
                    if (spell > 0)
                        range = spell;
                    else
                    {
                        try
                        {
                            var ss = (Spell.Skillshot) PlayerData.Spells.Find(sp => sp.Range == spell);
                            range = (uint) ss.Width;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                    if (range < 50) continue;
                    if (range > hero.Distance(Player))
                        if (spellranges.ContainsKey((uint) hero.Distance(Player)))
                            spellranges[(uint) hero.Distance(Player)].Add(
                                PlayerData.Spells.Find(sp => sp.Range == spell).Slot);
                        else
                            spellranges.Add((uint) hero.Distance(Player),
                                new List<SpellSlot> {PlayerData.Spells.Find(sp => sp.Range == spell).Slot});
                    else if (spellranges.ContainsKey(range))
                        spellranges[range].Add(PlayerData.Spells.Find(sp => sp.Range == spell).Slot);
                    else
                        spellranges.Add(range,
                            new List<SpellSlot> {PlayerData.Spells.Find(sp => sp.Range == spell).Slot});
                }

                var i = 0;
                var lastloc = new Vector3(0, 0, 0);
                foreach (var range in spellranges)
                {
                    var angle = Math.Atan2(hero.Position.Y - Player.Position.Y, hero.Position.X - Player.Position.X);
                    var sin = (Math.Sin(angle)*(range.Key - 20)) + Player.Position.Y;
                    var cosin = (Math.Cos(angle)*(range.Key - 20)) + Player.Position.X;
                    var location = new Vector3((float) cosin, (float) sin, Player.Position.Z);
                    if (i == 0)
                    {
                        Drawing.DrawLine(Player.Position.WorldToScreen(), location.WorldToScreen(), 2, Color.Red);
                        sin = (Math.Sin(angle)*(range.Key)) + Player.Position.Y;
                        cosin = (Math.Cos(angle)*(range.Key)) + Player.Position.X;
                        location = new Vector3((float) cosin, (float) sin, Player.Position.Z);
                        if (PlayerData.Spells.Find(sp => range.Value.Contains(sp.Slot)).IsReady())
                            new Circle
                            {
                                Color = Color.Chartreuse,
                                Radius = 20,
                                BorderWidth = 1f
                            }.Draw(location);
                        else
                            new Circle
                            {
                                Color = Color.Firebrick,
                                Radius = 20,
                                BorderWidth = 1f
                            }.Draw(location);
                        string text = "";
                        text = range.Value.Aggregate(text, (current, spell) => current + spell);
                        Drawing.DrawText(location.WorldToScreen().X, location.WorldToScreen().Y - (float) 7.5,
                            Color.White,
                            text, 15);
                        i++;
                        sin = (Math.Sin(angle)*(range.Key + 20)) + Player.Position.Y;
                        cosin = (Math.Cos(angle)*(range.Key + 20)) + Player.Position.X;
                        lastloc = new Vector3((float) cosin, (float) sin, Player.Position.Z);
                    }
                    else
                    {
                        Drawing.DrawLine(lastloc.WorldToScreen(), location.WorldToScreen(), 2, Color.Red);
                        sin = (Math.Sin(angle)*(range.Key)) + Player.Position.Y;
                        cosin = (Math.Cos(angle)*(range.Key)) + Player.Position.X;
                        location = new Vector3((float) cosin, (float) sin, Player.Position.Z);
                        if (PlayerData.Spells.Find(sp => range.Value.Contains(sp.Slot)).IsReady())
                            new Circle()
                            {
                                Color = Color.Chartreuse,
                                Radius = 20,
                                BorderWidth = 2f
                            }.Draw(location);
                        else
                            new Circle()
                            {
                                Color = Color.Firebrick,
                                Radius = 20,
                                BorderWidth = 2f
                            }.Draw(location);
                        string text = "";
                        text = range.Value.Aggregate(text, (current, spell) => current + spell);
                        Drawing.DrawText(location.WorldToScreen().X, location.WorldToScreen().Y - (float) 7.5,
                            Color.White,
                            text, 15);
                        i++;
                        sin = (Math.Sin(angle)*(range.Key + 20)) + Player.Position.Y;
                        cosin = (Math.Cos(angle)*(range.Key + 20)) + Player.Position.X;
                        lastloc = new Vector3((float) cosin, (float) sin, Player.Position.Z);
                    }
                }
            }
        }

        private static void DrawRecalls()
        {
            if (!RecallTracker.Recalls.Any()) return;
            var i = 0;
            RecallTracker.Recall removeme = null;
            foreach (var recall in RecallTracker.Recalls)
            {
                var y = RecallTracker.Y() - i;
                var y2 = y + 15;

                Drawing.DrawLine(RecallTracker.X(), y,
                    RecallTracker.X() +
                    (recall.PercentComplete()*HackMenu["recallwidth"].Cast<Slider>().CurrentValue/100), y, 16,
                    !recall.Hero.IsAlly ? Color.DarkRed : Color.DarkGreen);

                var boxVectors = new Vector2[5];
                boxVectors[0] = new Vector2(RecallTracker.X(), y2 - 8);
                boxVectors[1] = new Vector2(RecallTracker.X(), y2 + 8);
                boxVectors[2] = new Vector2(
                    RecallTracker.X() + HackMenu["recallwidth"].Cast<Slider>().CurrentValue, y2 + 8);
                boxVectors[3] = new Vector2(
                    RecallTracker.X() + HackMenu["recallwidth"].Cast<Slider>().CurrentValue, y2 - 8);

                boxVectors[4] = new Vector2(RecallTracker.X(), y2 - 8);
                Line.DrawLine(Color.White, boxVectors);

                var recallString = "";

                if (recall.IsAborted)
                    recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%" + " - Aborted!";
                else if (recall.PercentComplete() > 99.99)
                    recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%" + " - Finished!";
                else
                    recallString =
                        recall.Hero.ChampionName + " - " + recall.PercentComplete() + "%";

                Drawing.DrawText(RecallTracker.X() + 10, y + 8, Color.White, recallString);

                if (recall.ExpireTime < Environment.TickCount)
                {
                    removeme = recall;
                }
                i += 20;
            }
            if (removeme != null) RecallTracker.Recalls.Remove(removeme);
        }
    }
}