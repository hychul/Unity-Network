using System;
using System.Collections.Generic;
using System.IO;

public class PacketQueue {

	struct PacketInfo {
		public int offset;
		public int size;
	}

	private MemoryStream memoryStreamBuffer;
	private List<PacketInfo> offsetList;
	private int offset = 0;

	public PacketQueue() {
		memoryStreamBuffer = new MemoryStream();
		offsetList = new List<PacketInfo>();
	}

	public int Enqueue(byte[] data, int size) {
		PacketInfo info = new PacketInfo();

		info.offset = offset;
		info.size = size;

		offsetList.Add(info);

		memoryStreamBuffer.Position = offset;
		memoryStreamBuffer.Write(data, 0, size);
		memoryStreamBuffer.Flush();
		offset += size;

		return size;
	}

	public int Dequeue(ref byte[] buffer, int size) {

		if (offsetList.Count <= 0) {
			return -1;
		}

		PacketInfo info = offsetList[0];

		int dataSize = Math.Min(size, info.size);
		memoryStreamBuffer.Position = info.offset;
		int recvSize = memoryStreamBuffer.Read(buffer, 0, dataSize);

		if (recvSize > 0) {
			offsetList.RemoveAt(0);
		}

		if (offsetList.Count == 0) {
			Clear();
			offset = 0;
		}

		return recvSize;
	}

	public void Clear() {
		byte[] buffer = memoryStreamBuffer.GetBuffer();
		Array.Clear(buffer, 0, buffer.Length);

		memoryStreamBuffer.Position = 0;
		memoryStreamBuffer.SetLength(0);

		offsetList.Clear();
		offset = 0;
	}
}
