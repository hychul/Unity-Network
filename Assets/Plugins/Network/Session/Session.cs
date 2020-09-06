using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public abstract class Session<T> where T : ITransport, new() {

	protected const int DEFAULT_MTU_SIZE = 1400;

	protected Socket listener = null;

	protected int port = 0;

	protected int nodeIndex = 0;
	
	protected Dictionary<int, T> transports = null;

	protected bool threadLoop = false;
	
	protected Thread thread = null;

	protected System.Object transportLock = new System.Object();

	protected System.Object nodeIndexLock = new System.Object();
	
	protected bool isServer = false;

	protected int mtu;

	public delegate void NetworkStateHandler(int node, NetworkState state);
	protected NetworkStateHandler networkStateHandler;

	/*
	* +-------------------------+
	* |       Constructor       |
	* +-------------------------+
	*/
	
	public Session() {
		try {
			this.transports = new Dictionary<int, T>();
			this.mtu = DEFAULT_MTU_SIZE;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
		}
	}
	
	~Session() {
		Disconnect();
	}

	/*
	* +-------------------------+
	* |           Info          |
	* +-------------------------+
	*/

	public bool IsServer() {
		return this.isServer;
	}

	public bool IsConnected(int node) {
		if (this.transports.ContainsKey(node) == false) {
			return false;
		}

		return this.transports[node].IsConnected();
	}

	public int GetSessionNum() {
		return this.transports.Count;
	}

	public IPEndPoint GetLocalEndPoint(int node) {
		if (this.transports.ContainsKey(node) == false) {
			return default(IPEndPoint);
		}

		IPEndPoint ep;
		T transport = this.transports[node];
		ep = transport.GetLocalEndPoint();

		return ep;
	}
	
	public IPEndPoint GetRemoteEndPoint(int node) {
		if (this.transports.ContainsKey(node) == false) {
			return default(IPEndPoint);
		}

		IPEndPoint ep;
		T transport = this.transports[node];
		ep = transport.GetRemoteEndPoint();

		return ep;
	}

	int FindTransoprt(IPEndPoint sender) {
		foreach (int node in this.transports.Keys) {
			T transport = this.transports[node];
			IPEndPoint ep = transport.GetLocalEndPoint();
			if (ep.Address.ToString() == sender.Address.ToString()) {
				return node;
			}
		}
		
		return -1;
	}

	/*
	* +-------------------------+
	* |          Server         |
	* +-------------------------+
	*/
	
	public bool StartServer(int port, int connectionMax) {
		bool ret = CreateListener(port, connectionMax);
		if (ret == false) {
			return false;
		}

		if (this.threadLoop == false) {
			CreateThread();
		}

		this.port = port;
		this.isServer = true;
		
		return true;
	}
	
	public void StopServer() {
		this.isServer = false;

		DestroyThread();

		DestroyListener();
	}

	/*
	* +-------------------------+
	* |         Session         |
	* +-------------------------+
	*/

	protected int JoinSession(Socket socket) {
		T transport = new T();

		if (socket != null) {
			transport.Initialize(socket);
		}

		return JoinSession(transport);
	}

	protected int JoinSession(T transport) {
		int node = -1;
		lock (this.nodeIndexLock) {
			node = this.nodeIndex;
			++this.nodeIndex;
		}
		
		transport.SetNode(node);
		
		transport.SubscribeNetworkState(OnEventHandling);
		
		try {
			lock (this.transportLock) {
				this.transports.Add(node, transport);
			}
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return -1;
		}
		
		return node;
	}

	protected bool LeaveSession(int node) {
		if (node < 0) {
			return false;
		}
					
		T transport = (T) this.transports[node];
		if (transport == null) {
			return false;
		}

		lock (this.transportLock) {
			transport.Terminate();

			this.transports.Remove(node);
		}

		return true;
	}

	/*
	* +-------------------------+
	* |        Connection       |
	* +-------------------------+
	*/

	public virtual int Connect(string address, int port) {
		if (this.threadLoop == false) {
			CreateThread();
		}
	
		int node = -1;
		bool ret = false;
		try {
			T transport = new T();
			transport.SetServerPort(this.port);
			ret = transport.Connect(address, port);
			if (ret) {
				node = JoinSession(transport);
			}
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
		}

		if (this.networkStateHandler != null) {
			NetworkState state = new NetworkState();
			state.type = NetEventType.Connect;
			state.result = (ret)? NetEventResult.Success : NetEventResult.Failure;
			this.networkStateHandler(node, state);
		}

		return node;
	}

	public virtual bool Disconnect(int node) {
		if (node < 0) {
			return false;
		}

		if (this.transports == null) {
			return false;
		}

		if (this.transports.ContainsKey(node) == false) {
			return false;
		}

		T transport = this.transports[node];
		if (transport != null) {
			transport.Disconnect();
			LeaveSession(node);
		}

		if (this.networkStateHandler != null) {
			NetworkState state = new NetworkState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			this.networkStateHandler(node, state);
		}

		return true;
	}

	public virtual bool Disconnect() {
		DestroyThread();
		
		lock (this.transportLock) {
			Dictionary<int, T> transports = new Dictionary<int, T>(this.transports);

			foreach (T trans in transports.Values) {
				trans.Disconnect();
				trans.Terminate();
			}
		}

		return true;
	}

	/*
	* +-------------------------+
	* |      Send / Receive     |
	* +-------------------------+
	*/

	public virtual int Send(int node, byte[] data, int size) {
		if (node < 0) {
			return -1;
		}

		int sendSize = 0;
		try {
			T transport = (T) this.transports[node];
			sendSize = transport.Send(data, size);
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return -1;
		}

		return sendSize;
	}
	
	public virtual int Receive(int node, ref byte[] buffer) {
		if (node < 0) {
			return -1;
		}

		int recvSize = 0;
		try { 
			T transport = this.transports[node];
			recvSize = transport.Receive(ref buffer, buffer.Length);
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return -1;
		}

		return recvSize;
	}

	/*
	* +-------------------------+
	* |          Thread         |
	* +-------------------------+
	*/

	protected bool CreateThread() {
		try {
			this.thread = new Thread(new ThreadStart(ThreadDispatch));
			this.threadLoop = true;
			this.thread.Start();
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return true;
	}

	protected bool DestroyThread() {
		if (this.threadLoop == true) {
			this.threadLoop = false;

			if (this.thread != null) {
				this.thread.Join();
				this.thread = null;
			}
		}

		return true;
	}

	/*
	* +-------------------------+
	* |         Dispatch        |
	* +-------------------------+
	*/

	public virtual void ThreadDispatch() {	
		while (this.threadLoop) {
			AcceptClient();
			
			Dispatch();
			
			Thread.Sleep(3);	
		}
	}

	public virtual void Dispatch() {
		Dictionary<int, T> transports = new Dictionary<int, T>(this.transports);
		
		foreach (T transport in transports.Values) {
			if (transport != null) {
				transport.Dispatch();
			}
		}

		DispatchReceive();
	}

	protected virtual void DispatchReceive() {
		// For UDP dispatch receive
	}

	/*
	* +-------------------------+
	* |           Event         |
	* +-------------------------+
	*/

	public void SubscribeNetworkState(NetworkStateHandler handler) {
		this.networkStateHandler += handler;
	}
	
	public void UnsubscribeNetworkState(NetworkStateHandler handler) {
		this.networkStateHandler -= handler;
	}

	public virtual void OnEventHandling(ITransport transport, NetworkState state) {
		int node = transport.GetNode();

		do {
			if (this.transports.ContainsKey(node) == false) {
				break;
			}

			switch (state.type) {
				case NetEventType.Connect:
					break;
				case NetEventType.Disconnect:
					LeaveSession(node);
					break;
			}
		} while (false);

		if (this.networkStateHandler != null) {
			this.networkStateHandler(node, state);
		}
	}

	public abstract bool CreateListener(int port, int connectionMax);
	
	public abstract bool DestroyListener();
	
	public abstract void AcceptClient();
}
