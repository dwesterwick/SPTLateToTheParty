using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding.Events;
using Fika.Core.Modding;
using Fika.Core.Networking;
using LateToTheParty.Helpers;

namespace LateToThePartyFikaSync
{
    // Huge thanks to Lacyway for creating this plugin!

    [BepInDependency("com.fika.core", "1.1.3")]
    [BepInDependency("com.DanW.LateToTheParty", "2.7.1")]
    [BepInPlugin("com.DanW.LateToThePartyFikaSync", "LateToThePartyFikaSyncPlugin", "1.0.0.0")]
    internal class LateToThePartyFikaSyncPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PluginLogger;

        protected void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"{nameof(LateToThePartyFikaSyncPlugin)} has been loaded.");

            new GameWorldInitLevelPatch().Enable();

            InteractiveObjectHelpers.OnExecuteInteraction += InteractiveObjectHelpers_OnExecuteInteraction;
            InteractiveObjectHelpers.OnForceDoorState += InteractiveObjectHelpers_OnForceDoorState;

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent eventInfo)
        {
            if (eventInfo.Manager is FikaClient client)
            {
                client.RegisterPacket<DoorSyncPacket>(OnDoorSyncPacketReceived);
                client.RegisterPacket<InteractPacket>(OnInteractPacketReceived);
            }
        }

        private void OnDoorSyncPacketReceived(DoorSyncPacket packet)
        {
#if DEBUG
            Logger.LogWarning("Received packet for " + packet.Data.Id);
#endif
            if (Singleton<GameWorld>.Instance != null)
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                WorldInteractiveObject interactiveObject = gameWorld.FindDoor(packet.Data.Id);
                if (interactiveObject != null)
                {
                    interactiveObject.SetInitialSyncState(packet.Data);
                    return;
                }
                Logger.LogError("OnDoorSyncPacketReceived::Could not find door: " + packet.Data.Id);
                return;
            }
            Logger.LogError("OnDoorSyncPacketReceived::GameWorld was null");
        }

        private void OnInteractPacketReceived(InteractPacket packet)
        {
#if DEBUG
            Logger.LogWarning("Received interact packet for " + packet.Id);
#endif
            if (Singleton<GameWorld>.Instance != null)
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                WorldInteractiveObject interactiveObject = gameWorld.FindDoor(packet.Id);
                if (interactiveObject != null)
                {
                    interactiveObject.Interact(packet.InteractionType);
                    return;
                }
                Logger.LogError("OnDoorSyncPacketReceived::Could not find door: " + packet.Id);
                return;
            }
            Logger.LogError("OnDoorSyncPacketReceived::GameWorld was null");
        }

        private void InteractiveObjectHelpers_OnForceDoorState(WorldInteractiveObject worldInteractiveObject, EDoorState doorState)
        {
            if (FikaBackendUtils.IsClient) // safeguard
            {
                return;
            }
#if DEBUG
            Logger.LogWarning("Sending packet for " + worldInteractiveObject.Id);
#endif
            DoorSyncPacket packet = new DoorSyncPacket()
            {
                Data = worldInteractiveObject.GetStatusInfo()
            };

            if (Singleton<IFikaNetworkManager>.Instance is FikaServer server)
            {
                server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                return;
            }

            Logger.LogError("InteractiveObjectHelpers_OnForceDoorState::NetworkManager was not a server?");
        }

        private void InteractiveObjectHelpers_OnExecuteInteraction(WorldInteractiveObject worldInteractiveObject, InteractionResult interactionResult)
        {
            if (FikaBackendUtils.IsClient) // safeguard
            {
                return;
            }
#if DEBUG
            Logger.LogWarning("Sending interact packet for " + worldInteractiveObject.Id);
#endif
            InteractPacket packet = new InteractPacket()
            {
                Id = worldInteractiveObject.Id,
                InteractionType = interactionResult.InteractionType
            };

            if (Singleton<IFikaNetworkManager>.Instance is FikaServer server)
            {
                server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                return;
            }

            Logger.LogError("InteractiveObjectHelpers_OnExecuteInteraction::NetworkManager was not a server?");
        }
    }
}
