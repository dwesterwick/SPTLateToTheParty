using EFT.Interactive;
using EFT.InventoryLogic;
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
    internal class TaskWithTimeLimit : MethodWithTimeLimit
    {
        public bool IgnoredErrors { get; protected set; } = false;

        protected virtual Task task
        {
            get { return _task; }
        }

        protected CancellationTokenSource cancellationTokenSource;
        private Task _task;

        public TaskWithTimeLimit(double _maxTimePerIteration) : base(_maxTimePerIteration)
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start(Action action)
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("There is already a task running.");
            }

            SetMethodName(action.Method.Name);
            _task = Task.Run(action, cancellationTokenSource.Token);
            base.cycleTimer.Restart();
            base.IsRunning = true;
        }

        public void StartAndIgnoreErrors(Action action)
        {
            SetMethodName(action.Method.Name);
            Action actionWrapper = () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
            };

            this.Start(actionWrapper);
        }

        public IEnumerator WaitForTask()
        {
            while (taskIsRunning())
            {
                if (task.Wait(1, cancellationTokenSource.Token))
                {
                    break;
                }

                if (stopRequested)
                {
                    base.IsRunning = false;
                    yield break;
                }

                if (base.cycleTimer.ElapsedMilliseconds > base.maxTimePerIteration)
                {
                    yield return base.WaitForNextFrame();
                }
            }

            base.FinishedWaitingForFrames();
            base.IsRunning = false;
            base.IsCompleted = true;
        }

        public void Abort()
        {
            base.stopRequested = true;
            if (!task.IsCompleted)
            {
                cancellationTokenSource.Cancel();
                base.AbortWaitingForFrames();
                base.IsRunning = false;
            }
        }

        protected bool taskIsRunning()
        {
            if (!task.IsCompleted)
            {
                return true;
            }

            return false;
        }

        protected void HandleError(Exception ex)
        {
            IgnoredErrors = true;
            LoggingController.LogError(ex.ToString());
            LoggingController.LogWarning("The error above was ignored.");
        }
    }
}
