using System;

public struct Ping {
	public double timestamp;
}

public class PingPacket : IPacket {

	private Ping ping;

	public PingPacket() {
		ping = new Ping();
		ping.timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
	}

	public PingPacket(byte[] data) {
		ping = new Ping();

		PingSerializer serializer = new PingSerializer();

		serializer.Deserialize(data, ref ping);
	}

	public int GetPacketType() {
		return -1;
	}

	public Ping GetPing() {
		return ping;
	}

	public byte[] GetData() {
		PingSerializer serializer = new PingSerializer();

		serializer.Serialize(ping);

		return serializer.GetSerializedData();
	} 

	class PingSerializer : Serializer {
		public bool Serialize(Ping ping) {
			bool ret = true;

			ret &= Serialize(ping.timestamp);

			return ret;
		}

		public bool Deserialize(byte[] data, ref Ping ping) {
			bool ret = SetDeserializedData(data);

			if (ret == false) {
				return false;
			}

			ret &= Deserialize(ref ping.timestamp);

			return ret;
		}
	}
}
