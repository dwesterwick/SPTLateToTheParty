using LateToTheParty.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    public class EnumeratorWithTimeLimit
    {
        public bool IsRunning { get; private set; } = false;
        public bool IsCompleted { get; private set; } = false;

        private Stopwatch cycleTimer = new Stopwatch();
        private double maxTimePerIteration;
        private bool stopRequested = false;

        public EnumeratorWithTimeLimit(double _maxTimePerIteration)
        {
            maxTimePerIteration = _maxTimePerIteration;
        }

        public IEnumerator Run<T>(IEnumerable<T> collection, Action<T> collectionItemAction)
        {
            IsRunning = true;

            foreach (T item in collection)
            {
                collectionItemAction(item);

                if (cycleTimer.ElapsedMilliseconds > maxTimePerIteration)
                {
                    yield return null;
                    cycleTimer.Restart();
                }

                if (stopRequested)
                {
                    IsRunning = false;
                    yield break;
                }
            }

            IsRunning = false;
            IsCompleted = true;
        }

        public void Abort()
        {
            stopRequested = true;
        }
    }
}
