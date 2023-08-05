using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Bots;
using EFT.Game.Spawning;
using HarmonyLib;
using LateToTheParty.CoroutineExtensions;
using UnityEngine;

namespace LateToTheParty.Controllers
{
    public class BotGenerator : MonoBehaviour
    {
        public static bool IsSpawningPMCs { get; private set; } = false;
        public static bool IsGeneratingPMCs { get; private set; } = false;
        public static int SpawnedPMCCount { get; private set; } = 0;

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(5);
        private static CancellationTokenSource cancellationTokenSource;
        private static List<GClass628> PMCBots = new List<GClass628>();
        private static Dictionary<SpawnPointParams, Vector3> spawnPositions = new Dictionary<SpawnPointParams, Vector3>();

        public static void Clear()
        {
            if (IsSpawningPMCs)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsSpawningPMCs);
            }

            if (IsGeneratingPMCs)
            {
                TaskWithTimeLimit.WaitForCondition(() => !IsGeneratingPMCs);
            }

            PMCBots.Clear();
            spawnPositions.Clear();
        }

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                Clear();

                SpawnedPMCCount = 0;
                return;
            }

            if (IsSpawningPMCs || IsGeneratingPMCs || (SpawnedPMCCount > 0))
            {
                return;
            }

            // Get the current number of seconds remaining in the raid and calculate the fraction of total raid time remaining
            float escapeTimeSec = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (LocationSettingsController.LastOriginalEscapeTime * 60f) - escapeTimeSec;

            // Do not force spawns if the player spawned late
            if (raidTimeElapsed > 30)
            {
                //return;
            }

            if (PMCBots.Count == 0)
            {
                ConfigController.ForcePMCSpawns();

                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                generateBots(WildSpawnType.assault, EPlayerSide.Savage, BotDifficulty.normal, LocationSettingsController.LastLocationSelected.MaxPlayers);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return;
            }

            // Ensure the raid is progressing before running anything
            if (raidTimeElapsed < 1)
            {
                return;
            }

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            cancellationTokenSource = AccessTools.Field(typeof(BotSpawnerClass), "cancellationTokenSource_0").GetValue(botSpawnerClass) as CancellationTokenSource;
            
            StartCoroutine(SpawnPMCs(botSpawnerClass));
        }

        private async Task generateBots(WildSpawnType wildSpawnType, EPlayerSide side, BotDifficulty botdifficulty, int count)
        {
            IsGeneratingPMCs = true;

            LoggingController.LogInfo("Generating PMC bots...");

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawnerClass), "ginterface17_0").GetValue(botSpawnerClass) as IBotCreator;
            IBotData botData = new GClass629(side, wildSpawnType, botdifficulty, 0f, null);

            for (int i = 0; i < count; i++)
            {
                // This causes a deadlock for some reason
                /*if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }*/

                LoggingController.LogInfo("Generating PMC bot #" + i + "...");
                PMCBots.Add(await GClass628.Create(botData, ibotCreator, 1, botSpawnerClass));
            }

            LoggingController.LogInfo("Generating PMC bots...done.");

            IsGeneratingPMCs = false;
        }

        private IEnumerator SpawnPMCs(BotSpawnerClass botSpawnerClass)
        {
            IsSpawningPMCs = true;

            SpawnPointParams[] spawnPoints = getPMCSpawnPoints(LocationSettingsController.LastLocationSelected.SpawnPointParams, LocationSettingsController.LastLocationSelected.MaxPlayers);
            yield return enumeratorWithTimeLimit.Run(spawnPoints, spawnPMCBot, botSpawnerClass);

            IsSpawningPMCs = false;
        }

        private SpawnPointParams[] getPMCSpawnPoints(SpawnPointParams[] allSpawnPoints, int count)
        {
            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>();

            List<SpawnPointParams> validSpawnPoints = new List<SpawnPointParams>();
            foreach(SpawnPointParams spawnPoint in allSpawnPoints)
            {
                if (!spawnPoint.Categories.Contain(ESpawnCategory.Player))
                {
                    continue;
                }

                Vector3 spawnPosition = spawnPoint.Position.ToUnityVector3();
                Vector3? navMeshPosition = NavMeshController.FindNearestNavMeshPosition(spawnPosition, 10);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogInfo("Cannot spawn PMC at " + spawnPoint.Id + ". No valid NavMesh position nearby.");
                    continue;
                }

                if (!spawnPositions.ContainsKey(spawnPoint))
                {
                    spawnPositions.Add(spawnPoint, navMeshPosition.Value);
                }

                if (Vector3.Distance(navMeshPosition.Value, Singleton<GameWorld>.Instance.MainPlayer.Position) < 20)
                {
                    LoggingController.LogInfo("Cannot spawn PMC at " + spawnPoint.Id + ". Too close to player.");
                    continue;
                }

                validSpawnPoints.Add(spawnPoint);
            }

            SpawnPointParams playerSpawnPoint = getNearestSpawnPoint(Singleton<GameWorld>.Instance.MainPlayer.Position, allSpawnPoints.ToArray());
            LoggingController.LogInfo("Nearest spawn point to player: " + playerSpawnPoint.Position.ToUnityVector3().ToString());
            spawnPoints.Add(playerSpawnPoint);

            for (int s = 0; s < count; s++)
            {
                SpawnPointParams newSpawnPoint = getFurthestSpawnPoint(spawnPoints.ToArray(), validSpawnPoints.ToArray());
                LoggingController.LogInfo("Found furthest spawn point: " + newSpawnPoint.Position.ToUnityVector3().ToString());
                spawnPoints.Add(newSpawnPoint);
            }

            spawnPoints.Remove(playerSpawnPoint);

            return spawnPoints.ToArray();
        }

        private SpawnPointParams getFurthestSpawnPoint(SpawnPointParams[] referenceSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            if (referenceSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The reference spawn-point array is empty.", "referenceSpawnPoints");
            }

            if (allSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The spawn-point array is empty.", "allSpawnPoints");
            }

            Dictionary<SpawnPointParams, float> nearestReferencePoints = new Dictionary<SpawnPointParams, float>();
            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                SpawnPointParams nearestSpawnPoint = referenceSpawnPoints[0];
                float nearestDistance = Vector3.Distance(referenceSpawnPoints[0].Position.ToUnityVector3(), allSpawnPoints[s].Position.ToUnityVector3());

                for (int r = 1; r < referenceSpawnPoints.Length; r++)
                {
                    float distance = Vector3.Distance(referenceSpawnPoints[r].Position.ToUnityVector3(), allSpawnPoints[s].Position.ToUnityVector3());

                    if (distance < nearestDistance)
                    {
                        nearestSpawnPoint = referenceSpawnPoints[r];
                        nearestDistance = distance;
                    }
                }

                nearestReferencePoints.Add(allSpawnPoints[s], nearestDistance);
            }

            return nearestReferencePoints.OrderBy(p => p.Value).Last().Key;
        }

        private SpawnPointParams getFurthestSpawnPoint(Vector3 postition, SpawnPointParams[] allSpawnPoints)
        {
            SpawnPointParams furthestSpawnPoint = allSpawnPoints[0];
            float furthestDistance = Vector3.Distance(postition, furthestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());

                if (distance > furthestDistance)
                {
                    furthestSpawnPoint = allSpawnPoints[s];
                    furthestDistance = distance;
                }
            }

            return furthestSpawnPoint;
        }

        private SpawnPointParams getNearestSpawnPoint(Vector3 postition, SpawnPointParams[] allSpawnPoints)
        {
            SpawnPointParams nearestSpawnPoint = allSpawnPoints[0];
            float nearestDistance = Vector3.Distance(postition, nearestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());

                if (distance < nearestDistance)
                {
                    nearestSpawnPoint = allSpawnPoints[s];
                    nearestDistance = distance;
                }
            }

            return nearestSpawnPoint;
        }

        private void spawnPMCBot(SpawnPointParams spawnPoint, BotSpawnerClass botSpawnerClass)
        {
            if (SpawnedPMCCount > LocationSettingsController.LastLocationSelected.MaxPlayers)
            {
                LoggingController.LogWarning("Max PMC count of " + LocationSettingsController.LastLocationSelected.MaxPlayers + " already reached.");
                return;
            }

            LoggingController.LogInfo("Spawning PMC #" + (SpawnedPMCCount + 1) + " at " + spawnPoint.Id + "...");

            BotZone closestBotZone = botSpawnerClass.GetClosestZone(spawnPositions[spawnPoint], out float dist);
            PMCBots[SpawnedPMCCount].AddPosition(spawnPositions[spawnPoint]);

            LoggingController.LogInfo("Spawning PMC at " + spawnPoint.Id + "...");

            float _originalDelayToCanSpawnSec = spawnPoint.DelayToCanSpawnSec;
            spawnPoint.DelayToCanSpawnSec = 0;

            MethodInfo botSpawnMethod = typeof(BotSpawnerClass).GetMethod("method_11", BindingFlags.Instance | BindingFlags.NonPublic);
            botSpawnMethod.Invoke(botSpawnerClass, new object[] { closestBotZone, PMCBots[SpawnedPMCCount], null, cancellationTokenSource.Token });

            LoggingController.LogInfo("Spawning PMC #" + (SpawnedPMCCount + 1) + " at " + spawnPoint.Id + "...done.");

            SpawnedPMCCount++;

            spawnPoint.DelayToCanSpawnSec = _originalDelayToCanSpawnSec;
        }
    }
}
