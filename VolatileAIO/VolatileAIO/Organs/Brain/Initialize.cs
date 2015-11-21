using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using VolatileAIO.Organs._Test;

namespace VolatileAIO.Organs.Brain
{
    internal class Initialize : Heart
    {
        public enum Type
        {
            Targeted,
            Active,
            Skillshot
        }

        public List<Spell.SpellBase> Spells(Type q, Type w, Type e, Type r)
        {
            Spell.SpellBase qBase = null;
            var firstOrDefault = Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.Q);
            var qData = firstOrDefault.SData;
            var qData2 =
                SpellDatabase.Spells.Find(
                    s =>
                        string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) &&
                        s.Slot == SpellSlot.Q);

            Spell.SpellBase wBase = null;
            firstOrDefault = Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.W);
            var wData = firstOrDefault.SData;
            var wData2 =
                SpellDatabase.Spells.Find(
                    s =>
                        string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) &&
                        s.Slot == SpellSlot.W);

            Spell.SpellBase eBase = null;
            firstOrDefault = Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.E);
            var eData = firstOrDefault.SData;
            var eData2 =
                SpellDatabase.Spells.Find(
                    s =>
                        string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) &&
                        s.Slot == SpellSlot.E);

            Spell.SpellBase rBase = null;
            firstOrDefault = Player.Spellbook.Spells.Find(s => s.Slot == SpellSlot.R);
            var rData = firstOrDefault.SData;
            var rData2 =
                SpellDatabase.Spells.Find(
                    s =>
                        string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) &&
                        s.Slot == SpellSlot.R);

            switch (q)
            {
                case Type.Active:
                    qBase = new Spell.Active(SpellSlot.Q, (uint) qData.CastRange);
                    break;
                case Type.Skillshot:
                    qBase = new Spell.Skillshot(SpellSlot.Q, Convert.ToUInt32(qData2.Range * 0.95), qData2.Type, qData2.Delay,
                        qData2.MissileSpeed, qData2.Radius);
                    break;
                case Type.Targeted:
                    qBase = new Spell.Targeted(SpellSlot.Q, (uint) qData.CastRange);
                    break;
            }

            switch (w)
            {
                case Type.Active:
                    wBase = new Spell.Active(SpellSlot.W, (uint) wData.CastRange);
                    break;
                case Type.Skillshot:
                    wBase = new Spell.Skillshot(SpellSlot.W, Convert.ToUInt32(wData2.Range * 0.95), wData2.Type, wData2.Delay,
                        wData2.MissileSpeed, wData2.Radius);
                    break;
                case Type.Targeted:
                    wBase = new Spell.Targeted(SpellSlot.W, (uint) wData.CastRange);
                    break;
            }

            switch (e)
            {
                case Type.Active:
                    eBase = new Spell.Active(SpellSlot.E, (uint) eData.CastRange);
                    break;
                case Type.Skillshot:
                    eBase = new Spell.Skillshot(SpellSlot.E, Convert.ToUInt32(eData2.Range*0.95), eData2.Type, eData2.Delay,
                        eData2.MissileSpeed, eData2.Radius);
                    break;
                case Type.Targeted:
                    eBase = new Spell.Targeted(SpellSlot.E, (uint) eData.CastRange);
                    break;
            }

            switch (r)
            {
                case Type.Active:
                    rBase = new Spell.Active(SpellSlot.R, (uint) rData.CastRange);
                    break;
                case Type.Skillshot:
                    rBase = new Spell.Skillshot(SpellSlot.R, Convert.ToUInt32(rData2.Range * 0.95), rData2.Type, rData2.Delay,
                        rData2.MissileSpeed, rData2.Radius);
                    break;
                case Type.Targeted:
                    rBase = new Spell.Targeted(SpellSlot.R, (uint) rData.CastRange);
                    break;
            }

            return new List<Spell.SpellBase>
            {
                qBase,
                wBase,
                eBase,
                rBase
            };
        }
    }
}