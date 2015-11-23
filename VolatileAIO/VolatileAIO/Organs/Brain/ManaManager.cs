using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using VolatileAIO.Organs.Brain.Data;
using VolatileAIO.Organs._Test;

namespace VolatileAIO.Organs.Brain
{
    internal class ManaManager : Heart
    {
        private static SpellSlot _one = SpellSlot.Unknown;
        private static SpellSlot _two = SpellSlot.Unknown;
        private static SpellSlot _three = SpellSlot.Unknown;
        private static SpellSlot _four = SpellSlot.Unknown;
        private static SpellSlot _lastBlocked = SpellSlot.Unknown;
        private static float _lastBlockedTime=0;
        internal static Menu MmMenu;

        public float GetMana(SpellSlot slot)
        {
            var spell = Player.Spellbook.Spells.Find(s => s.Slot == slot);
            return spell.Level > 0 ? spell.SData.ManaCostArray[spell.Level] : 0;
        }

        private static bool PrioritiesAreSet()
        {
            if (MmMenu["s2"].Cast<Slider>().CurrentValue == MmMenu["s3"].Cast<Slider>().CurrentValue)
                return false;
            if (MmMenu["s2"].Cast<Slider>().CurrentValue == MmMenu["s4"].Cast<Slider>().CurrentValue)
                return false;
            if (MmMenu["s3"].Cast<Slider>().CurrentValue == MmMenu["s4"].Cast<Slider>().CurrentValue)
                return false;
            return true;
        }

        public ManaManager()
        {   
            MmMenu = VolatileMenu.AddSubMenu("Mana Manager (beta)", "manamanager", "Volatile Mana Manager (beta)");
            MmMenu.AddLabel(
                "Mana Manager will make sure you dont use a lower priority spell if it doesnt leave you" +
                Environment.NewLine +
                "with enough mana for higher priority spells.");
            MmMenu.Add("manamanager", new CheckBox("Use Mana Manager", false));
            MmMenu.AddSeparator();
            MmMenu.Add("s1", new Slider("R", 4, 1, 4)).OnValueChange +=
                ManaManager_OnValueChange;
            MmMenu.Add("s2", new Slider("Q", 1, 1, 4)).OnValueChange +=
                ManaManager_OnValueChange;
            MmMenu.Add("s3", new Slider("Q", 1, 1, 4)).OnValueChange +=
                ManaManager_OnValueChange;
            MmMenu.Add("s4", new Slider("Q", 1, 1, 4)).OnValueChange +=
                ManaManager_OnValueChange;
            ResetSliders();
        }

        private static void ResetSliders()
        {
            var sliders = new List<Slider>();
            sliders.Add(MmMenu["s1"].Cast<Slider>());
            sliders.Add(MmMenu["s2"].Cast<Slider>());
            sliders.Add(MmMenu["s3"].Cast<Slider>());
            sliders.Add(MmMenu["s4"].Cast<Slider>());

            foreach (var slider in sliders)
            {
                slider.CurrentValue = 1;
            }
            UpdateSliders();
        }

        protected override void OnSpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe || !PrioritiesAreSet() || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None ||
                !MmMenu["manamanager"].Cast<CheckBox>().CurrentValue) return;
            var slot = args.Slot;
            if (slot == _two)
            {
                var neededMana = GetMana(_two);
                if (PlayerData.Spells.Find(s => s.Slot == _one).IsReady())
                    neededMana += GetMana(_one);
                if (Player.Mana < neededMana)
                {
                    args.Process = false;
                }
            }
            else if (slot == _three)
            {
                var neededMana = GetMana(_three);
                if (PlayerData.Spells.Find(s => s.Slot == _one).IsReady())
                    neededMana += GetMana(_one);
                if (PlayerData.Spells.Find(s => s.Slot == _two).IsReady())
                    neededMana += GetMana(_two);
                if (Player.Mana < neededMana)
                {
                    args.Process = false;
                }
            }
            else if (slot == _four)
            {
                var neededMana = GetMana(_four);
                if (PlayerData.Spells.Find(s => s.Slot == _one).IsReady())
                    neededMana += GetMana(_one);
                if (PlayerData.Spells.Find(s => s.Slot == _two).IsReady())
                    neededMana += GetMana(_two);
                if (PlayerData.Spells.Find(s => s.Slot == _three).IsReady())
                    neededMana += GetMana(_three);
                if (Player.Mana < neededMana)
                {
                    args.Process = false;
                }
            }
            if (args.Process ||
                (_lastBlocked == slot &&
                 !(Game.Time - _lastBlockedTime > Player.Spellbook.Spells.Find(s => s.Slot == slot).SData.CooldownTime)))
                return;
            Chat.Print("Mana Manager: Blocked " + slot);
            _lastBlocked = slot;
            _lastBlockedTime = Game.Time;
        }

        private static void ManaManager_OnValueChange(ValueBase<int> sender,
            ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSliders();
        }

        private static void SetPriority(int i, SpellSlot s)
        {
            switch (i)
            {
                case 1:
                    _one = s;
                    break;
                case 2:
                    _two = s;
                    break;
                case 3:
                    _three = s;
                    break;
                case 4:
                    _four = s;
                    break;
            }
        }

        private static void UpdateSliders()
        {
            var sliders = new Dictionary<int, Slider>();
            sliders.Add(1, MmMenu["s1"].Cast<Slider>());
            sliders.Add(2, MmMenu["s2"].Cast<Slider>());
            sliders.Add(3, MmMenu["s3"].Cast<Slider>());
            sliders.Add(4, MmMenu["s4"].Cast<Slider>());

            foreach (var slider in sliders)
            {
                switch (slider.Value.CurrentValue)
                {
                    case 1:
                        slider.Value.DisplayName = "Priority " + slider.Key + ": Q";
                        SetPriority(slider.Key, SpellSlot.Q);
                        break;
                    case 2:
                        slider.Value.DisplayName = "Priority " + slider.Key + ": W";
                        SetPriority(slider.Key, SpellSlot.W);
                        break;
                    case 3:
                        slider.Value.DisplayName = "Priority " + slider.Key + ": E";
                        SetPriority(slider.Key, SpellSlot.E);
                        break;
                    case 4:
                        slider.Value.DisplayName = "Priority " + slider.Key + ": R";
                        SetPriority(slider.Key, SpellSlot.R);
                        break;
                }
            }
        }
    }
}