using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class SessionUDP : Session<TransportUDP> {

	private Dictionary<string, int> nodeAddress = new Dictionary<string, int>();

	public SessionUDP() {
		// UDP start index
		this.nodeIndex = 10000;
	}

	public override bool CreateListener(int port, int connectionMax) {
		try {
			listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			listener.Bind(new IPEndPoint(IPAddress.Any, port));
		} catch {
			return false;
		}
		
		return true;
	}
	
	public override bool DestroyListener() {
		if (listener == null) {
			return false;
		}

		listener.Close();
		listener = null;
		
		return true;
	}	
	
	public override void AcceptClient() {}

	protected override void DispatchReceive() {
		if (listener != null && listener.Poll(0, SelectMode.SelectRead)) {
			byte[] buffer = new byte[this.mtu];
			IPEndPoint address = new IPEndPoint(IPAddress.Any, 0);
			EndPoint endPoint = (EndPoint) address;
			
			int recvSize = listener.ReceiveFrom(buffer, SocketFlags.None, ref endPoint);

			IPEndPoint iep = (IPEndPoint) endPoint;
			string nodeAddr = iep.Address.ToString() + ":" + iep.Port;

			int node = -1;
			// To recognize sender that in the same device, extract IP address and port number from keepalive packet.
			string str = System.Text.Encoding.UTF8.GetString(buffer).Trim('\0');
			if (str.Contains(TransportUDP.CONNECTION_CHECK_REQUST_DATA)) {
				string[] strArray = str.Split(':');
				IPEndPoint ep = new IPEndPoint(IPAddress.Parse(strArray[0]), int.Parse(strArray[1]));

				// Link sender's address and node
				if (this.nodeAddress.ContainsKey(nodeAddr)) {
					node = this.nodeAddress[nodeAddr];
				} else {
					node = getNodeFromEndPoint(ep);
					if (node >= 0) {
						this.nodeAddress.Add(nodeAddr, node);
					}
				}
			} else {
				if (this.nodeAddress.ContainsKey(nodeAddr)) {
					node = this.nodeAddress[nodeAddr];
				}
			}

			if (node >= 0) {
				TransportUDP transport = this.transports[node];
				transport.SetReceiveData(buffer, recvSize, (IPEndPoint) endPoint);
			}
		}
	}

	private int getNodeFromEndPoint(IPEndPoint endPoint) {
		foreach (int node in this.transports.Keys) {
			TransportUDP transport = this.transports[node];

			IPEndPoint transportEp = transport.GetRemoteEndPoint();
			if (transportEp != null) {
				NetworkLogger.Log("[session udp] recv " + " end point: " + endPoint.ToString() + " node: " + node + " transport: " + transportEp.ToString());
				if (transportEp.Port == endPoint.Port && transportEp.Address.ToString() == endPoint.Address.ToString()) {
					return node;
				}
			}
		}
		
		return -1;
	}
}
