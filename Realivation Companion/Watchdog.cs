using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Realivation_Companion
{
    using System;
    using System.Threading;

    public class Watchdog
    {
        private readonly TimeSpan _initialDuration;
        private readonly Action _onTimerElapsed;
        private Timer _timer;
        private DateTime _startTime;        
        public TimeSpan RemainingTime { get; private set; }

        
        public Watchdog(TimeSpan duration, Action onElapsed)
        {
            _initialDuration = duration;
            _onTimerElapsed = onElapsed;
            RemainingTime = duration; 
            _timer = new Timer(TimerElapsed, null, Timeout.Infinite, 100);
        }

        public void Start()
        {
            _startTime = DateTime.Now;
            _timer.Change(0, 100);
        }
        
        
        public void Reset()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            RemainingTime = _initialDuration;
            Log.Information($"Timer Reset. Remaining: {RemainingTime.TotalSeconds:F1}s");
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        
        private void TimerElapsed(object? state)
        {
            TimeSpan elapsedTime = DateTime.Now - _startTime;
            RemainingTime = _initialDuration - elapsedTime;

            if (RemainingTime <= TimeSpan.Zero)
            {
                Stop();   
                RemainingTime = TimeSpan.Zero;                
                _onTimerElapsed();
            }
        }
    }
}
