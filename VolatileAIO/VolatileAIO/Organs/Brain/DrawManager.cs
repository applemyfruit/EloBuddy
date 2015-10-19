using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace VolatileAIO.Organs.Brain
{
    class DrawManager : Heart
    {
        private static bool _initialized = false;
        private Spell.SpellBase _q, _w, _e, _r;

        public void UpdateValues(Spell.SpellBase q, Spell.SpellBase w, Spell.SpellBase e, Spell.SpellBase r)
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
            var target = new TargetManager().Target(1000, DamageType.Physical);
            if (target != null)
            {
                Drawing.DrawCircle(target.Position, 100, Color.Red);
            }

            if (!_initialized) return;
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

                if (_q.IsReady()) drawQ = (Player.GetSpellDamage(enemy, SpellSlot.Q) / drawDamage);
                if (_w.IsReady()) drawW = (Player.GetSpellDamage(enemy, SpellSlot.W) / drawDamage);
                if (_e.IsReady()) drawE = (Player.GetSpellDamage(enemy, SpellSlot.E) / drawDamage);
                if (_r.IsReady()) drawR = (Player.GetSpellDamage(enemy, SpellSlot.R) / drawDamage);

                var hpleft = Math.Max(0, enemy.Health - drawDamage) / enemy.MaxHealth;
                var yPos = barPosition.Y + yOffset;
                var xPosDamage = barPosition.X + xOffset + width * hpleft;
                var xPosCurrentHp = barPosition.X + xOffset + width * enemy.Health / enemy.MaxHealth;
                var differenceInHp = xPosCurrentHp - xPosDamage;
                var pos1 = barPosition.X + xOffset + (107 * hpleft);
                for (int i = 0; i < differenceInHp; i++)
                {
                    if (_q.IsReady() && i < drawQ * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Cyan);
                    else if (_w.IsReady() && i < (drawQ + drawW) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Orange);
                    else if (_e.IsReady() && i < (drawQ  + drawW + drawE) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.Yellow);
                    else if (_r.IsReady() && i < (drawQ + drawW + drawE + drawR) * differenceInHp)
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + height, 1, System.Drawing.Color.YellowGreen);
                }
            }
        }
    }
}