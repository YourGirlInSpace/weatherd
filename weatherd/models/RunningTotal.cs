using System;
using System.Collections.Generic;
using System.Linq;

namespace weatherd.models
{
    public class RunningTotal
    {
        private readonly TimeSpan _totalTime;

        private readonly Queue<(DateTime measurementTime, float value)> _queue =
            new();

        public float Total
        {
            get
            {
                float first = _queue.First().value;
                float last = _queue.Last().value;

                return last - first;
            }
        }

        public RunningTotal(TimeSpan totalTime)
        {
            _totalTime = totalTime;
        }

        public void Add(float point)
        {
            _queue.Enqueue((DateTime.UtcNow, point));

            while (_queue.Count > 0 && (DateTime.UtcNow - _queue.Peek().measurementTime) > _totalTime)
                _queue.Dequeue();
        }
    }
}
