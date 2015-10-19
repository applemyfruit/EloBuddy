using EloBuddy;
using EloBuddy.SDK;

namespace VolatileAIO.Organs.Brain
{
    internal class ManaManager : Heart
    {
        public float ManaQ = 0, ManaW = 0, ManaE = 0, ManaR = 0;

        public void SetMana()
        {
            if (!TickManager.NoLag(0)) return;
            foreach (var spell in Player.Spellbook.Spells)
            {
                switch (spell.Slot)
                {
                    case SpellSlot.Q:
                        if (spell.Level > 0) ManaQ = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.W:
                        if (spell.Level > 0) ManaW = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.E:
                        if (spell.Level > 0) ManaE = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.R:
                        if (spell.Level > 0) ManaR = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                }
            }
        }
    }
}