using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    public class TaskWithTimeLimit<TResult>
    {
        private CancellationTokenSource cancellationTokenSource;
        private Task<TResult> task;

        public TaskWithTimeLimit(Func<TResult> action)
        {
            cancellationTokenSource = new CancellationTokenSource();
            task = Task.Run(action, cancellationTokenSource.Token);
        }

        public IEnumerator WaitForTask()
        {
            while (isRunning())
            {
                Task.Delay(1);
                yield return null;
            }
        }

        public void Abort()
        {
            cancellationTokenSource.Cancel();
        }

        public TResult GetResult()
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The task has not completed.");
            }

            return task.Result;
        }

        private bool isRunning()
        {
            if (task.Status == TaskStatus.Running)
            {
                return false;
            }

            return false;
        }
    }
}
