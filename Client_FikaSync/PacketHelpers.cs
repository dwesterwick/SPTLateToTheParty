using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace LateToTheParty
{
    internal static class PacketHelpers
    {
        internal static void WriteReceivedMessage<T>(this T packet) where T : INetSerializable, IObjectPacket
        {
#if DEBUG
            LateToThePartyFikaSyncPlugin.PluginLogger.LogDebug($"Received {packet.GetType().Name} for {packet.ObjectName}");
#endif
        }

        internal static void WriteSendingMessage<T>(this T packet) where T : INetSerializable, IObjectPacket
        {
#if DEBUG
            LateToThePartyFikaSyncPlugin.PluginLogger.LogDebug($"Sending {packet.GetType().Name} for {packet.ObjectName}");
#endif
        }

        internal static void SendToAllClients<T>(this T packet) where T : INetSerializable, IObjectPacket
        {
            if (FikaBackendUtils.IsClient) // safeguard
            {
                return;
            }

            if (Singleton<IFikaNetworkManager>.Instance is FikaServer server)
            {
                packet.WriteSendingMessage();
                server.SendData(ref packet, Fika.Core.Networking.LiteNetLib.DeliveryMethod.ReliableOrdered);

                return;
            }

            LateToThePartyFikaSyncPlugin.PluginLogger.LogError($"NetworkManager was not a server when trying to send {packet.GetType().Name}");
        }

        internal static bool TryGetGameWorld(out GameWorld? gameWorld)
        {
            gameWorld = null;

            if (Singleton<GameWorld>.Instance == null)
            {
                LateToThePartyFikaSyncPlugin.PluginLogger.LogError("GameWorld is null");
                return false;
            }

            gameWorld = Singleton<GameWorld>.Instance;

            return true;
        }

        internal static bool TryGetWorldInteractiveObject(string id, out WorldInteractiveObject? worldInteractiveObject)
        {
            worldInteractiveObject = null;

            if (!TryGetGameWorld(out GameWorld? gameWorld))
            {
                return false;
            }

            worldInteractiveObject = gameWorld?.FindDoor(id);
            if (worldInteractiveObject == null)
            {
                LateToThePartyFikaSyncPlugin.PluginLogger.LogError("Could not find WorldInteractiveObject: " + id);
                return false;
            }

            return true;
        }

        internal static bool TryGetItem(string id, out Item? item)
        {
            item = null;

            if (!TryGetGameWorld(out GameWorld? gameWorld))
            {
                return false;
            }

            item = gameWorld?.FindItemById(id).Value;
            if (item == null)
            {
                LateToThePartyFikaSyncPlugin.PluginLogger.LogError("Could not find Item: " + id);
                return false;
            }

            return true;
        }
    }
}
