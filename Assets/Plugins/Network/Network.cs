using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

// save node infos at outside
public class Network {

	private const int PACKET_SIZE = 1400;

	private SessionTCP sessionTcp = null;
	private SessionUDP sessionUdp = null;
	
	public delegate void PacketHandler(int node, int packetType, byte[] data);
	private Dictionary<int, PacketHandler> packetHandlers = new Dictionary<int, PacketHandler>();

	public delegate void NodeStateHandler(ConnectionType connectionType, int node, NetworkState networkState);
	private NodeStateHandler nodeStateHandler = null;

	public enum ConnectionType {
		UDP = 0,
		TCP,
	}

	/*
	 * +-------------------------+
	 * |       Life Cycle        |
	 * +-------------------------+
	 */

	public Network() {
		sessionTcp = new SessionTCP();
		sessionTcp.SubscribeNetworkState(OnTcpNodeStateChanged);

		sessionUdp = new SessionUDP();
		sessionUdp.SubscribeNetworkState(OnUdpNodeStateChanged);
	}

	void OnApplicationQuit() {
		StopServer();
	}

	/*
	 * +-------------------------+
	 * |          Info          |
	 * +-------------------------+
	 */

	public int GetPacketSize() {
		return PACKET_SIZE;
	}

	public bool IsConnected(int node) {
		if (sessionTcp != null && sessionTcp.IsConnected(node)) {
			return true;
		}
		
		if (sessionUdp != null && sessionUdp.IsConnected(node)) {
			return true;
		}

		return	false;
	}

	public bool IsServer() {
		if (sessionTcp == null) {
			return false;
		}

		return	sessionTcp.IsServer();
	}

	public IPEndPoint GetLocalEndPoint(int node) {
		if (sessionTcp == null) {
			return default(IPEndPoint);
		}

		return sessionTcp.GetLocalEndPoint(node);
	}

	/*
	 * +-------------------------+
	 * |         Server          |
	 * +-------------------------+
	 */

	public bool StartServer(int port, int connectionMax, ConnectionType type) {
		try {
			if (type == ConnectionType.TCP) {
				sessionTcp.StartServer(port, connectionMax);
			} 
			
			if (type == ConnectionType.UDP) {
				sessionUdp.StartServer(port, connectionMax);
			}
		} catch {
			return false;
		}

		return true;
	}

	public void StopServer() {
		if (sessionUdp != null) {
			sessionUdp.StopServer();
		}

		if (sessionTcp != null) {
			sessionTcp.StopServer();
		}
	}

	/*
	 * +-------------------------+
	 * |       Connection        |
	 * +-------------------------+
	 */

	public int Connect(string address, int port, ConnectionType type) {
		int node = -1;

		if (type == ConnectionType.TCP && sessionTcp != null) {
			node = sessionTcp.Connect(address, port);
		}

		if (type == ConnectionType.UDP && sessionUdp != null) {
			node = sessionUdp.Connect(address, port);
		}

		return node;
	}

	public void Disconnect(int node) {	
		if (sessionTcp != null) {
			sessionTcp.Disconnect(node);
		}
		
		if (sessionUdp != null) {
			sessionUdp.Disconnect(node);
		}
	}

	public void Disconnect() {
		if (sessionTcp != null) {
			sessionTcp.Disconnect();
		}
		
		if ( sessionUdp != null) {
			sessionUdp.Disconnect();
		}

		packetHandlers.Clear();
	}

	/*
	 * +-------------------------+
	 * |          Send           |
	 * +-------------------------+
	 */

	public int SendTcp(int node, IPacket packet) {
		int sendSize = 0;
		
		if (sessionTcp == null) {
			return sendSize;
		}

		PacketHeader header = new PacketHeader();
		PacketHeaderSerializer serializer = new PacketHeaderSerializer();
		
		header.type = packet.GetPacketType();

		byte[] headerData = null;
		if (serializer.Serialize(header) == true) {
			headerData = serializer.GetSerializedData();
		}

		byte[] packetData = packet.GetData();

		byte[] data = new byte[headerData.Length + packetData.Length];

		int headerSize = Marshal.SizeOf(typeof(PacketHeader));
		Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
		Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);
		
		sendSize = sessionTcp.Send(node, data, data.Length);

