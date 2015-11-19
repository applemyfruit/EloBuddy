using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using VolatileAIO.Organs.Brain.Data;
using VolatileAIO.Organs._Test;
using Color = System.Drawing.Color;

namespace VolatileAIO.Organs.Brain
{
    internal class DrawManager : Heart
    {
        private bool _initialized;
        private Spell.SpellBase _q, _w, _e, _r;

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
            if (!_initialized)
            {
                UpdateValues();
                return;
            }
            var target = TargetManager.Target(1000, DamageType.Physical);
            if (target != null)
            {
                Drawing.DrawCircle(target.Position, 100, Color.Red);
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

                if (_q.IsReady()) drawQ = (Player.GetSpellDamage(enemy, SpellSlot.Q) / drawDamage);
                if (_w.IsReady()) drawW = (Player.GetSpellDamage(enemy, SpellSlot.W) / drawDamage);
                if (_e.IsReady()) drawE = (Player.GetSpellDamage(enemy, SpellSlot.E) / drawDamage);
                if (_r.IsReady()) drawR = (Player.GetSpellDamage(enemy, SpellSlot.R) / drawDamage);

                var hpleft = Math.Max(0, enemy.Health - drawDamage) / enemy.MaxHealth;
                var yPos = barPosition.Y + enemy.HPBarYOffset + 5;
                var xPosDamage = barPosition.X + xOffset + width * hpleft;
                var xPosCurrentHp = barPosition.X + xOffset + width * enemy.Health / enemy.MaxHealth;
                var differenceInHp = xPosCurrentHp - xPosDamage;
                var pos1 = barPosition.X + xOffset + (107 * hpleft);
                for (var i = 0; i < differenceInHp; i++)
                {
                    if (_q.IsReady() && i < drawQ * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Cyan);
                    else if (_w.IsReady() && i < (drawQ + drawW) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Orange);
                    else if (_e.IsReady() && i < (drawQ + drawW + drawE) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.Yellow);
                    else if (_r.IsReady() && i < (drawQ + drawW + drawE + drawR) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, Color.YellowGreen);
                }
            }
        }

        private static void DrawDebug()
        {
            //CursorPos
            Drawing.DrawText(Game.CursorPos2D.X, Game.CursorPos2D.Y - 20, Color.Red, Game.CursorPos2D.X + "," + Game.CursorPos2D.Y);
        }


        protected override void Volative_OnDrawEnd(EventArgs args)
        {
            DrawDamageIndicator();
            DrawRecalls();
            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
            {
               DrawDebug();
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