using BepInEx;
using BepInEx.Logging;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LateToTheParty.Helpers;
using LateToTheParty.Helpers.Loot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty
{
    [BepInDependency("com.fika.core", "2.1.1")]
    [BepInDependency(ModInfo.MODNAME, ModInfo.MOD_VERSION)]
    [BepInPlugin(ModInfo.GUID + "fikasync", ModInfo.MODNAME + "FikaSync", ModInfo.MOD_VERSION)]
    internal class LateToThePartyFikaSyncPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PluginLogger = null!;

        protected void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"{nameof(LateToThePartyFikaSyncPlugin)} has been loaded.");

            new GameWorldInitLevelPatch().Enable();

            InteractiveObjectHelpers.OnExecuteInteraction += InteractiveObjectHelpers_OnExecuteInteraction;
            InteractiveObjectHelpers.OnForceDoorState += InteractiveObjectHelpers_OnForceDoorState;
            LootDestructionHelpers.OnDestroyLoot += LootDestructionHelpers_OnLootDestructionRequested;

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent eventInfo)
        {
            if (eventInfo.Manager is FikaClient client)
            {
                client.RegisterPacket<DoorSyncPacket>(OnDoorSyncPacketReceived);
                client.RegisterPacket<InteractPacket>(OnInteractPacketReceived);
                client.RegisterPacket<ItemPacket>(OnItemPacketReceived);
            }
        }

        private void OnDoorSyncPacketReceived(DoorSyncPacket packet)
        {
            packet.WriteReceivedMessage();

            if (PacketHelpers.TryGetWorldInteractiveObject(packet.Data.Id, out WorldInteractiveObject? worldInteractiveObject) && (worldInteractiveObject != null))
            {
                worldInteractiveObject.SetInitialSyncState(packet.Data);
                return;
            }

            Logger.LogError("Could not set the sync state of door: " + packet.Data.Id);
        }

        private void OnInteractPacketReceived(InteractPacket packet)
        {
            packet.WriteReceivedMessage();

            if (PacketHelpers.TryGetWorldInteractiveObject(packet.Id, out WorldInteractiveObject? worldInteractiveObject) && (worldInteractiveObject != null))
            {
                worldInteractiveObject.PrepareInteraction();
                worldInteractiveObject.Interact(packet.InteractionType);
                return;
            }

            Logger.LogError("Could not interact with door: " + packet.Id);
        }

        private void OnItemPacketReceived(ItemPacket packet)
        {
            packet.WriteReceivedMessage();

            if (PacketHelpers.TryGetItem(packet.Id, out Item? item))
            {
                item?.DestroyViaLTTP();
                return;
            }

            Logger.LogError("Could not destroy loot with ID: " + packet.Id);
        }

        private void InteractiveObjectHelpers_OnForceDoorState(WorldInteractiveObject worldInteractiveObject, EDoorState doorState)
        {
            DoorSyncPacket packet = new DoorSyncPacket()
            {
                Data = worldInteractiveObject.GetStatusInfo()
            };

            packet.SendToAllClients();
        }

        private void InteractiveObjectHelpers_OnExecuteInteraction(WorldInteractiveObject worldInteractiveObject, InteractionResult interactionResult)
        {
            InteractPacket packet = new InteractPacket()
            {
                Id = worldInteractiveObject.Id,
                InteractionType = interactionResult.InteractionType
            };

            packet.SendToAllClients();
        }

        private void LootDestructionHelpers_OnLootDestructionRequested(Item item)
        {
            ItemPacket packet = new ItemPacket()
            {
                Id = item.Id,
            };

            packet.SendToAllClients();
        }
    }
}
