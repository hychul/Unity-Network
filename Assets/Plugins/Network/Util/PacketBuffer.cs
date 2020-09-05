using System;
using System.IO;

public class PacketBuffer {

	private MemoryStream memoryStream = new MemoryStream();
	private EndianType endianType;
	private int offset = 0;

	public enum EndianType {
		BigEndian = 0,
		LittleEndian,
	}

	public PacketBuffer() {
		int val = 1;
		byte[] conv = BitConverter.GetBytes(val);
		endianType = (conv[0] == 1) ? EndianType.LittleEndian : EndianType.BigEndian;
	}

	public bool AddBuffer(byte[] data, int size) {
		try {
			memoryStream.Position = offset;
			memoryStream.Write(data, 0, size);
			offset += size;
		} catch {
			memoryStream.Flush();
			offset = 0;
			return false;
		}
		return true;
	}

	public byte[] GetPacketData() {
		int size = ReadPacketSize();

		if (size <= 0) {
			return null;
		}

		byte[] data = new byte[size];

		try {
			memoryStream.Position = sizeof(int);
			memoryStream.Read(data, 0, size);

			byte[] stream = memoryStream.ToArray();
			int restSize = stream.Length - size - sizeof(int);
			byte[] restStream = new byte[restSize];
			Buffer.BlockCopy(stream, size + sizeof(int), restStream, 0, restSize);

			memoryStream = new MemoryStream(restStream);
			offset = restSize;
		} catch {
			return null;
		}
		return data;
	}

	int ReadPacketSize() {
		int size = sizeof(int);
		byte[] data = new byte[size];
		try {
			memoryStream.Position = 0;
			memoryStream.Read(data, 0, size);
		} catch {
			return 0;
		}

		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(data);
		}

		return BitConverter.ToInt32(data, 0);
	}
}
