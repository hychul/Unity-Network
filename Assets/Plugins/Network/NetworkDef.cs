public class NetworkState {
	public int node;
	public NetEventType type;
	public NetEventResult result;
}

public enum NetEventType {
	Connect = 0,
	Disconnect,
	SendError,
	ReceiveError,
}

public enum NetEventResult {
	Success = 0,
	Failure = -1,
}
