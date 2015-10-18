using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace VolatileAIO.Organs.Brain
{
    class TargetManager : Heart
    {
        public static AIHeroClient ChosenTarget;

        public static void SetChosenTarget(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowMessages.LeftButtonDown)
            {
                return;
            }
            AIHeroClient oldTarget = null;
            if (ChosenTarget != null)
            {
                oldTarget = ChosenTarget;
            }
            ChosenTarget =
                EntityManager.Heroes.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
            if (ChosenTarget != null)
            {
                if (ChosenTarget != oldTarget)
                Chat.Print("Set Target: " + ChosenTarget.ChampionName);
            }
            else if (oldTarget!=null)
                Chat.Print("Reset target");
        }

        public AIHeroClient Target(Spell.SpellBase spell, DamageType damageType)
        {
            if (ChosenTarget != null && ChosenTarget.Distance(Player) < spell.Range*1.2) return ChosenTarget;
            return TargetSelector.GetTarget(spell.Range, damageType);
        }
    }

}
