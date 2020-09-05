using System;
using System.Net;
using System.Net.Sockets;

public class TransportUDP : ITransport {

	private const int PACKET_SIZE = 1400;

	// Dummy packet data to check connection
	public const string CONNECTION_CHECK_REQUST_DATA = "KeepAlive.";

	private const int KEEP_ALIVE_INTER = 1; 
	private const int TIME_OUT_S = 10;

	private int node = -1;

	private Socket socket = null;

	private IPEndPoint localEndPoint;
	private IPEndPoint remoteEndPoint;

	private PacketQueue sendQueue = new PacketQueue();
	private PacketQueue	recvQueue = new PacketQueue();

	private bool isConnectionRequested = false;
	private	bool isConnected = false;

	private DateTime timeOutTicker;

	private DateTime keepAliveTicker;
	// To send 'keepalive' at the first connection
	private bool isFirst = false;

	private NetworkStateHandler networkStateHandler;

	// To identificate when running in the same terminal
	private int serverPort = -1;

	/*
	* +-------------------------+
	* |          ?????          |
	* +-------------------------+
	*/

	public TransportUDP() { }

	public TransportUDP(Socket socket) {
		this.socket = socket;
	}

	public void Initialize(Socket socket) {
		this.socket = socket;
		this.isConnectionRequested = true;
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
        return localEndPoint;
    }

	public IPEndPoint GetRemoteEndPoint() {
		return remoteEndPoint;
	}

    public void SetServerPort(int port) {
        this.serverPort = port;
    }

	public bool IsRequested() {
		return isConnectionRequested;
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
		if (socket == null) {
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		try {
			string hostname = Dns.GetHostName();

			IPAddress[]	hostAddresses = Dns.GetHostAddresses(hostname);
			foreach (IPAddress hostAddress in hostAddresses) {
				if (hostAddress.AddressFamily == AddressFamily.InterNetwork) {
					localEndPoint = new IPEndPoint(hostAddress, serverPort);
					break;
				}
			}

			// TODO: find better way
			string ipAddress = "localhost".Equals(address) ? "127.0.0.1" : address;

			remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
			NetworkLogger.Log("[transport udp] local address: " + localEndPoint.Address.ToString() + " port: " + localEndPoint.Port);
			NetworkLogger.Log("[transport udp] remote address: " + remoteEndPoint.Address.ToString() + " port: " + remoteEndPoint.Port);
			isConnectionRequested = true;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			isConnectionRequested = false;
		}

		if (networkStateHandler != null) {
			NetworkState state = new NetworkState();
			state.type = NetEventType.Connect;
			state.result = isConnectionRequested ? NetEventResult.Success : NetEventResult.Failure;
			networkStateHandler(this, state);
		}

		sendQueue.Clear();
		recvQueue.Clear();

		keepAliveTicker = DateTime.Now;
		isFirst = true;

		return isConnectionRequested;
	}

	public void Disconnect()  {
		isConnectionRequested = false;
		isConnected = false;

		if (socket != null) {
			socket.Shutdown(SocketShutdown.Both);
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

	public int Receive(ref byte[] buffer, int size) {
		return recvQueue.Dequeue(ref buffer, size);
	}

	/*
	* +-------------------------+
	* |         Dispatch        |
	* +-------------------------+
	*/

	public void Dispatch() {
		if (socket == null) {
			return;
		}

		DispatchSend();
		CheckTimeout();

		// keepalive
		TimeSpan ts = DateTime.Now - keepAliveTicker;

		if (ts.Seconds > KEEP_ALIVE_INTER || isFirst) {
			string message = localEndPoint.Address.ToString() + ":" + serverPort + ":" + CONNECTION_CHECK_REQUST_DATA;
			byte[] request = System.Text.Encoding.UTF8.GetBytes(message);
			socket.SendTo(request, request.Length, SocketFlags.None, remoteEndPoint);	
			keepAliveTicker = DateTime.Now;
			isFirst = false;
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
					socket.SendTo(buffer, sendSize, SocketFlags.None, remoteEndPoint);	
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
	
	public void SetReceiveData(byte[] data, int size, IPEndPoint endPoint) {
		string str = System.Text.Encoding.UTF8.GetString(data).Trim('\0');
		if (str.Contains(CONNECTION_CHECK_REQUST_DATA)) {
			if (isConnected == false && networkStateHandler != null) {
				NetworkState state = new NetworkState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				networkStateHandler(this, state);
			}

			isConnected = true;
			timeOutTicker = DateTime.Now;
		} else if (size > 0) {
			recvQueue.Enqueue(data, size);
		}
	}

	void CheckTimeout() {
		TimeSpan ts = DateTime.Now - timeOutTicker;

		if (isConnectionRequested && isConnected && TIME_OUT_S < ts.Seconds) {
			Disconnect();
		}
	}
}
