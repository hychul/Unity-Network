using System.Net;
using System.Net.Sockets;

public delegate void NetworkStateHandler(ITransport transport, NetworkState state);

public interface ITransport {

	void Initialize(Socket socket);

	void Terminate();

	int GetNode();

	void SetNode(int node);

	IPEndPoint GetLocalEndPoint();

	IPEndPoint GetRemoteEndPoint();

	int Send(byte[] data, int size);
	
	int Receive(ref byte[] buffer, int size);

	bool Connect(string address, int port);
	
	void Disconnect();
	
	void Dispatch();

	bool IsConnected();

	void SubscribeNetworkState(NetworkStateHandler handler);

	void UnsubscribeNetworkState(NetworkStateHandler handler);

	void SetServerPort(int port);
}
