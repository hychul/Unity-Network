using System.Diagnostics;
using UnityEngine;

public static class NetworkLogger {
    
    [Conditional("NETWORK_LOG")]
    public static void Log(string message) {
#if NETWORK_LOG
            UnityEngine.Debug.Log(message);
#endif
    }
}
