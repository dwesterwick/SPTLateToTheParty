using Comfort.Common;
using EFT;
using EFT.Interactive;
using LateToTheParty.Controllers;
using LateToTheParty.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.BotLogic
{
    public class BotQuestController : MonoBehaviour
    {
        public static bool IsFindingTriggers = false;
        public static bool HaveTriggersBeenFound = false;

        public void Clear()
        {
            HaveTriggersBeenFound = false;
        }

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                Clear();
                return;
            }

            if (IsFindingTriggers || HaveTriggersBeenFound)
            {
                return;
            }

            IsFindingTriggers = true;

            TriggerWithId[] allTriggers = FindObjectsOfType<TriggerWithId>();
            foreach (TriggerWithId trigger in allTriggers)
            {
                LoggingController.LogInfo("Found trigger " + trigger.Id + " of type " + trigger.GetType());

                Collider triggerCollider = trigger.gameObject.GetComponent<Collider>();

                if (triggerCollider == null)
                {
                    LoggingController.LogInfo("Trigger " + trigger.Id + " has no collider");
                    continue;
                }

                Vector3[] triggerColliderBounds = PathRender.GetBoundingBoxPoints(triggerCollider.bounds);

                PathVisualizationData triggerVisual = new PathVisualizationData("Trigger_" + trigger.Id, triggerColliderBounds, Color.white);
                PathRender.AddOrUpdatePath(triggerVisual);
            }

            IsFindingTriggers = false;
            HaveTriggersBeenFound = true;
        }
    }
}
