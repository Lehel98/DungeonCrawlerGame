using System;
using System.Timers;
using DungeonTest.Persistence;

namespace DungeonTest.Model
{
    public class Scrap
    {
        private readonly Timer _timer;
        private readonly Random _random;

        public Field ScrapType { get; }

        public Int32 X { get; }

        public Int32 Y { get; }

        public Scrap(Int32 x, Int32 y, Field scrapType)
        {
            X = x;
            Y = y;
            ScrapType = scrapType;
            _random = new Random();
            _timer = new Timer
            {
                Enabled = true,
                AutoReset = false
            };
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
        }

        /// <summary>
        /// Újraindítjuk az időzítőt, ha valaki felvette a scrapet.
        /// </summary>
        public void StartTimer()
        {
            _timer.Stop();
            _timer.Enabled = true;
            _timer.Interval = _random.Next(5000) + 6000;
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer.Stop();
            _timer.Enabled= false;
        }

        public void DisposeTimer()
        {
            if (_timer == null)
                return;

            _timer.Stop();
            _timer.Elapsed -= new ElapsedEventHandler(TimerElapsed);
            _timer?.Dispose();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            OnRespawn();
        }

        public event EventHandler<ScrapEventargs> Respawn;

        private void OnRespawn()
        {
            if (Respawn != null)
                Respawn.Invoke(this, new ScrapEventargs { Pair = new Tuple<Int32, Int32>(X, Y), Type = ScrapType });
        }
    }
}
