using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VolatileAIO.Organs.Brain
{
    internal class RecallTracker : Heart
    {
        public List<Recall> Recalls = new List<Recall>();

        public float X()
        {
            return _hackMenu["recallx"].Cast<Slider>().CurrentValue;
        }

        public float Y()
        {
            return _hackMenu["recally"].Cast<Slider>().CurrentValue;
        }

        public class Recall
        {
            public Recall(AIHeroClient hero, int recallStart, int recallEnd, int duration)
            {
                Hero = hero;
                RecallStart = recallStart;
                Duration = duration;
                RecallEnd = recallEnd;
                ExpireTime = RecallEnd + 2000;
            }

            public int RecallEnd;
            public int Duration;
            public int RecallStart;
            public int ExpireTime;
            public int CancelT;
            public bool IsAborted;
            public AIHeroClient Hero;

            public void Abort()
            {
                CancelT = Environment.TickCount;
                ExpireTime = Environment.TickCount + 2000;
                IsAborted = true;
            }

            private float Elapsed
            {
                get { return ((CancelT > 0 ? CancelT : Environment.TickCount) - RecallStart); }
            }

            public float PercentComplete()
            {
                return (float)Math.Round((Elapsed / Duration) * 100) > 100 ? 100 : (float)Math.Round((Elapsed / Duration) * 100);
            }
        }

        public RecallTracker()
        {
            _hackMenu.AddGroupLabel("Volatile Recall Tracker");
            _hackMenu.Add("trackRecalls", new CheckBox("Track Recalls"));
            _hackMenu.Add("resetPos", new CheckBox("Reset Values")).OnValueChange += RecallTracker_OnReset; ;
            _hackMenu.Add("recallx", new Slider("X Position", 645, 0, Drawing.Width));
            _hackMenu.Add("recally", new Slider("Y Position", 910, 0, Drawing.Height));
            _hackMenu.Add("recallwidth", new Slider("Bar Width", 465, 0, 1500));
        }

        private void RecallTracker_OnReset(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            _hackMenu["recallx"].Cast<Slider>().CurrentValue = 645;
            _hackMenu["recally"].Cast<Slider>().CurrentValue = 915;
            _hackMenu["recallwidth"].Cast<Slider>().CurrentValue = 465;

            if (_hackMenu["resetPos"].Cast<CheckBox>().CurrentValue)
                _hackMenu["resetPos"].Cast<CheckBox>().CurrentValue = false;
        }

        protected override void Volatile_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            if (args.Type == TeleportType.Recall && sender is AIHeroClient && _hackMenu["trackRecalls"].Cast<CheckBox>().CurrentValue && !sender.IsMe)
            {
                switch (args.Status)
                {
                    case TeleportStatus.Abort:
                        foreach (var source in Recalls.Where(r => r.Hero == sender))
                        {
                            source.Abort();
                        }
                        break;

                    case TeleportStatus.Start:
                        var recall = Recalls.FirstOrDefault(r => r.Hero == sender);
                        if (recall != null)
                        {
                            Recalls.Remove(recall);
                        }
                        Recalls.Add(new Recall((AIHeroClient) sender, Environment.TickCount,
                            Environment.TickCount + args.Duration, args.Duration));
                        break;
                }
            }
        }
    }
}