using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;
using SharpDX;
using SharpDX.Multimedia;
using EloBuddy.SDK.Enumerations;
using VolatileAIO.Organs.Brain.Utils;
using HitChance = EloBuddy.SDK.Enumerations.HitChance;
using Prediction = EloBuddy.SDK.Prediction;

namespace VolatileAIO.Organs.Brain
{
    internal class CastManager : Heart
    {
        internal static bool IsAutoAttacking;

        internal struct OptimizedLocation
        {
            public int ChampsHit;
            public Vector2 Position;

            public OptimizedLocation(Vector2 position, int champsHit)
            {
                Position = position;
                ChampsHit = champsHit;
            }
        }

        internal class Cast
        {
            internal class Line
            {
                internal static void SingleTargetHero(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, HitChance? hitChance = HitChance.Medium, AIHeroClient targetHero = null)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4))) return;

                    if (!spell.IsReady() || IsAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!target.IsValidTarget(spell.Range) || spell.GetPrediction(target).HitChance < hitChance)
                        return;
                    spell.Cast(spell.GetPrediction(target).CastPosition);
                }

                internal static void NewPredTest(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, Utils.HitChance hitChance = Utils.HitChance.Medium, AIHeroClient targetHero = null)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                        return;

                    if (!spell.IsReady() || IsAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!target.IsValidTarget(spell.Range))
                        return;

                    var coreType2 = SkillshotType.SkillshotLine;
                    bool aoe2 = false;
                    if ((int)spell.Type == (int)SkillshotType.SkillshotCircle)
                    {
                        coreType2 = SkillshotType.SkillshotCircle;
                        aoe2 = true;
                    }
                    if (spell.Width > 80 && spell.AllowedCollisionCount < 100)
                        aoe2 = true;
                    var predInput2 = new PredictionInput
                    {
                        Aoe = aoe2,
                        Collision = spell.AllowedCollisionCount < 100,
                        Speed = spell.Speed,
                        Delay = spell.CastDelay,
                        Range = spell.Range,
                        From = Player.ServerPosition,
                        Radius = spell.Radius,
                        Unit = target,
                        Type = coreType2
                    };
                    var poutput2 = Utils.VPrediction.GetPrediction(predInput2);
                    //var poutput2 = spell.GetPrediction(target);
                    Chat.Print(spell.Slot + " " + predInput2.Collision + poutput2.Hitchance);
                    if (spell.Speed < float.MaxValue && CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                        return;

                    if (hitChance == Utils.HitChance.VeryHigh)
                    {
                        if (poutput2.Hitchance >= Utils.HitChance.VeryHigh)
                            spell.Cast(poutput2.CastPosition);
                        else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 &&
                                 poutput2.Hitchance >= Utils.HitChance.High)
                        {
                            spell.Cast(poutput2.CastPosition);
                        }

                    }
                    else if (hitChance == Utils.HitChance.High)
                    {
                        if (poutput2.Hitchance >= Utils.HitChance.High)
                            spell.Cast(poutput2.CastPosition);

                    }
                    else if (hitChance == Utils.HitChance.Medium)
                    {
                        if (poutput2.Hitchance >= Utils.HitChance.Medium)
                            spell.Cast(poutput2.CastPosition);
                    }
                }

                internal static void Farm(Spell.Skillshot spell, int minHit = 1)
            {
                if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                    (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                    (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                    (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                    return;
                if (!spell.IsReady() || IsAutoAttacking) return;

                var minions =
                    MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius).Select(
                            minion =>
                                Prediction.Position.PredictUnitPosition(minion,
                                    (int)(spell.CastDelay + (Player.Distance(minion) / spell.Speed))))
                        .ToList();

                if (MinionManager.GetBestLineFarmLocation(minions, spell.Width, spell.Range).MinionsHit >= minHit)
                    spell.Cast(
                        MinionManager.GetBestLineFarmLocation(minions, spell.Width, spell.Range).Position.To3D());
            }
        }

        internal class Circle
            {
                internal static void Farm(Spell.Skillshot spell, int minHit = 1)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4))) return;
                    if (!spell.IsReady() || IsAutoAttacking) return;

                    var minions =
                        MinionManager.GetMinions(Player.ServerPosition, spell.Range + spell.Radius)
                            .Select(
                                minion =>
                                    Prediction.Position.PredictUnitPosition(minion,
                                        (int) (spell.CastDelay + (Player.Distance(minion)/spell.Speed))))
                            .ToList();
                    
                    if (MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).MinionsHit <
                        minHit) return;
                        spell.Cast(
                            MinionManager.GetBestCircularFarmLocation(minions, spell.Width, spell.Range).Position.To3D());
                }

                internal static void Optimized(Spell.Skillshot spell, DamageType damageType,
                    int range = 0, int minHit = 1, HitChance? hitChance = HitChance.Medium,
                    AIHeroClient targetHero = null)
                {
                    if ((spell.Slot != SpellSlot.Q || !TickManager.NoLag(1)) &&
                        (spell.Slot != SpellSlot.W || !TickManager.NoLag(2)) &&
                        (spell.Slot != SpellSlot.E || !TickManager.NoLag(3)) &&
                        (spell.Slot != SpellSlot.R || !TickManager.NoLag(4)))
                        return;

                    if (!spell.IsReady() || IsAutoAttacking) return;

                    AIHeroClient target;
                    if (targetHero == null)
                        target = range != 0
                            ? TargetManager.Target(range, damageType)
                            : TargetManager.Target(spell, damageType);
                    else target = targetHero;

                    if (target == null) return;

                    if (!target.IsValidTarget(spell.Range + spell.Radius) ||
                        spell.GetPrediction(target).HitChance < hitChance)
                        return;

                    var champs = EntityManager.Heroes.Enemies.Where(e => e.Distance(target) < spell.Radius).Select(champ => Prediction.Position.PredictUnitPosition(champ, ((int)Player.Distance(champ) / spell.Speed) + spell.CastDelay)).ToList();

                    var posAndHits = GetOptimizedCircleLocation(champs, spell.Width, spell.Range);

                    if (posAndHits.ChampsHit >= minHit)
                        spell.Cast(posAndHits.Position.To3DWorld());
                }
            }
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsAutoAttacking = false;
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsAutoAttacking = true;
        }

        public static OptimizedLocation GetOptimizedCircleLocation(List<Vector2> champPositions,
            float width,
            float range,
            // ReSharper disable once InconsistentNaming
            int useMECMax = 9)
        {
            var result = new Vector2();
            var champsHit = 0;
            var startPos = ObjectManager.Player.ServerPosition.To2D();

            range = range*range;

            if (champPositions.Count == 0)
            {
                return new CastManager.OptimizedLocation(result, champsHit);
            }
            
            if (champPositions.Count <= useMECMax)
            {
                var subGroups = GetCombinations(champPositions);
                foreach (var subGroup in subGroups)
                {
                    if (subGroup.Count > 0)
                    {
                        var circle = MEC.GetMec(subGroup);

                        if (circle.Radius <= width && circle.Center.Distance(startPos, true) <= range)
                        {
                            champsHit = subGroup.Count;
                            return new CastManager.OptimizedLocation(circle.Center, champsHit);
                        }
                    }
                }
            }
            else
            {
                foreach (var pos in champPositions)
                {
                    if (pos.Distance(startPos, true) <= range)
                    {
                        var count = champPositions.Count(pos2 => pos.Distance(pos2, true) <= width*width);

                        if (count >= champsHit)
                        {
                            result = pos;
                            champsHit = count;
                        }
                    }
                }
            }

            return new OptimizedLocation(result, champsHit);
        }

        private static List<List<Vector2>> GetCombinations(List<Vector2> allValues)
        {
            var collection = new List<List<Vector2>>();
            for (var counter = 0; counter < (1 << allValues.Count); ++counter)
            {
                var combination = allValues.Where((t, i) => (counter & (1 << i)) == 0).ToList();

                collection.Add(combination);
            }
            return collection;
        }

        public static void MenuInit()
        {
            CastMenu = VolatileMenu.AddSubMenu("Cast Manager", "castmenu", "Volatile CastManager");
            CastMenu.AddGroupLabel("Target Manager");
            CastMenu.Add("chosenignores", new CheckBox("Ignore all other champions if Selected Target", false));
            CastMenu.AddGroupLabel("HitChance Settings");
            if (QChance != null) CastMenu.Add("q", new Slider("Q", 1, 0, 2)).OnValueChange += OnSliderChange; 
            if (WChance != null) CastMenu.Add("w", new Slider("W", 1, 0, 2)).OnValueChange += OnSliderChange;
            if (EChance != null) CastMenu.Add("e", new Slider("E", 1, 0, 2)).OnValueChange += OnSliderChange;
            if (RChance != null) CastMenu.Add("r", new Slider("R", 1, 0, 2)).OnValueChange += OnSliderChange;
            UpdateSliders();
        }

        private static void OnSliderChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            UpdateSliders();
        }

        private static void UpdateSliders()
        {
            if (QChance != null)
            {
                switch (CastMenu["q"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        QChance = HitChance.Low;
                        CastMenu["q"].Cast<Slider>().DisplayName = "Q Hitchance: Fast (but less accurate)";
                        break;
                    case 1:
                        QChance = HitChance.Medium;
                        CastMenu["q"].Cast<Slider>().DisplayName = "Q Hitchance: Balanced";
                        break;
                    case 2:
                        QChance = HitChance.High;
                        CastMenu["q"].Cast<Slider>().DisplayName = "Q Hitchance: Slow (but more accurate)";
                        break;
                }
                if (Profileinit)
                {
                    if (ChampionProfiles.Options.Any(o => o.Id == "q"))
                    {
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "q",
                                ChampionProfiles.OptionType.Slider, CastMenu["q"].Cast<Slider>().CurrentValue.ToString()));
                    }
                    else
                    {
                        ChampionProfiles.Options.Remove(ChampionProfiles.Options.Find(o => o.Id != "q"));
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "q",
                                ChampionProfiles.OptionType.Slider, CastMenu["q"].Cast<Slider>().CurrentValue.ToString()));
                    }
                }
            }
            if (WChance != null)
            {
                switch (CastMenu["w"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        WChance = HitChance.Low;
                        CastMenu["w"].Cast<Slider>().DisplayName = "W Hitchance: Fast (but less accurate)";
                        break;
                    case 1:
                        WChance = HitChance.Medium;
                        CastMenu["w"].Cast<Slider>().DisplayName = "W Hitchance: Balanced";
                        break;
                    case 2:
                        WChance = HitChance.High;
                        CastMenu["w"].Cast<Slider>().DisplayName = "W Hitchance: Slow (but more accurate)";
                        break;
                }
                if (Profileinit)
                {
                    if (ChampionProfiles.Options.All(o => o.Id != "w"))
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "w",
                                ChampionProfiles.OptionType.Slider, CastMenu["w"].Cast<Slider>().CurrentValue.ToString()));
                    else
                    {
                        ChampionProfiles.Options.Remove(ChampionProfiles.Options.Find(o => o.Id != "w"));
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "w",
                                ChampionProfiles.OptionType.Slider, CastMenu["w"].Cast<Slider>().CurrentValue.ToString()));
                    }
                }
            }
            if (EChance != null)
            {
                switch (CastMenu["e"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        EChance = HitChance.Low;
                        CastMenu["e"].Cast<Slider>().DisplayName = "E Hitchance: Fast (but less accurate)";
                        break;
                    case 1:
                        EChance = HitChance.Medium;
                        CastMenu["e"].Cast<Slider>().DisplayName = "E Hitchance: Balanced";
                        break;
                    case 2:
                        EChance = HitChance.High;
                        CastMenu["e"].Cast<Slider>().DisplayName = "E Hitchance: Slow (but more accurate)";
                        break;
                }
                if (Profileinit)
                {
                    if (ChampionProfiles.Options.All(o => o.Id != "e"))
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "e",
                                ChampionProfiles.OptionType.Slider, CastMenu["e"].Cast<Slider>().CurrentValue.ToString()));
                    else
                    {
                        ChampionProfiles.Options.Remove(ChampionProfiles.Options.Find(o => o.Id != "e"));
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "e",
                                ChampionProfiles.OptionType.Slider, CastMenu["e"].Cast<Slider>().CurrentValue.ToString()));
                    }
                }
            }
            if (RChance != null)
            {
                switch (CastMenu["r"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        RChance = HitChance.Low;
                        CastMenu["r"].Cast<Slider>().DisplayName = "R Hitchance: Fast (but less accurate)";
                        break;
                    case 1:
                        RChance = HitChance.Medium;
                        CastMenu["r"].Cast<Slider>().DisplayName = "R Hitchance: Balanced";
                        break;
                    case 2:
                        RChance = HitChance.High;
                        CastMenu["r"].Cast<Slider>().DisplayName = "R Hitchance: Slow (but more accurate)";
                        break;
                }
                if (Profileinit)
                {
                    if (ChampionProfiles.Options.All(o => o.Id != "r"))
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "r",
                                ChampionProfiles.OptionType.Slider, CastMenu["r"].Cast<Slider>().CurrentValue.ToString()));
                    else
                    {
                        ChampionProfiles.Options.Remove(ChampionProfiles.Options.Find(o => o.Id != "r"));
                        ChampionProfiles.Options.Add(
                            new ChampionProfiles.ProfileOption(ChampionProfiles.MenuType.Castmanager, "r",
                                ChampionProfiles.OptionType.Slider, CastMenu["r"].Cast<Slider>().CurrentValue.ToString()));
                    }
                }
            }
        }
    }
}