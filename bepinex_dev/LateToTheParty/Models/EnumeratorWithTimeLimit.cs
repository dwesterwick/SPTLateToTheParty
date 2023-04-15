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
        private bool hadToWait = false;

        public EnumeratorWithTimeLimit(double _maxTimePerIteration)
        {
            maxTimePerIteration = _maxTimePerIteration;
        }

        public IEnumerator Run<T>(IEnumerable<T> collection, Action<T> collectionItemAction)
        {
            IsRunning = true;
            cycleTimer.Restart();

            foreach (T item in collection)
            {
                try
                {
                    collectionItemAction(item);
                }
                catch(Exception ex)
                {
                    LateToThePartyPlugin.Log.LogError("Aborting coroutine iteration for " + item.ToString());
                    LateToThePartyPlugin.Log.LogError(ex);
                }

                if (cycleTimer.ElapsedMilliseconds > maxTimePerIteration)
                {
                    hadToWait = true;
                    LateToThePartyPlugin.Log.LogWarning("Waiting for next frame... (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")");

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

            if (hadToWait)
            {
                LateToThePartyPlugin.Log.LogWarning("Waiting for next frame...done. (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")");
            }
        }

        public void Abort()
        {
            stopRequested = true;
        }
    }
}
