using System;
using System.Net;
using System.Net.Sockets;

public class SessionTCP : Session<TransportTCP> {
	
    public override bool CreateListener(int port, int connectionMax) {
		try {
			this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.listener.Bind(new IPEndPoint(IPAddress.Any, port));
			this.listener.Listen(connectionMax);
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return true;
	}
	
	public override bool DestroyListener() {
		if (this.listener == null) {
			return false;
		}
		
		this.listener.Close();
		this.listener = null;

		return true;
	}	
	
	public override void AcceptClient()  {
		if ((this.listener != null) && this.listener.Poll(0, SelectMode.SelectRead)) {
			Socket socket = this.listener.Accept();

			int node = -1;
			try {
				TransportTCP transport = new TransportTCP();
				transport.Initialize(socket);
				transport.transportName = "serverSocket";
				node = JoinSession(transport);
			} catch (Exception e) {
				NetworkLogger.Log(e.ToString());
				return;
			}

			if (node >= 0 && this.networkStateHandler != null) {
				NetworkState state = new NetworkState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				this.networkStateHandler(node, state);
			}
		}
	}
}

