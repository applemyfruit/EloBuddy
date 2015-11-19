using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu.Values;

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
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000)
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
            if (ChosenTarget != null)
            {
                if (ChosenTarget != oldTarget)
                Chat.Print("Set Target: " + ChosenTarget.ChampionName);
            }
            else if (oldTarget!=null)
                Chat.Print("Reset target");
        }

        public static AIHeroClient Target(Spell.SpellBase spell, DamageType damageType)
        {
            TargetSelector.ActiveMode = TargetSelectorMode.Auto;
            if (TargetMenu["chosenignores"].Cast<CheckBox>().CurrentValue && ChosenTarget != null)
                return ChosenTarget;

            if (ChosenTarget != null && ChosenTarget.Distance(Player) < spell.Range*1.2) return ChosenTarget;
            return TargetSelector.GetTarget(spell.Range, damageType);
        }

        public static AIHeroClient Target(int range, DamageType damageType)
        {
            TargetSelector.ActiveMode = TargetSelectorMode.Auto;
            if (TargetMenu["chosenignores"].Cast<CheckBox>().CurrentValue && ChosenTarget != null)
                return ChosenTarget;

            if (ChosenTarget != null && ChosenTarget.Distance(Player) < range * 1.2) return ChosenTarget;
            return TargetSelector.GetTarget(range, damageType);
        }
    }

}
