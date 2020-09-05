public struct NetworkEntity {
	public int id;

	public short payloadSize;
	public byte[] payload;
}

public class NetworkEntityPacket : IPacket {

	private NetworkEntity networkEntity;

	public NetworkEntityPacket(NetworkEntity networkEntity) {
		this.networkEntity = networkEntity;
	}

	public NetworkEntityPacket(byte[] data) {
		networkEntity = new NetworkEntity();

		NetworkEntitySerializer serializer = new NetworkEntitySerializer();

		serializer.Deserialize(data, ref networkEntity);
	}

	public int GetPacketType() {
		return 0;
	}

	public NetworkEntity GetNetworkEntity() {
		return networkEntity;
	}

	public byte[] GetData() {
		NetworkEntitySerializer serializer = new NetworkEntitySerializer();

		serializer.Serialize(networkEntity);

		return serializer.GetSerializedData();
	} 

	class NetworkEntitySerializer : Serializer {
		public bool Serialize(NetworkEntity networkEntity) {
			bool ret = true;

			ret &= Serialize(networkEntity.id);
			ret &= Serialize(networkEntity.payloadSize);
			ret &= WriteRawBuffer(networkEntity.payload, networkEntity.payloadSize);

			return ret;
		}

		public bool Deserialize(byte[] data, ref NetworkEntity networkEntity) {
			bool ret = SetDeserializedData(data);

			if (ret == false) {
				return false;
			}

			ret &= Deserialize(ref networkEntity.id);
			ret &= Deserialize(ref networkEntity.payloadSize);
			networkEntity.payload = new byte[networkEntity.payloadSize];
			ret &= ReadRawBuffer(ref networkEntity.payload, networkEntity.payloadSize);

			return ret;
		}
	}
}
