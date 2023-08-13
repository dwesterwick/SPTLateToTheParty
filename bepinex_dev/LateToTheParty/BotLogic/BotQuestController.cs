using Aki.Common.Http;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using LateToTheParty.Controllers;
using LateToTheParty.CoroutineExtensions;
using LateToTheParty.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.BotLogic
{
    public class BotQuestController : MonoBehaviour
    {
        public static bool IsFindingTriggers = false;
        public static bool HaveTriggersBeenFound = false;

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(5);
        private static List<Quest> allQuests = new List<Quest>();
        private static List<string> zoneIDsInLocation = new List<string>();

        public void Clear()
        {
            if (IsFindingTriggers)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsFindingTriggers);
            }

            foreach (Quest quest in allQuests)
            {
                quest.ClearPositionData();
            }

            zoneIDsInLocation.Clear();

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

            StartCoroutine(LoadAllQuests());
        }

        public static Quest findQuest(string questID)
        {
            IEnumerable<Quest> matchingQuests = allQuests.Where(q => q.TemplateId == questID);
            if (matchingQuests.Count() == 0)
            {
                return null;
            }

            return matchingQuests.First();
        }

        public static Quest getRandomQuestInCurrentLocation()
        {
            IEnumerable<Quest> applicableQuests = allQuests
                .Where(q => q.ZoneIDs.Length > 0)
                .Where(q => q.ZoneIDs.Any(z => zoneIDsInLocation.Contains(z)));
            
            if (applicableQuests.Count() == 0)
            {
                return null;
            }

            return applicableQuests.Random();
        }

        public static string getRandomZoneIDForQuest(Quest quest)
        {
            string[] zoneIDs = quest.ZoneIDs;
            if (zoneIDs.Length == 0)
            {
                return null;
            }

            return zoneIDs.Random();
        }

        public static string getRandomZoneIDForQuest(string questID)
        {
            Quest quest = findQuest(questID);
            if (quest == null)
            {
                return null;
            }

            return getRandomZoneIDForQuest(quest);
        }

        private IEnumerator LoadAllQuests()
        {
            IsFindingTriggers = true;

            try
            {
                if (allQuests.Count == 0)
                {
                    RawQuestClass[] allQuestTemplates = ConfigController.GetAllQuestTemplates();
                    foreach (RawQuestClass questTemplate in allQuestTemplates)
                    {
                        allQuests.Add(new Quest(questTemplate));
                    }

                    enumeratorWithTimeLimit.Reset();
                    yield return enumeratorWithTimeLimit.Run(allQuests, LoadQuest);
                }

                IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();
                //IEnumerable<Type> allTriggerTypes = allTriggers.Select(t => t.GetType()).Distinct();
                //LoggingController.LogInfo("Found " + allTriggers.Count() + " triggers of types: " + string.Join(", ", allTriggerTypes));

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allTriggers, ProcessTrigger);

                //IEnumerable<LootItem> allLoot = FindObjectsOfType<LootItem>(); <-- this does not work for inactive quest items!
                IEnumerable<LootItem> allItems = Singleton<GameWorld>.Instance.LootItems.Where(i => i.Item != null).Distinct(i => i.TemplateId);

                //IEnumerable<LootItem> allQuestItems = allItems.Where(l => l.Item.QuestItem);
                //LoggingController.LogInfo("Quest items: " + string.Join(", ", allQuestItems.Select(l => l.Item.LocalizedName())));

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allQuests, LocateQuestItems, allItems);

                LoggingController.LogInfo("Finished loading quest data.");

                HaveTriggersBeenFound = true;
            }
            finally
            {
                IsFindingTriggers = false;
            }
        }

        private void LocateQuestItems(Quest quest, IEnumerable<LootItem> allLoot)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    string target = "";
                    ConditionFindItem conditionFindItem = condition as ConditionFindItem;
                    if (conditionFindItem != null)
                    {
                        target = conditionFindItem.target[0];
                    }
                    if (target == "")
                    {
                        continue;
                    }

                    if (quest.FindLootItem(target) != null)
                    {
                        continue;
                    }

                    IEnumerable<LootItem> matchingLootItems = allLoot.Where(l => l.TemplateId == target);
                    if (matchingLootItems.Count() == 0)
                    {
                        continue;
                    }

                    LootItem item = matchingLootItems.First();
                    if (item.Item.QuestItem)
                    {
                        Collider itemCollider = item.GetComponent<Collider>();
                        if (itemCollider == null)
                        {
                            LoggingController.LogError("Quest item " + item.Item.LocalizedName() + " has no collider");
                            return;
                        }

                        Vector3? navMeshTargetPoint = NavMeshController.FindNearestNavMeshPosition(itemCollider.bounds.center, 2f);
                        if (!navMeshTargetPoint.HasValue)
                        {
                            LoggingController.LogError("Cannot find NavMesh point for quest item " + item.Item.LocalizedName());
                            return;
                        }

                        quest.AddItemAndPosition(item, navMeshTargetPoint.Value);
                        LoggingController.LogInfo("Found " + item.Item.LocalizedName() + " for quest " + quest.Name);
                    }
                }
            }
        }

        private void LoadQuest(Quest quest)
        {
            List<string> zoneIDs = new List<string>();
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(condition));
                }
            }

            //LoggingController.LogInfo("Zone ID's for quest \"" + quest.Name + "\": " + string.Join(",", zoneIDs));
            quest.AddZonesWithoutPosition(zoneIDs);

            int minLevel = getMinLevelForQuest(quest);
            //LoggingController.LogInfo("Min level for quest \"" + quest.Name + "\": " + minLevel);
            quest.MinLevel = minLevel;
        }

        private int getMinLevelForQuest(Quest quest)
        {
            int minLevel = quest.Template.Level;
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForStart;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    ConditionLevel conditionLevel = condition as ConditionLevel;
                    if (conditionLevel != null)
                    {
                        if ((conditionLevel.compareMethod == ECompareMethod.MoreOrEqual) || (conditionLevel.compareMethod == ECompareMethod.More))
                        {
                            if (conditionLevel.value > minLevel)
                            {
                                minLevel = (int)conditionLevel.value;
                            }
                        }
                    }

                    ConditionQuest conditionQuest = condition as ConditionQuest;
                    if (conditionQuest != null)
                    {
                        string preReqQuestID = conditionQuest.target;
                        Quest preReqQuest = allQuests.First(q => q.Template.Id == preReqQuestID);

                        int minLevelForPreReqQuest = getMinLevelForQuest(preReqQuest);
                        if (minLevelForPreReqQuest > minLevel)
                        {
                            minLevel = minLevelForPreReqQuest;
                        }
                    }
                }
            }

            return minLevel;
        }

        private IEnumerable<string> getAllZoneIDsForQuestCondition(Condition condition)
        {
            List<string> zoneIDs = new List<string>();

            ConditionZone conditionZone = condition as ConditionZone;
            if (conditionZone != null)
            {
                zoneIDs.Add(conditionZone.zoneId);
            }

            ConditionLeaveItemAtLocation conditionLeaveItemAtLocation = condition as ConditionLeaveItemAtLocation;
            if (conditionLeaveItemAtLocation != null)
            {
                zoneIDs.Add(conditionLeaveItemAtLocation.zoneId);
            }

            ConditionPlaceBeacon conditionPlaceBeacon = condition as ConditionPlaceBeacon;
            if (conditionPlaceBeacon != null)
            {
                zoneIDs.Add(conditionPlaceBeacon.zoneId);
            }

            ConditionLaunchFlare conditionLaunchFlare = condition as ConditionLaunchFlare;
            if (conditionLaunchFlare != null)
            {
                zoneIDs.Add(conditionLaunchFlare.zoneID);
            }

            ConditionVisitPlace conditionVisitPlace = condition as ConditionVisitPlace;
            if (conditionVisitPlace != null)
            {
                zoneIDs.Add(conditionVisitPlace.target);
            }

            ConditionInZone conditionInZone = condition as ConditionInZone;
            if (conditionInZone != null)
            {
                zoneIDs.AddRange(conditionInZone.zoneIds);
            }

            ConditionCounterCreator conditionCounterCreator = condition as ConditionCounterCreator;
            if (conditionCounterCreator != null)
            {
                foreach (Condition childCondition in conditionCounterCreator.counter.conditions)
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(childCondition));
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                zoneIDs.AddRange(getAllZoneIDsForQuestCondition(childCondition));
            }

            return zoneIDs.Distinct();
        }

        private void ProcessTrigger(TriggerWithId trigger)
        {
            zoneIDsInLocation.Add(trigger.Id);

            Quest[] matchingQuests = allQuests.Where(q => q.ZoneIDs.Contains(trigger.Id)).ToArray();
            if (matchingQuests.Length == 0)
            {
                return;
            }

            Collider triggerCollider = trigger.gameObject.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                LoggingController.LogError("Trigger " + trigger.Id + " has no collider");
                return;
            }

            float searchDistance = Math.Max(2f, triggerCollider.bounds.extents.y);
            Vector3? navMeshTargetPoint = NavMeshController.FindNearestNavMeshPosition(triggerCollider.bounds.center, searchDistance);
            if (!navMeshTargetPoint.HasValue)
            {
                LoggingController.LogError("Cannot find NavMesh point for trigger " + trigger.Id + ". Search distance: " + searchDistance);
                return;
            }

            foreach (Quest quest in matchingQuests)
            {
                LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest);
                quest.AddZoneAndPosition(trigger.Id, navMeshTargetPoint.Value);
            }

            if (ConfigController.Config.Debug.LootPathVisualization.Enabled)
            {
                Vector3[] triggerColliderBounds = PathRender.GetBoundingBoxPoints(triggerCollider.bounds);
                PathVisualizationData triggerVisual = new PathVisualizationData("Trigger_" + trigger.Id, triggerColliderBounds, Color.white);
                PathRender.AddOrUpdatePath(triggerVisual);
            }
        }
    }
}
