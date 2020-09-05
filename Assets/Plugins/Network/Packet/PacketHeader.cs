public struct PacketHeader {
	public int type;
}

public class PacketHeaderSerializer : Serializer {

	public bool Serialize(PacketHeader data) {
		Clear();

		bool ret = true;
		ret &= Serialize(data.type);

		return ret;
	}

	public bool Deserialize(byte[] data, ref PacketHeader serialized) {
		bool ret = SetDeserializedData(data);
		if (!ret) {
			return false;
		}

		if (GetDataSize() < 1) {
			return false;
		}

		int packetType = -1;
		ret &= Deserialize(ref packetType);
		serialized.type = packetType;

		return ret;
	}

	public byte[] SerializeSize(int size) {
		Clear();
		Serialize(size);
		return GetSerializedData();
	}
}
