using LateToTheParty.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        public IEnumerator Run<TItem, TArgs>(IEnumerable<TItem> collection, Action<TItem, TArgs> collectionItemAction, TArgs args)
        {
            IsCompleted = false;
            IsRunning = true;
            hadToWait = false;

            cycleTimer.Restart();

            foreach (TItem item in collection)
            {
                try
                {
                    collectionItemAction(item, args);
                }
                catch(Exception ex)
                {
                    LoggingController.LogError("Aborting coroutine iteration for " + item.ToString());
                    LoggingController.LogError(ex.ToString());
                }

                if (cycleTimer.ElapsedMilliseconds > maxTimePerIteration)
                {
                    hadToWait = true;
                    LoggingController.LogWarning("Waiting for next frame... (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")", true);

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
                LoggingController.LogWarning("Waiting for next frame...done. (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")", true);
            }
        }

        public void Abort()
        {
            stopRequested = true;
        }
    }
}
