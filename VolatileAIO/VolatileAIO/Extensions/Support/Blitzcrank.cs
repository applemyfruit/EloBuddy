using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;
using VolatileAIO.Organs;
using VolatileAIO.Organs.Brain;
using VolatileAIO.Organs._Test;
using Color = System.Drawing.Color;

namespace VolatileAIO.Extensions.Support
{
    internal class Blitzcrank : Heart
    {
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active E;
        public static Spell.Active R;
        
        private readonly DrawManager _drawManager = new DrawManager();

        public Blitzcrank()
        {
            InitializeMenu();
            InitializeSpells();
            _drawManager.UpdateValues(Q, W, E, R);
        }
         
        private void InitializeMenu()
        {

        }

        public static void InitializeSpells()
        {
            var qdata = SpellDatabase.Spells.Find(s => string.Equals(s.ChampionName, Player.ChampionName, StringComparison.CurrentCultureIgnoreCase) && s.Slot == SpellSlot.Q);

            Q = new Spell.Skillshot(SpellSlot.Q, (uint)qdata.Range, qdata.Type, qdata.Delay, qdata.MissileSpeed, qdata.Radius);
            W = new Spell.Active(SpellSlot.W, 150);
            E = new Spell.Active(SpellSlot.E, 150);
            R = new Spell.Active(SpellSlot.R, 550);
        }
        
        public static List<List<Obj_AI_Minion>> FarmR = new List<List<Obj_AI_Minion>>();

        public static List<List<Obj_AI_Minion>> GetMinionWaves()
        {
            List<List<Obj_AI_Minion>> waves = new List<List<Obj_AI_Minion>>();
            List<Obj_AI_Minion> creeps = EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => m.ServerPosition.IsOnScreen()).ToList();
                while (creeps.Count > 0)
                {
                    var waveunchecked = new List<Obj_AI_Minion>();
                    var wavechecked = new List<Obj_AI_Minion>();
                    Obj_AI_Minion creep = creeps[0];
                    waveunchecked.Add(creep);
                    creeps.Remove(creep);
                    while(waveunchecked.Count > 0)
                    {
                        foreach (
                            var minion in
                                creeps.Where(
                                    m => m.Distance(waveunchecked.ElementAt(0)) < 300).ToList()
                            )
                        {
                            if (wavechecked.Contains(minion)) continue;
                            waveunchecked.Add(minion);
                            creeps.Remove(minion);
                        }
                        wavechecked.Add(waveunchecked.FirstOrDefault());
                        waveunchecked.RemoveAt(0);
                    } 
                    waves.Add(wavechecked);
                };
                return waves;
        }

        protected override void Volative_OnDraw(EventArgs args)
        {
            if (EntityManager.MinionsAndMonsters.EnemyMinions.Count(m => m.ServerPosition.IsOnScreen()) > 2)
            {
                if (TickManager.NoLag(0))
                {
                    FarmR = GetMinionWaves();
                }
                var lastwavepos= new Vector3();
                if (FarmR != null)
                    foreach (var wave in FarmR)
                    {

                        //Chat.Print(wave.Count + " minions");
                        var wavepos = new Vector3();
                        foreach (var minion in wave)
                            wavepos = wavepos + minion.ServerPosition;
                        wavepos /= wave.Count;
                        if (lastwavepos != new Vector3() && wavepos.Distance(lastwavepos) < 450)
                        {
                            wavepos += lastwavepos;
                            wavepos /= 2; 
                        }
                        Drawing.DrawCircle(wavepos, R.Range, Color.GreenYellow);
                        lastwavepos = wavepos;
                    }
            }
        }

        protected override void Volatile_OnHeartBeat(EventArgs args)
        {
            TickManager.Tick();
            AutoCast();
            if (TickManager.NoLag(0))
                FarmR = GetMinionWaves();
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) Combo();
        }

        private void AutoCast()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(e=>e.IsValidTarget(Q.Range)))
            {
                if (TargetSelector.GetPriority(enemy) > 3)
                {
                    CastManager.Cast.Line.SingleTarget(Q, DamageType.Magical, (int)Q.Range, HitChance.High, enemy);
                }
            }
        }

        private void Combo()
        {
            CastManager.Cast.Line.SingleTarget(Q, DamageType.Magical);
            if (E.IsReady() && TickManager.NoLag(3))
            {
                var enemy = EntityManager.Heroes.Enemies.FirstOrDefault(e => Player.Distance(e) < 300);
                if (enemy != null)
                {
                    Orbwalker.DisableMovement = true;
                    Orbwalker.DisableAttacking = true;
                    E.Cast();
                    EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, enemy);
                    Orbwalker.DisableMovement = false;
                    Orbwalker.DisableAttacking = false;
                }
            }
            if (EntityManager.Heroes.Enemies.Exists(e => Player.Distance(e) < R.Range && e.HasBuffOfType(BuffType.Knockup)) && R.IsReady())
            {
                R.Cast();
            }
        }
    }
}