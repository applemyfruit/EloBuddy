using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using VolatileAIO.Organs;


namespace VolatileAIO
{
    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += VolatileLoader;
        }

        private static void VolatileLoader(EventArgs args)
        {
            var loader = new Heart(true);
        }
    }
}
