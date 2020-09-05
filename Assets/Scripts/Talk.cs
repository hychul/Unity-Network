using System.Text;
using UnityEngine;

public struct Talk {
    public int size;
	public string text;
}

public class TalkPacket : IPacket {

	private Talk talk;

	public TalkPacket(string text) {
		this.talk = new Talk();
        this.talk.text = text;
	}

	public TalkPacket(byte[] data) {
		talk = new Talk();

		TalkSerializer serializer = new TalkSerializer();

		serializer.Deserialize(data, ref talk);
	}

	public int GetPacketType() {
		return 1;
	}

	public Talk GetTalk() {
		return talk;
	}

	public byte[] GetData() {
        NetworkLogger.Log("GetData");
		TalkSerializer serializer = new TalkSerializer();

		serializer.Serialize(talk);

		return serializer.GetSerializedData();
	} 

	class TalkSerializer : Serializer {
		public bool Serialize(Talk talk) {
			bool ret = true;
            
            int size = Encoding.Default.GetBytes(talk.text).Length;

			ret &= Serialize(size);
            ret &= Serialize(talk.text, size);

			return ret;
		}

		public bool Deserialize(byte[] data, ref Talk talk) {
			bool ret = SetDeserializedData(data);

			if (ret == false) {
				return false;
			}

			ret &= Deserialize(ref talk.size);
            ret &= Deserialize(ref talk.text, talk.size);

			return ret;
		}
	}
}
