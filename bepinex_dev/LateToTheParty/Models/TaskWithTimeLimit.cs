using LateToTheParty.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    public class TaskWithTimeLimit
    {
        protected virtual Task task
        {
            get { return _task; }
        }

        protected Stopwatch cycleTimer = new Stopwatch();
        protected CancellationTokenSource cancellationTokenSource;
        protected double maxTimePerIteration;
        protected bool hadToWait = false;

        private Task _task;

        protected TaskWithTimeLimit(double _maxTimePerIteration)
        {
            maxTimePerIteration = _maxTimePerIteration;
            cancellationTokenSource = new CancellationTokenSource();
            cycleTimer.Restart();
        }

        public TaskWithTimeLimit(double _maxTimePerIteration, Action action) : this(_maxTimePerIteration)
        {
            _task = Task.Run(action, cancellationTokenSource.Token);
        }

        public IEnumerator WaitForTask()
        {
            while (isRunning())
            {
                if (task.Wait(1))
                {
                    break;
                }

                if (cycleTimer.ElapsedMilliseconds > maxTimePerIteration)
                {
                    hadToWait = true;
                    LoggingController.LogWarning("Waiting for next frame (task)... (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")", true);

                    yield return null;
                    cycleTimer.Restart();
                }
            }

            if (hadToWait)
            {
                LoggingController.LogWarning("Waiting for next frame (task)...done. (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")", true);
            }
        }

        public void Abort()
        {
            if (!task.IsCompleted)
            {
                cancellationTokenSource.Cancel();

                if (hadToWait)
                {
                    LoggingController.LogWarning("Waiting for next frame (task)...aborted. (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")", true);
                }
            }
        }

        protected bool isRunning()
        {
            if (!task.IsCompleted)
            {
                return true;
            }

            return false;
        }
    }
}