		if (sendSize < 0 && nodeStateHandler != null) {
			NetworkState state = new NetworkState();
			state.node = node;
			state.type = NetEventType.SendError;
			state.result = NetEventResult.Failure;
			nodeStateHandler.Invoke(ConnectionType.TCP, node, state);
		}
		
		return sendSize;
	}

	public int SendUdp(int node, IPacket packet) {
		int sendSize = 0;
		
		if (sessionUdp == null) {
			return sendSize;
		}

		PacketHeader header = new PacketHeader();
		PacketHeaderSerializer headerSerializer = new PacketHeaderSerializer();
		
		header.type = packet.GetPacketType();

		byte[] headerData = null;
		if (headerSerializer.Serialize(header) == true) {
			headerData = headerSerializer.GetSerializedData();
		}

		byte[] packetData = packet.GetData();
		
		byte[] data = new byte[headerData.Length + packetData.Length];
		
		int headerSize = Marshal.SizeOf(typeof(PacketHeader));
		Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
		Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);
		
		sendSize = sessionUdp.Send(node, data, data.Length);

		if (sendSize < 0 && nodeStateHandler != null) {
			NetworkState state = new NetworkState();
			state.node = node;
			state.type = NetEventType.SendError;
			state.result = NetEventResult.Failure;
			nodeStateHandler.Invoke(ConnectionType.UDP, node, state);
		}
		
		return sendSize;
	}

	/*
	 * +-------------------------+
	 * |         Receive         |
	 * +-------------------------+
	 */

	// TODO: Receive from all connected nodes
	public void ReceiveTcp(int node) {
		byte[] packet = new byte[1400];
		if (IsConnected(node) == true) {
			while (sessionTcp.Receive(node, ref packet) > 0) {
				Receive(node, packet);
			}
		}
	}

	public void ReceiveUdp(int node) {
		byte[] packet = new byte[1400];
		if (IsConnected(node) == true) {
			while (sessionUdp.Receive(node, ref packet) > 0) {
				Receive(node, packet);
			}
		}
	}

	// TODO: data param need?
	private void Receive(int node, byte[] data) {
		PacketHeader header = new PacketHeader();
		PacketHeaderSerializer headerSerializer = new PacketHeaderSerializer();

		bool ret = headerSerializer.Deserialize(data, ref header);
		if (ret == false) {
			// Skip invalid header packet
			return;			
		}

		int packetType = (int) header.type;

		if (packetHandlers.ContainsKey(packetType) &&
		    packetHandlers[packetType] != null) {
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			byte[] packetData = new byte[data.Length - headerSize];
			Buffer.BlockCopy(data, headerSize, packetData, 0, packetData.Length);
	
			packetHandlers[packetType](node, header.type, packetData);
		}
	}

	/*
	 * +-------------------------+
	 * |   Network State Event   |
	 * +-------------------------+
	 */

	public void OnTcpNodeStateChanged(int node, NetworkState networkState) {
		if (nodeStateHandler == null) {
			return;
		}

		NetworkState state = new NetworkState();
		state.node = node;
		state.type = networkState.type;
		state.result = NetEventResult.Success;
		nodeStateHandler.Invoke(ConnectionType.TCP, node, state);
	}
	
	public void OnUdpNodeStateChanged(int node, NetworkState networkState) {
		if (nodeStateHandler == null) {
			return;
		}

		NetworkState state = new NetworkState();
		state.node = node;
		state.type = networkState.type;
		state.result = NetEventResult.Success;
		nodeStateHandler.Invoke(ConnectionType.UDP, node, state);
	}

	public void SetNetworkStateHandler(NodeStateHandler handler) {
		this.nodeStateHandler = handler;
	}

	/*
	 * +-------------------------+
	 * |       Packet Event      |
	 * +-------------------------+
	 */

	public void SubscribePacketReceived(int packetType, PacketHandler handler) {
		packetHandlers.Add(packetType, handler);
	}
	
	public void UnsubscribePacketReceived(int packetType) {
		if (packetHandlers.ContainsKey(packetType)) {
			packetHandlers.Remove(packetType);
		}
	}

	public void ClearPacketReceived() {
		packetHandlers.Clear();
	}
}
