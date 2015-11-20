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
    internal class AutoLeveler : Heart
    {
        private static SpellSlot _one = SpellSlot.Unknown;
        private static SpellSlot _two = SpellSlot.Unknown;
        private static SpellSlot _three = SpellSlot.Unknown;
        private static SpellSlot _four = SpellSlot.Unknown;
        private static Menu _autoLevelMenu;

        protected override void Volatile_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!PrioritiesAreSet()) return;

            if (!sender.IsMe || !_autoLevelMenu["autolevel"].Cast<CheckBox>().CurrentValue ||
                Player.Level < _autoLevelMenu["start"].Cast<Slider>().CurrentValue)
                return;
            if (!PlayerData.Spells.Find(s => s.Slot == _one).IsLearned)
                Player.Spellbook.LevelSpell(_one);
            if (!PlayerData.Spells.Find(s => s.Slot == _two).IsLearned)
                Player.Spellbook.LevelSpell(_two);
            if (!PlayerData.Spells.Find(s => s.Slot == _three).IsLearned)
                Player.Spellbook.LevelSpell(_three);
            if (!PlayerData.Spells.Find(s => s.Slot == _four).IsLearned)
                Player.Spellbook.LevelSpell(_four);
            Player.Spellbook.LevelSpell(_one);
            Player.Spellbook.LevelSpell(_two);
            Player.Spellbook.LevelSpell(_three);
            Player.Spellbook.LevelSpell(_four);
        }

        private static bool PrioritiesAreSet()
        {
            if (_autoLevelMenu["s2"].Cast<Slider>().CurrentValue == _autoLevelMenu["s3"].Cast<Slider>().CurrentValue)
                return false;
            if (_autoLevelMenu["s2"].Cast<Slider>().CurrentValue == _autoLevelMenu["s4"].Cast<Slider>().CurrentValue)
                return false;
            if (_autoLevelMenu["s3"].Cast<Slider>().CurrentValue == _autoLevelMenu["s4"].Cast<Slider>().CurrentValue)
                return false;
            return true;
        }

        public AutoLeveler()
        {
            _autoLevelMenu = VolatileMenu.AddSubMenu("AutoLeveler", "autolevel", "Volatile Automatic Spell Leveler");
            _autoLevelMenu.AddLabel("Auto-Leveler will automatically level your spells in the set priority");
            _autoLevelMenu.Add("autolevel", new CheckBox("Use Auto-Leveler"));
            _autoLevelMenu.AddSeparator();
            _autoLevelMenu.Add("s1", new Slider("R", 4, 1, 4)).OnValueChange +=
                AutoLeveler_OnValueChange;
            _autoLevelMenu.Add("s2", new Slider("Q", 1, 1, 4)).OnValueChange +=
                AutoLeveler_OnValueChange;
            _autoLevelMenu.Add("s3", new Slider("Q", 1, 1, 4)).OnValueChange +=
                AutoLeveler_OnValueChange;
            _autoLevelMenu.Add("s4", new Slider("Q", 1, 1, 4)).OnValueChange +=
                AutoLeveler_OnValueChange;
            _autoLevelMenu.Add("start", new Slider("Start Auto-Leveler at level: ", 2, 1, 6));
            ResetSliders();
        }

        private static void ResetSliders()
        {
            var sliders = new List<Slider>();
            sliders.Add(_autoLevelMenu["s1"].Cast<Slider>());
            sliders.Add(_autoLevelMenu["s2"].Cast<Slider>());
            sliders.Add(_autoLevelMenu["s3"].Cast<Slider>());
            sliders.Add(_autoLevelMenu["s4"].Cast<Slider>());

            foreach (var slider in sliders)
            {
                slider.CurrentValue = 1;
            }
            UpdateSliders();
        }
        
        private static void AutoLeveler_OnValueChange(ValueBase<int> sender,
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
            sliders.Add(1, _autoLevelMenu["s1"].Cast<Slider>());
            sliders.Add(2, _autoLevelMenu["s2"].Cast<Slider>());
            sliders.Add(3, _autoLevelMenu["s3"].Cast<Slider>());
            sliders.Add(4, _autoLevelMenu["s4"].Cast<Slider>());

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