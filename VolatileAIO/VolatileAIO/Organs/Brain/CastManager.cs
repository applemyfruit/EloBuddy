using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Utils;

namespace VolatileAIO.Organs.Brain
{
    class CastManager : Heart
    {
        private bool _isAutoAttacking = false;

        public void CastSkillShot(Spell.Skillshot spell, DamageType damageType, HitChance hitChance = HitChance.Medium, Obj_AI_Base targetHero = null)
        {
            if ((spell.Slot == SpellSlot.Q && TickManager.NoLag(1)) || (spell.Slot == SpellSlot.W && TickManager.NoLag(2)) ||
                (spell.Slot == SpellSlot.E && TickManager.NoLag(3)) || (spell.Slot == SpellSlot.R && TickManager.NoLag(4)))
            {
                if (spell.IsReady() && !_isAutoAttacking)
                {
                    var target = new TargetManager().Target(spell, damageType);
                    if (target == null) return;
                    if (target.IsValidTarget(spell.Range) && spell.GetPrediction(target).HitChance >= hitChance)
                        spell.Cast(target);
                }
            }
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            _isAutoAttacking = false;
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            _isAutoAttacking = true;
        }

    }
}