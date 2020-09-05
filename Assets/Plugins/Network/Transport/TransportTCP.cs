using System;
using System.Net;
using System.Net.Sockets;

public class TransportTCP : ITransport {

	private const int PACKET_SIZE = 1400;

	private int node = -1;

	private Socket socket = null;

	private	bool isConnected = false;

	private PacketQueue sendQueue = new PacketQueue();
	private PacketQueue	recvQueue = new PacketQueue();

	public string transportName = "";

	private NetworkStateHandler networkStateHandler;

	/*
	* +-------------------------+
	* |          ?????          |
	* +-------------------------+
	*/

	public TransportTCP() { }

	public TransportTCP(Socket socket, string name) {
		this.socket = socket;
		this.transportName = name;
	}

    public void Initialize(Socket socket) {
		this.socket = socket;
		this.isConnected = true;
    }

    public void Terminate() {
		this.socket = null;
    }

	/*
	* +-------------------------+
	* |           Info          |
	* +-------------------------+
	*/

    public int GetNode() {
        return node;
    }

    public void SetNode(int node) {
        this.node = node;
    }

	public IPEndPoint GetLocalEndPoint() {
		if (socket == null) {
			return default(IPEndPoint);
		}
		
		return socket.LocalEndPoint as IPEndPoint;
	}

	public IPEndPoint GetRemoteEndPoint() {
		if (socket == null) {
			return default(IPEndPoint);
		}
		
		return socket.RemoteEndPoint as IPEndPoint;
	}

    public void SetServerPort(int port) {
		// For UDP
    }

	public bool IsConnected() {
		return	isConnected;
	}

	public void SubscribeNetworkState(NetworkStateHandler handler) {
		networkStateHandler += handler;
	}

	public void UnsubscribeNetworkState(NetworkStateHandler handler) {
		networkStateHandler -= handler;
	}

	/*
	* +-------------------------+
	* |        Connection       |
	* +-------------------------+
	*/

	public bool Connect(string address, int port) {
		if (socket != null) {
			return false;
		}

		try {
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.NoDelay = true;
			socket.Connect(address, port);

			isConnected = true;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			socket = null;
			isConnected = false;
		}

		sendQueue.Clear();
		recvQueue.Clear();

		if (networkStateHandler != null) {
			NetworkState state = new NetworkState();
			state.type = NetEventType.Connect;
			state.result = isConnected == true ? NetEventResult.Success : NetEventResult.Failure;
			networkStateHandler(this, state);
		}

		return isConnected;
	}

	public void Disconnect() {
		isConnected = false;

		if (socket != null) {
			try {
				socket.Shutdown(SocketShutdown.Both);
			} catch {
				// Do Nothing. Just server is already closed.
			}
			socket.Close();
			socket = null;
		}

		if (networkStateHandler != null) {
			NetworkState state = new NetworkState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			networkStateHandler(this, state);
		}
	}

	/*
	* +-------------------------+
	* |      Send / Receive     |
	* +-------------------------+
	*/

	public int Send(byte[] data, int size) {
		return sendQueue.Enqueue(data, size);
	}

	public int Receive(ref byte[] buffer, int size)  {
		return recvQueue.Dequeue(ref buffer, size);
	}

	/*
	* +-------------------------+
	* |         Dispatch        |
	* +-------------------------+
	*/

	public void Dispatch() {
		if (isConnected == true && socket != null) {
			DispatchSend();
			DispatchReceive();
		}
	}

	void DispatchSend() {
		if (socket == null) {
			return;
		}

		try {
			if (socket.Poll(0, SelectMode.SelectWrite)) {
				byte[] buffer = new byte[PACKET_SIZE];

				int sendSize = sendQueue.Dequeue(ref buffer, buffer.Length);
				while (sendSize > 0) {
					socket.Send(buffer, sendSize, SocketFlags.None);
					sendSize = sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		} catch {
			if (networkStateHandler != null) {
				NetworkState state = new NetworkState();
				state.type = NetEventType.SendError;
				state.result = NetEventResult.Failure;
				networkStateHandler(this, state);
			}
		}
	}

	void DispatchReceive() {
		if (socket == null) {
			return;
		}

		try {
			while (socket.Poll(0, SelectMode.SelectRead)) {
				byte[] buffer = new byte[PACKET_SIZE];

				int recvSize = socket.Receive(buffer, buffer.Length, SocketFlags.None);
				
				if (recvSize == 0) {
					Disconnect();
				}
				else if (recvSize > 0) {
					recvQueue.Enqueue(buffer, recvSize);
				}
			}
		} catch {
			if (networkStateHandler != null) {
				NetworkState state = new NetworkState();
				state.type = NetEventType.ReceiveError;
				state.result = NetEventResult.Failure;
				networkStateHandler(this, state);
			}
			return;
		}
	}
	
	public void SetReceiveData(byte[] data, int size) {	
		if (size > 0) {
			recvQueue.Enqueue(data, size);
		}
	}
}
