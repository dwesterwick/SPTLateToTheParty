using EFT;
using EFT.Interactive;
using LiteNetLib.Utils;

namespace LateToThePartyFikaSync
{
	public interface IObjectPacket
	{
		string ObjectName { get; }
	}

    public class ItemPacket : INetSerializable, IObjectPacket
    {
        public string Id;

		public string ObjectName => Id;

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
        }

		public override string ToString() => Id;
    }

    public class DoorSyncPacket : INetSerializable, IObjectPacket
    {
		public WorldInteractiveObject.WorldInteractiveDataPacketStruct Data;

        public string ObjectName => Data.Id;

        public void Deserialize(NetDataReader reader)
		{
			Data = new WorldInteractiveObject.WorldInteractiveDataPacketStruct()
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

	public class InteractPacket : INetSerializable, IObjectPacket
    {
		public string Id;
		public EInteractionType InteractionType;

        public string ObjectName => Id;

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
