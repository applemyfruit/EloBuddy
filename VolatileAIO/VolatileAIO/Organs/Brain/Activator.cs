using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VolatileAIO.Organs.Brain
{
    internal class Activator : Heart
    {
        public static Menu ActivatorMenu;
        public static Menu SummonersMenu;
        public static Menu OffensivesMenu;
        public static Menu RegeneratingMenu;
        public static Menu DefensivesMenu;
        public static Menu CleansersMenu;

        public int Muramana = 3042;
        public int Manamune = 3004;

        private readonly int[] _smiteDamage =
        {
            390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950,
            1000
        };

        private readonly SpellSlot _heal = Player.GetSpellSlotFromName("summonerheal");
        private readonly SpellSlot _barrier = Player.GetSpellSlotFromName("summonerbarrier");
        private readonly SpellSlot _ignite = Player.GetSpellSlotFromName("summonerdot");
        private readonly SpellSlot _exhaust = Player.GetSpellSlotFromName("summonerexhaust");
        private SpellSlot _smite;

        public static Item

            Potion = new Item(2003),
            Biscuit = new Item(2010),
            Refillable = new Item(2031),
            Hunter = new Item(2032),
            Corrupting = new Item(2033),

            Botrk = new Item(3153, 550f),
            Cutlass = new Item(3144, 550f),
            Youmuus = new Item(3142, 650f),
            Tiamat = new Item(3074, 440f),
            Hydra = new Item(3077, 440f),
            HydraTitanic = new Item(3748, 150f),
            Hextech = new Item(3146, 700f),
            FrostQueen = new Item(3092, 850f),

            FaceOfTheMountain = new Item(3401, 600f),
            Zhonya = new Item(3157),
            Seraph = new Item(3040),
            Solari = new Item(3190, 600f),
            Randuin = new Item(3143, 400f),

            Mikaels = new Item(3222, 600f),
            Quicksilver = new Item(3140),
            Mercurial = new Item(3139),
            Dervish = new Item(3137);

        public Activator()
        {
            ActivatorMenu = MainMenu.AddMenu("V.Activator", "activator", "Volatile Activator");
            InitializeSummoners();
            InitializePots();
            InitializeOffensive();
            InitializeDefensive();
            InitializeCleansers();
        }

        private void InitializeCleansers()
        {
            CleansersMenu = ActivatorMenu.AddSubMenu("Cleansers", "cleansers");
            CleansersMenu.Add("qss", new CheckBox("Use QSS"));
            CleansersMenu.Add("qss2", new CheckBox("Use Mikaels"));
            CleansersMenu.Add("qss3", new CheckBox("Use Mercurial"));
            CleansersMenu.Add("qss4", new CheckBox("Use Dervish"));

            foreach (var ally in EntityManager.Heroes.Allies)
                CleansersMenu.Add("m" + ally.ChampionName, new CheckBox("Mikaels " + ally.ChampionName));

            CleansersMenu.Add("special", new CheckBox("Cleanse Special Spells"));
            CleansersMenu.Add("stun", new CheckBox("Cleanse Stun"));
            CleansersMenu.Add("snare", new CheckBox("Cleanse Snare"));
            CleansersMenu.Add("charm", new CheckBox("Cleanse Charm"));
            CleansersMenu.Add("fear", new CheckBox("Cleanse Fear"));
            CleansersMenu.Add("suppr", new CheckBox("Cleanse Suppression"));
            CleansersMenu.Add("taunt", new CheckBox("Cleanse Taunt"));
            CleansersMenu.Add("blind", new CheckBox("Cleanse Blind"));
        }

        private void InitializeDefensive()
        {
            DefensivesMenu = ActivatorMenu.AddSubMenu("Defensive Items", "defensive");
            DefensivesMenu.AddGroupLabel("Randuin's Omen");
            DefensivesMenu.Add("ro", new CheckBox("Use Randuins"));

            DefensivesMenu.AddGroupLabel("Face of the Mountain");
            DefensivesMenu.Add("fotm", new CheckBox("Use FotM"));

            DefensivesMenu.AddGroupLabel("Zhonya's Hourglass");
            DefensivesMenu.Add("zh", new CheckBox("Use Zhonyas"));

            DefensivesMenu.AddGroupLabel("Seraph's Embrace");
            DefensivesMenu.Add("se", new CheckBox("Use Seraphs"));

            DefensivesMenu.AddGroupLabel("Locket of the Iron Solari");
            DefensivesMenu.Add("lotis", new CheckBox("Use Solari"));
        }

        private void InitializeOffensive()
        {
            OffensivesMenu = ActivatorMenu.AddSubMenu("Offensive Items", "offensive");
            OffensivesMenu.AddGroupLabel("Blade of the Ruined King");
            OffensivesMenu.Add("botrk", new CheckBox("Use BotRK"));
            OffensivesMenu.Add("botrkks", new CheckBox("Use BotRK to KS"));
            OffensivesMenu.Add("botrkls", new CheckBox("Use BotRK to save your life"));
            OffensivesMenu.Add("botrkc", new CheckBox("Always use BotRK in Combo"));

            OffensivesMenu.AddGroupLabel("Bilgewater Cutlass");
            OffensivesMenu.Add("cut", new CheckBox("Use Cutlass"));
            OffensivesMenu.Add("cutks", new CheckBox("Use Cutlass to KS"));
            OffensivesMenu.Add("cutc", new CheckBox("Always use Cutlass in Combo"));

            OffensivesMenu.AddGroupLabel("Hextech Gunblade");
            OffensivesMenu.Add("hex", new CheckBox("Use Hextech"));
            OffensivesMenu.Add("hexks", new CheckBox("Use Hextech to KS"));
            OffensivesMenu.Add("hexc", new CheckBox("Always use Hextech in Combo"));

            OffensivesMenu.AddGroupLabel("Youmuu's Ghostblade");
            OffensivesMenu.Add("gb", new CheckBox("Use Ghostblade"));

            OffensivesMenu.AddGroupLabel("Tiamat");
            OffensivesMenu.Add("tmat", new CheckBox("Use Tiamat"));

            OffensivesMenu.AddGroupLabel("Ravenous Hydra");
            OffensivesMenu.Add("rh", new CheckBox("Use Rav. Hydra"));

            OffensivesMenu.AddGroupLabel("Titanic Hydra");
            OffensivesMenu.Add("th", new CheckBox("Use Titanic Hydra"));

            OffensivesMenu.AddGroupLabel("Muramana");
            OffensivesMenu.Add("mm", new CheckBox("Use Muramana"));

            OffensivesMenu.AddGroupLabel("Frost Queen's Claim");
            OffensivesMenu.Add("fq", new CheckBox("Use Frost Queen"));
        }

        private void InitializePots()
        {
            RegeneratingMenu = ActivatorMenu.AddSubMenu("Potions", "potions");
            RegeneratingMenu.AddGroupLabel("Potions");
            RegeneratingMenu.Add("hpot", new CheckBox("Use Health Pot"));
            RegeneratingMenu.Add("hpot2", new CheckBox("Use Biscuit"));
            RegeneratingMenu.Add("hpot3", new CheckBox("Use Refillable Potion"));
            RegeneratingMenu.Add("hpot4", new CheckBox("Use Hunter's Potion"));
            RegeneratingMenu.Add("hpot5", new CheckBox("Use Corrupting Potion"));
        }

        private void InitializeSummoners()
        {
            _smite = Player.GetSpellSlotFromName("summonersmite");
            if (_smite == SpellSlot.Unknown)
            {
                _smite = Player.GetSpellSlotFromName("itemsmiteaoe");
            }
            if (_smite == SpellSlot.Unknown)
            {
                _smite = Player.GetSpellSlotFromName("s5_summonersmiteplayerganker");
            }
            if (_smite == SpellSlot.Unknown)
            {
                _smite = Player.GetSpellSlotFromName("s5_summonersmitequick");
            }
            if (_smite == SpellSlot.Unknown)
            {
                _smite = Player.GetSpellSlotFromName("s5_summonersmiteduel");
            }

            SummonersMenu = ActivatorMenu.AddSubMenu("Summoners", "summoners", "Summoners");

            if (_smite != SpellSlot.Unknown)
            {
                SummonersMenu.AddLabel("Smite");
                SummonersMenu.Add("smite", new KeyBind("Use Smite", false, KeyBind.BindTypes.PressToggle, 'M'));
                SummonersMenu.Add("smitebig", new CheckBox("Use Smite objectives"));
                SummonersMenu.Add("smiteenemy", new CheckBox("Auto Smite enemy under 50% hp"));
                SummonersMenu.AddSeparator();
            }

            if (_exhaust != SpellSlot.Unknown)
            {
                SummonersMenu.AddLabel("Exhaust");
                SummonersMenu.Add("exhaust", new CheckBox("Use Exhaust"));
                SummonersMenu.Add("exhaust2", new CheckBox("Exhaust if channeling important spell"));
                SummonersMenu.Add("exhaust3", new CheckBox("Always in combo"));
                SummonersMenu.AddSeparator();
            }
            if (_heal != SpellSlot.Unknown)
            {
                SummonersMenu.AddLabel("Heal");
                SummonersMenu.Add("heal", new CheckBox("Use Heal"));
                SummonersMenu.Add("healteam", new CheckBox("Use Heal to save allies"));
                SummonersMenu.AddSeparator();
            }
            if (_barrier != SpellSlot.Unknown)
            {
                SummonersMenu.AddLabel("Barrier");
                SummonersMenu.Add("barrier", new CheckBox("Use Barrier"));
                SummonersMenu.AddSeparator();

            }
            if (_ignite != SpellSlot.Unknown)
            {
                SummonersMenu.AddLabel("Ignite");
                SummonersMenu.Add("ignite", new CheckBox("Use Ignite"));
                SummonersMenu.AddSeparator();
            }
        }

        protected override void Volatile_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!OffensivesMenu["th"].Cast<CheckBox>().CurrentValue ||
                Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.Combo || !HydraTitanic.IsReady() || !target.IsValid ||
                !(target is AIHeroClient)) return;
            HydraTitanic.Cast();
            Orbwalker.ResetAutoAttack();
        }

        protected override void Volatile_ProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy)
                return;

            if (!Solari.IsReady() && !FaceOfTheMountain.IsReady() && !Seraph.IsReady() && !Zhonya.IsReady() &&
                !CanUse(_barrier) && !CanUse(_heal) && !CanUse(_exhaust))
                return;

            if (sender.Distance(Player.Position) > 1600)
                return;

            foreach (
                var ally in
                    EntityManager.Heroes.Allies.Where(
                        ally =>
                            ally.IsValid && !ally.IsDead && ally.HealthPercent < 51 &&
                            Player.Distance(ally.ServerPosition) < 700))
            {
                double dmg = 0;
                if (args.Target != null && args.Target.NetworkId == ally.NetworkId)
                {
                    dmg = dmg + ((AIHeroClient) sender).GetSpellDamage(ally, args.Slot);
                }
                else
                {
                    var castArea = ally.Distance(args.End)*(args.End - ally.ServerPosition).Normalized() +
                                   ally.ServerPosition;
                    if (castArea.Distance(ally.ServerPosition) < ally.BoundingRadius/2)
                        dmg = dmg + ((AIHeroClient) sender).GetSpellDamage(ally, args.Slot);
                    else
                        continue;
                }

                if (CanUse(_exhaust) && SummonersMenu["exhaust"].Cast<CheckBox>().CurrentValue)
                {
                    if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*40)
                        TryCast(() => Player.Spellbook.CastSpell(_exhaust, sender));
                }

                if (CanUse(_heal) && SummonersMenu["heal"].Cast<CheckBox>().CurrentValue)
                {
                    if (SummonersMenu["healteam"].Cast<CheckBox>().CurrentValue && !ally.IsMe)
                        return;

                    if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*10)
                        TryCast(() => Player.Spellbook.CastSpell(_heal, ally));
                    else if (ally.Health - dmg < ally.Level*10)
                        TryCast(() => Player.Spellbook.CastSpell(_heal, ally));
                }

                if (DefensivesMenu["lotis"].Cast<CheckBox>().CurrentValue && Solari.IsReady() &&
                    Player.Distance(ally.ServerPosition) < Solari.Range)
                {
                    var value = 75 + (15*Player.Level);
                    if (dmg > value && Player.HealthPercent < 50)
                        Solari.Cast();
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                        Solari.Cast();
                    else if (ally.Health - dmg < ally.Level*10)
                        Solari.Cast();
                }

                if (DefensivesMenu["fotm"].Cast<CheckBox>().CurrentValue && FaceOfTheMountain.IsReady() &&
                    Player.Distance(ally.ServerPosition) < FaceOfTheMountain.Range)
                {
                    var value = 0.1*Player.MaxHealth;
                    if (dmg > value && Player.HealthPercent < 50)
                        TryCast(() => FaceOfTheMountain.Cast(ally));
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                        TryCast(() => FaceOfTheMountain.Cast(ally));
                    else if (ally.Health - dmg < ally.Level*10)
                        TryCast(() => FaceOfTheMountain.Cast(ally));
                }

                if (!ally.IsMe)
                    continue;

                if (CanUse(_barrier) && SummonersMenu["barrier"].Cast<CheckBox>().CurrentValue)
                {
                    var value = 95 + Player.Level*20;
                    if (dmg > value && Player.HealthPercent < 50)
                        TryCast(() => Player.Spellbook.CastSpell(_barrier, Player));
                    else if (Player.Health - dmg < Player.CountEnemiesInRange(700)*Player.Level*15)
                        TryCast(() => Player.Spellbook.CastSpell(_barrier, Player));
                    else if (ally.Health - dmg < ally.Level*15)
                        TryCast(() => Seraph.Cast());
                }

                if (Seraph.IsReady() && DefensivesMenu["se"].Cast<CheckBox>().CurrentValue)
                {
                    var value = Player.Mana*0.2 + 150;
                    if (dmg > value && Player.HealthPercent < 50)
                        TryCast(() => Seraph.Cast());
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                        TryCast(() => Seraph.Cast());
                    else if (ally.Health - dmg < ally.Level*15)
                        TryCast(() => Seraph.Cast());
                }

                if (Zhonya.IsReady() && DefensivesMenu["zh"].Cast<CheckBox>().CurrentValue)
                {
                    if (dmg > Player.Level*30)
                        TryCast(() => Zhonya.Cast());
                    else if (ally.Health - dmg < ally.CountEnemiesInRange(700)*ally.Level*15)
                    {
                        TryCast(() => Zhonya.Cast());
                    }
                    else if (ally.Health - dmg < ally.Level*15)
                    {
                        TryCast(() => Zhonya.Cast());
                    }
                }
            }
        }

        private static void TryCast(Action cast)
        {
            Core.DelayAction(cast, 0);
            Core.DelayAction(cast, 100);
            Core.DelayAction(cast, 200);
            Core.DelayAction(cast, 300);
        }

        protected override void Volatile_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (OffensivesMenu["mm"].Cast<CheckBox>().CurrentValue)
            {
                int mur = Item.HasItem(Muramana) ? 3042 : 3043;
                if (Item.HasItem(mur) && args.Target.IsEnemy && args.Target.IsValid && args.Target is AIHeroClient &&
                    Item.CanUseItem(mur) && Player.Mana > Player.MaxMana*0.3)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                        Item.UseItem(mur);
                }
                else if (ObjectManager.Player.HasBuff("Muramana") && Item.HasItem(mur) && Item.CanUseItem(mur))
                    Item.UseItem(mur);
            }
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            Cleansers();
            Smite();

            if (!TickManager.NoLag(0) || Player.IsRecalling() || Player.IsDead)
                return;

            PotionManagement();
            Ignite();
            Exhaust();
            Offensive();
            Defensive();
            ZhonyaCast();
        }

        public static readonly string[] SmiteableUnits =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron",
            "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak",
            "SRU_Krug", "Sru_Crab"
        };

        private void Smite()
        {
            if (CanUse(_smite))
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 520, MinionTypes.All, MinionTeam.Neutral);
                if (mobs.Count == 0 && SummonersMenu["smiteenemy"].Cast<CheckBox>().CurrentValue &&
                    (Player.GetSpellSlotFromName("s5_summonersmiteplayerganker") != SpellSlot.Unknown ||
                     Player.GetSpellSlotFromName("s5_summonersmiteduel") != SpellSlot.Unknown))
                {
                    var enemy = TargetSelector.GetTarget(500, DamageType.True);
                    if (enemy.IsValidTarget() && enemy.HealthPercent < 50)
                    {
                        Player.Spellbook.CastSpell(_smite, enemy);
                    }
                }

                foreach (var mob in mobs)
                {
                    if ((mob.BaseSkinName == "SRU_Dragon" || mob.BaseSkinName == "SRU_Baron") &&
                        SummonersMenu["smitebig"].Cast<CheckBox>().CurrentValue
                        && Prediction.Health.GetPrediction(mob, 20) < _smiteDamage[Player.Level])
                    {
                        Player.Spellbook.CastSpell(_smite, mob);
                    }
                    else if (SmiteableUnits.Contains(mob.BaseSkinName) &&
                             Prediction.Health.GetPrediction(mob, 20) < _smiteDamage[Player.Level] &&
                             SummonersMenu["smite"].Cast<CheckBox>().CurrentValue)
                    {
                        Player.Spellbook.CastSpell(_smite, mob);
                    }
                }
            }
        }

        private void Exhaust()
        {
            if (CanUse(_exhaust) && SummonersMenu["exhaust"].Cast<CheckBox>().CurrentValue)
            {
                if (SummonersMenu["exhaust3"].Cast<CheckBox>().CurrentValue &&
                    Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                {
                    var t = TargetSelector.GetTarget(650, DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        Player.Spellbook.CastSpell(_exhaust, t);
                    }
                }
            }
        }

        protected override void Volatile_OnInterruptable(Obj_AI_Base sender,
            Interrupter.InterruptableSpellEventArgs args)
        {
            if (args.DangerLevel != DangerLevel.High) return;
            if (!SummonersMenu["exhaust2"].Cast<CheckBox>().CurrentValue) return;
            foreach (
                var enemy in
                    EntityManager.Heroes.Enemies.Where(
                        enemy => enemy.IsValidTarget(650)))
            {
                Player.Spellbook.CastSpell(_exhaust, enemy);
            }
        }

        private void Ignite()
        {
            if (CanUse(_ignite) && SummonersMenu["ignite"].Cast<CheckBox>().CurrentValue)
            {
                var enemy = TargetSelector.GetTarget(600, DamageType.True);
                if (enemy.IsValidTarget())
                {
                    var ignDmg = Player.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite);
                    if (enemy.Health <= ignDmg && Player.Distance(enemy.ServerPosition) > 500 &&
                        EntityManager.Heroes.Enemies.Count(e => e.Distance(enemy) < 500) < 2)
                        Player.Spellbook.CastSpell(_ignite, enemy);

                    if (enemy.Health <= 2*ignDmg)
                    {
                        if (enemy.PercentLifeStealMod > 10)
                            Player.Spellbook.CastSpell(_ignite, enemy);

                        if (enemy.HasBuff("RegenerationPotion") || enemy.HasBuff("ItemMiniRegenPotion") ||
                            enemy.HasBuff("ItemCrystalFlask"))
                            Player.Spellbook.CastSpell(_ignite, enemy);

                        if (enemy.Health > Player.Health)
                            Player.Spellbook.CastSpell(_ignite, enemy);
                    }
                }
            }
        }

        private void ZhonyaCast()
        {
            if (DefensivesMenu["zh"].Cast<CheckBox>().CurrentValue && Zhonya.IsReady())
            {
                float time = 10;
                if (Player.HasBuff("zedrdeathmark"))
                {
                    time = GetPassiveTime(Player, "zedulttargetmark");
                }
                if (Player.HasBuff("FizzMarinerDoom"))
                {
                    time = GetPassiveTime(Player, "FizzMarinerDoom");
                }
                if (Player.HasBuff("MordekaiserChildrenOfTheGrave"))
                {
                    time = GetPassiveTime(Player, "MordekaiserChildrenOfTheGrave");
                }
                if (Player.HasBuff("VladimirHemoplague"))
                {
                    time = GetPassiveTime(Player, "VladimirHemoplague");
                }
                if (time < 1 && time > 0)
                    Zhonya.Cast();
            }
        }

        public static float GetPassiveTime(Obj_AI_Base target, string buffName)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == buffName)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }

        private void Cleansers()
        {
            if (!Quicksilver.IsReady() && !Mikaels.IsReady() && !Mercurial.IsReady() && !Dervish.IsReady())
                return;

            if ((Player.HasBuff("zedrdeathmark") || Player.HasBuff("FizzMarinerDoom") ||
                 Player.HasBuff("MordekaiserChildrenOfTheGrave") || Player.HasBuff("PoppyDiplomaticImmunity") ||
                 Player.HasBuff("VladimirHemoplague")) && CleansersMenu["special"].Cast<CheckBox>().CurrentValue)
                Clean();

            if (Mikaels.IsReady() && CleansersMenu["qss2"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var ally in EntityManager.Heroes.Allies.Where(
                    ally =>
                        ally.IsValid && !ally.IsDead &&
                        CleansersMenu["m" + ally.ChampionName].Cast<CheckBox>().CurrentValue &&
                        Player.Distance(ally.Position) < Mikaels.Range))
                {
                    if (ally.HasBuff("zedrdeathmark") || ally.HasBuff("FizzMarinerDoom") ||
                        ally.HasBuff("MordekaiserChildrenOfTheGrave") || ally.HasBuff("PoppyDiplomaticImmunity") ||
                        ally.HasBuff("VladimirHemoplague"))
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Stun) && CleansersMenu["stun"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Snare) && CleansersMenu["snare"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Charm) && CleansersMenu["charm"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Fear) && CleansersMenu["fear"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Taunt) && CleansersMenu["taunt"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Suppression) && CleansersMenu["suppr"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                    if (ally.HasBuffOfType(BuffType.Blind) && CleansersMenu["blind"].Cast<CheckBox>().CurrentValue)
                        Mikaels.Cast(ally);
                }
            }

            if (Player.HasBuffOfType(BuffType.Stun) && CleansersMenu["stun"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Snare) && CleansersMenu["snare"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Charm) && CleansersMenu["charm"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Fear) && CleansersMenu["fear"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Taunt) && CleansersMenu["taunt"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Suppression) && CleansersMenu["suppr"].Cast<CheckBox>().CurrentValue)
                Clean();
            if (Player.HasBuffOfType(BuffType.Blind) && CleansersMenu["blind"].Cast<CheckBox>().CurrentValue)
                Clean();
        }

        private static void Clean()
        {
            if (Quicksilver.IsReady() && CleansersMenu["qss"].Cast<CheckBox>().CurrentValue)
                Quicksilver.Cast();
            else if (Mercurial.IsReady() && CleansersMenu["qss3"].Cast<CheckBox>().CurrentValue)
                Mercurial.Cast();
            else if (Dervish.IsReady() && CleansersMenu["qss4"].Cast<CheckBox>().CurrentValue)
                Dervish.Cast();
        }

        private static void Defensive()
        {
            if (Randuin.IsReady() && DefensivesMenu["ro"].Cast<CheckBox>().CurrentValue &&
                Player.CountEnemiesInRange(Randuin.Range) > 0)
            {
                Randuin.Cast();
            }
        }

        private static void Offensive()
        {
            if (Botrk.IsReady() && OffensivesMenu["botrk"].Cast<CheckBox>().CurrentValue)
            {
                var t = TargetSelector.GetTarget(Botrk.Range, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (OffensivesMenu["botrkks"].Cast<CheckBox>().CurrentValue &&
                        Player.CalculateDamageOnUnit(t, DamageType.Physical, t.MaxHealth*(float) 0.1) > t.Health)
                        Botrk.Cast(t);
                    if (OffensivesMenu["botrkls"].Cast<CheckBox>().CurrentValue && Player.Health < Player.MaxHealth*0.5)
                        Botrk.Cast(t);
                    if (OffensivesMenu["botrkc"].Cast<CheckBox>().CurrentValue &&
                        Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                        Botrk.Cast(t);
                }
            }

            if (Hextech.IsReady() && OffensivesMenu["hex"].Cast<CheckBox>().CurrentValue)
            {
                var t = TargetSelector.GetTarget(Hextech.Range, DamageType.Magical);
                if (t.IsValidTarget())
                {
                    if (OffensivesMenu["hexks"].Cast<CheckBox>().CurrentValue &&
                        Player.CalculateDamageOnUnit(t, DamageType.Magical, 150 + Player.FlatMagicDamageMod*(float) 0.4) >
                        t.Health)
                        Hextech.Cast(t);
                    if (OffensivesMenu["hexc"].Cast<CheckBox>().CurrentValue &&
                        Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                        Hextech.Cast(t);
                }
            }

            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo && FrostQueen.IsReady() &&
                OffensivesMenu["fq"].Cast<CheckBox>().CurrentValue)
            {
                var t = TargetSelector.GetTarget(FrostQueen.Range, DamageType.Magical);
                if (t.IsValidTarget() && t.Distance(Player) < 1500)
                {

                    FrostQueen.Cast();
                }
            }

            if (Cutlass.IsReady() && OffensivesMenu["cut"].Cast<CheckBox>().CurrentValue)
            {
                var t = TargetSelector.GetTarget(Cutlass.Range, DamageType.Magical);
                if (t.IsValidTarget())
                {
                    if (OffensivesMenu["cutks"].Cast<CheckBox>().CurrentValue &&
                        Player.CalculateDamageOnUnit(t, DamageType.Magical, 100) > t.Health)
                        Cutlass.Cast(t);
                    if (OffensivesMenu["cutc"].Cast<CheckBox>().CurrentValue &&
                        Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
                        Cutlass.Cast(t);
                }
            }

            if (Youmuus.IsReady() && OffensivesMenu["gb"].Cast<CheckBox>().CurrentValue &&
                Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                var t = Orbwalker.LastTarget;

                if (t.IsValidTarget(Player.AttackRange) && t is AIHeroClient)
                {
                    Youmuus.Cast();
                }
            }

            if ((OffensivesMenu["tmat"].Cast<CheckBox>().CurrentValue ||
                OffensivesMenu["rh"].Cast<CheckBox>().CurrentValue) && (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo || Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass))
            {
                if (Hydra.IsReady() && Player.CountEnemiesInRange(Hydra.Range) > 0)
                    Hydra.Cast();
                else if (Tiamat.IsReady() && Player.CountEnemiesInRange(Tiamat.Range) > 0)
                    Tiamat.Cast();
            }
        }

        private static void PotionManagement()
        {
            if (!InFountain(Player) && !Player.HasBuff("Recall"))
            {
                if (Player.HasBuff("RegenerationPotion") || Player.HasBuff("ItemMiniRegenPotion") ||
                    Player.HasBuff("ItemCrystalFlask") || Player.HasBuff("ItemCrystalFlaskJungle") ||
                    Player.HasBuff("ItemDarkCrystalFlask"))
                    return;

                if (Hunter.IsReady())
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Hunter.Cast();
                    else if (Player.Health < Player.MaxHealth*0.6)
                        Hunter.Cast();
                    else if (Player.CountEnemiesInRange(1200) > 0 && Player.Mana < 200 &&
                             !Player.HasBuff("FlaskOfCrystalWater"))
                        Hunter.Cast();
                    return;
                }

                if (Corrupting.IsReady())
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Corrupting.Cast();
                    else if (Player.Health < Player.MaxHealth*0.6)
                        Corrupting.Cast();
                    else if (Player.CountEnemiesInRange(1200) > 0 && Player.Mana < 200 &&
                             !Player.HasBuff("FlaskOfCrystalWater"))
                        Corrupting.Cast();
                    return;
                }

                if (Refillable.IsReady())
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Refillable.Cast();
                    else if (Player.Health < Player.MaxHealth*0.6)
                        Refillable.Cast();
                    return;
                }

                if (Potion.IsReady())
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Potion.Cast();
                    else if (Player.Health < Player.MaxHealth*0.6)
                        Potion.Cast();
                    return;
                }

                if (Biscuit.IsReady())
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Biscuit.Cast();
                    else if (Player.Health < Player.MaxHealth*0.6)
                        Biscuit.Cast();
                    return;
                }
            }
        }

        private static bool CanUse(SpellSlot sum)
        {
            return sum != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(sum) == SpellState.Ready;
        }
    }
}