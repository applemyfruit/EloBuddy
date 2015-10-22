using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace VolatileAIO.Organs.Brain
{
    internal class DrawManager : Heart
    {
        private bool _initialized;
        private Spell.SpellBase _q, _w, _e, _r;
        
        internal void UpdateValues(Spell.SpellBase q, Spell.SpellBase w, Spell.SpellBase e, Spell.SpellBase r)
        {
            if (!TickManager.NoLag(0)) return;
            if (!_initialized) _initialized = true;
            _q = q;
            _w = w;
            _e = e;
            _r = r;
        }

        protected override void Volative_OnDraw(EventArgs args)
        {
            if (!_initialized) return;
            var target = TargetManager.Target(1000, DamageType.Physical);
            if (target != null)
            {
                Drawing.DrawCircle(target.Position, 100, Color.Red);
            }

            const int width = 103;
            const int height = 9;
            const int xOffset = -11;
            const int yOffset = 17;
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
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
                var yPos = barPosition.Y + yOffset;
                var xPosDamage = barPosition.X + xOffset + width*hpleft;
                var xPosCurrentHp = barPosition.X + xOffset + width*enemy.Health/enemy.MaxHealth;
                var differenceInHp = xPosCurrentHp - xPosDamage;
                var pos1 = barPosition.X + xOffset + (107*hpleft);
                for (int i = 0; i < differenceInHp; i++)
                {
                    if (_q.IsReady() && i < drawQ*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Cyan);
                    else if (_w.IsReady() && i < (drawQ + drawW)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Orange);
                    else if (_e.IsReady() && i < (drawQ + drawW + drawE)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Yellow);
                    else if (_r.IsReady() && i < (drawQ + drawW + drawE + drawR)*differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.YellowGreen);
                }
            }
            if (VolatileMenu["debug"].Cast<CheckBox>().CurrentValue)
            {
                var w = Drawing.Width - 200;
                var h = (Drawing.Height/3)*1.5;
                float hitchance = 0;
                if (CastManager.CastCount != 0 && CastManager.HitCount != 0)
                    hitchance = (CastManager.HitCount/CastManager.CastCount)*100;
                Drawing.DrawText(w, (float) h, System.Drawing.Color.Red,
                    String.Format("Casted Spell.Q Count: {0}", CastManager.CastCount));
                Drawing.DrawText(w, (float) h + 20, System.Drawing.Color.Red,
                    String.Format("Hit Spell.Q Count: {0}", CastManager.HitCount));
                Drawing.DrawText(w, (float) h + 40, System.Drawing.Color.Red,
                    String.Format("Hitchance (%): {0}%",
                        CastManager.CastCount > 0
                            ? (((float) CastManager.HitCount/CastManager.CastCount)*100).ToString("00.00")
                            : "n/a"));
                Drawing.DrawText(w, (float) h + 60, Color.Red, String.Format("Ticks p/s: {0}", Game.TicksPerSecond));
                h /= 4;
                w /= 2;
                for (int index = 0; index < CastManager.Champions.Count; index++)
                {
                    var champ = CastManager.Champions[index];
                    float lowestdiff = 0, avgdiff = 0, highestdiff = 0;
                    foreach (var diff in champ.Differences)
                    {
                        if (lowestdiff == 0) lowestdiff = diff;
                        else if (diff < lowestdiff) lowestdiff = diff;
                        if (highestdiff == 0) highestdiff = diff;
                        else if (diff > highestdiff) highestdiff = diff;
                        avgdiff += diff;
                    }
                    avgdiff /= champ.Differences.Count;
                    Drawing.DrawText(w - (index*250), (float) h - 120, System.Drawing.Color.Red,
                        "Champ: " + champ.Name);
                    Drawing.DrawText(w - (index*250), (float) h - 100, System.Drawing.Color.Red,
                        "Spells Hit: " + champ.Differences.Count);
                    Drawing.DrawText(w - (index*250), (float) h - 80, System.Drawing.Color.Red,
                        "Highest difference: " + highestdiff);
                    Drawing.DrawText(w - (index*250), (float) h - 60, System.Drawing.Color.Red,
                        "Lowest difference: " + lowestdiff);
                    Drawing.DrawText(w - (index*250), (float) h - 40, System.Drawing.Color.Red,
                        "Average difference: " + avgdiff);

                }
            }
        }
    }
}