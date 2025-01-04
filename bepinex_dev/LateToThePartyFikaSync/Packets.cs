using EFT;
using EFT.Interactive;
using LiteNetLib.Utils;

namespace LateToThePartyFikaSync
{
	public class DoorSyncPacket : INetSerializable
	{
		public WorldInteractiveObject.GStruct415 Data;

		public void Deserialize(NetDataReader reader)
		{
			Data = new WorldInteractiveObject.GStruct415()
			{
				NetId = reader.GetInt(),
				Id = reader.GetString(),
				Angle = reader.GetInt(),
				IsBroken = reader.GetBool(),
				State = reader.GetByte(),
				Updated = reader.GetBool()
			};
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Data.NetId);
			writer.Put(Data.Id);
			writer.Put(Data.Angle);
			writer.Put(Data.IsBroken);
			writer.Put(Data.State);
			writer.Put(Data.Updated);
		}
	}

	public class InteractPacket : INetSerializable
	{
		public string Id;
		public EInteractionType InteractionType;

		public void Deserialize(NetDataReader reader)
		{
			Id = reader.GetString();
			InteractionType = (EInteractionType)reader.GetByte();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Id);
			writer.Put((byte)InteractionType);
		}
	}
}
