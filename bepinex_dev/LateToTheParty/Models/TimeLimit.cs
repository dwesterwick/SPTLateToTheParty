using LateToTheParty.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    internal abstract class MethodWithTimeLimit
    {
        public bool IsRunning { get; protected set; } = false;
        public bool IsCompleted { get; protected set; } = false;

        protected Stopwatch cycleTimer = new Stopwatch();
        protected double maxTimePerIteration;
        protected bool stopRequested = false;
        protected bool hadToWait = false;

        private string methodName = "";

        protected MethodWithTimeLimit(double _maxTimePerIteration)
        {
            maxTimePerIteration = _maxTimePerIteration;
        }

        protected void SetMethodName(string _methodName)
        {
            if (methodName.Length == 0)
            {
                methodName = _methodName;
            }
        }

        protected IEnumerator WaitForNextFrame(string extraDetail = "")
        {
            hadToWait = true;
            LoggingController.LogWarning(messageTextPrefix(extraDetail) + messageTextSuffix(), true);

            yield return null;
            cycleTimer.Restart();
        }

        protected void FinishedWaitingForFrames(string extraDetail = "")
        {
            if (hadToWait)
            {
                LoggingController.LogWarning(messageTextPrefix(extraDetail) + "done." + messageTextSuffix(), true);
            }
        }

        protected void AbortWaitingForFrames(string extraDetail = "")
        {
            if (IsRunning)
            {
                LoggingController.LogWarning(messageTextPrefix(extraDetail) + "aborted." + messageTextSuffix(), true);
            }
        }

        private string messageTextPrefix(string extraDetail = "")
        {
            string message = "Waiting ";
            if (methodName.Length > 0)
            {
                message += "for " + methodName + " ";

                if (extraDetail.Length > 0)
                {
                    message += "(" + extraDetail + ") ";
                }
            }
            message += "until next frame...";

            return message;
        }

        private string messageTextSuffix()
        {
            return " (Cycle time: " + cycleTimer.ElapsedMilliseconds + ")";
        }
    }
}
