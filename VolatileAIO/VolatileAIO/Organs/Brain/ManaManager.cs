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
                        ManaQ = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.W:
                        ManaW = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.E:
                        ManaE = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                    case SpellSlot.R:
                        ManaR = spell.SData.ManaCostArray[spell.Level - 1];
                        break;
                }
            }
        }
    }
}