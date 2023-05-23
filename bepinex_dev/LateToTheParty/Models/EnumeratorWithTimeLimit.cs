using EFT.InventoryLogic;
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
    internal class EnumeratorWithTimeLimit : MethodWithTimeLimit
    {
        public EnumeratorWithTimeLimit(double _maxTimePerIteration) : base(_maxTimePerIteration)
        {
            
        }

        public IEnumerator Run<TItem>(IEnumerable<TItem> collection, Action<TItem> collectionItemAction)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item); };
            yield return Run_Internal(collection, action);
        }

        public IEnumerator Run<TItem, T1>(IEnumerable<TItem> collection, Action<TItem, T1> collectionItemAction, T1 param1)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item, param1); };
            yield return Run_Internal(collection, action);
        }

        public IEnumerator Run<TItem, T1, T2>(IEnumerable<TItem> collection, Action<TItem, T1, T2> collectionItemAction, T1 param1, T2 param2)
        {
            SetMethodName(collectionItemAction.Method.Name);
            Action<TItem> action = (item) => { collectionItemAction(item, param1, param2); };
            yield return Run_Internal(collection, action);
        }

        private IEnumerator Run_Internal<TItem>(IEnumerable<TItem> collection, Action<TItem> action)
        {
            if (base.IsRunning)
            {
                throw new InvalidOperationException("There is already a coroutine running.");
            }

            base.IsCompleted = false;
            base.IsRunning = true;
            base.hadToWait = false;

            base.cycleTimer.Restart();

            foreach (TItem item in collection)
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {
                    LoggingController.LogError("Aborting coroutine iteration for " + item.ToString());
                    LoggingController.LogError(ex.ToString());
                }

                if (base.cycleTimer.ElapsedMilliseconds > base.maxTimePerIteration)
                {
                    yield return base.WaitForNextFrame(typeof(TItem).Name);
                }

                if (base.stopRequested)
                {
                    base.IsRunning = false;
                    yield break;
                }
            }

            base.IsRunning = false;
            base.IsCompleted = true;

            base.FinishedWaitingForFrames(typeof(TItem).Name);
        }

        public void Abort()
        {
            stopRequested = true;
            base.AbortWaitingForFrames();
        }
    }
}
