using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using LateToTheParty.Controllers;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.BotLogic
{
    public class BotQuestController : MonoBehaviour
    {
        public static bool IsFindingTriggers = false;
        public static bool HaveTriggersBeenFound = false;

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(5);
        private static RawQuestClass[] allQuestTemplates = new RawQuestClass[0];
        private static Dictionary<RawQuestClass, string[]> QuestZoneIDs = new Dictionary<RawQuestClass, string[]>();

        public void Clear()
        {
            if (IsFindingTriggers)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsFindingTriggers);
            }

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
            LoadQuests();
            FindTriggers();
            IsFindingTriggers = false;
        }

        private void LoadQuests()
        {
            if (allQuestTemplates.Length == 0)
            {
                allQuestTemplates = ConfigController.GetAllQuestTemplates();
            }

            QuestZoneIDs.Clear();
            foreach (RawQuestClass quest in allQuestTemplates)
            {
                EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
                if (!quest.Conditions.ContainsKey(eQuestStatus))
                {
                    continue;
                }

                List<string> zoneIDs = new List<string>();
                foreach (Condition condition in quest.Conditions[eQuestStatus])
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(condition));
                }

                //LoggingController.LogInfo("Zone ID's for quest \"" + quest.Name + "\": " + string.Join(",", zoneIDs));
                QuestZoneIDs.Add(quest, zoneIDs.ToArray());
            }
        }

        private IEnumerable<string> getAllZoneIDsForQuestCondition(Condition condition)
        {
            List<string> zoneIDs = new List<string>();

            ConditionZone conditionZone = condition as ConditionZone;
            if (conditionZone != null)
            {
                zoneIDs.Add(conditionZone.zoneId);
            }

            ConditionInZone conditionInZone = condition as ConditionInZone;
            if (conditionInZone != null)
            {
                zoneIDs.AddRange(conditionInZone.zoneIds);
            }

            foreach(Condition childCondition in condition.ChildConditions)
            {
                zoneIDs.AddRange(getAllZoneIDsForQuestCondition(childCondition));
            }

            return zoneIDs.Distinct();
        }

        private void FindTriggers()
        {
            TriggerWithId[] allTriggers = FindObjectsOfType<TriggerWithId>();
            foreach (TriggerWithId trigger in allTriggers)
            {
                RawQuestClass[] matchingQuests = QuestZoneIDs.Where(q => q.Value.Contains(trigger.Id)).Select(q => q.Key).ToArray();

                if (matchingQuests.Length == 0)
                {
                    continue;
                }

                LoggingController.LogInfo("Found trigger " + trigger.Id + " of type " + trigger.GetType() + " for quest(s): " + string.Join(", ", matchingQuests.Select(q => q.Name)));

                Collider triggerCollider = trigger.gameObject.GetComponent<Collider>();
                if (triggerCollider == null)
                {
                    LoggingController.LogError("Trigger " + trigger.Id + " has no collider");
                    continue;
                }

                Vector3[] triggerColliderBounds = PathRender.GetBoundingBoxPoints(triggerCollider.bounds);

                if (ConfigController.Config.Debug.LootPathVisualization.Enabled)
                {
                    PathVisualizationData triggerVisual = new PathVisualizationData("Trigger_" + trigger.Id, triggerColliderBounds, Color.white);
                    PathRender.AddOrUpdatePath(triggerVisual);
                }
            }

            HaveTriggersBeenFound = true;
        }
    }
}
