using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LateToTheParty.CoroutineExtensions
{
    internal class TaskWithReturnValueAndTimeLimit<TResult> : TaskWithTimeLimit
    {
        protected override Task task
        {
            get { return _task; }
        }

        private Task<TResult> _task;

        public TaskWithReturnValueAndTimeLimit(double _maxTimePerIteration): base(_maxTimePerIteration)
        {
            
        }

        public void Start(Func<TResult> action)
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("There is already a task running.");
            }

            SetMethodName(action.Method.Name);
            _task = Task.Run(action, base.cancellationTokenSource.Token);
            base.cycleTimer.Restart();
            base.IsRunning = true;
        }

        public void StartAndIgnoreErrors(Func<TResult> action)
        {
            SetMethodName(action.Method.Name);
            Func<TResult> actionWrapper = () =>
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    IgnoreError(ex);
                }

                return default;
            };

            this.Start(actionWrapper);
        }

        public TResult GetResult()
        {
            if (!_task.IsCompleted)
            {
                throw new InvalidOperationException("The task has not completed.");
            }

            return _task.Result;
        }
    }
}
