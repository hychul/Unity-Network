using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class TcpNetworkSystem : MonoBehaviour {

    private Network network = new Network();

    private int node = -1;

    [SerializeField]
    private InputField serverPortInputField;

    [SerializeField]
    private InputField connectAddressInputField;
    [SerializeField]
    private InputField connectPortInputField;

    [SerializeField]
    private InputField sendTextInputField;

    [SerializeField]
    private Text receivedText;

    void Start() {
        network.SubscribePacketReceived(1, OnPacketHAndled);
        network.SetNetworkStateHandler(OnNetwork);
    }

    void Update() {
        network.Receive();
    }

    public void OnStartServerButton() {
        network.StartServer(int.Parse(serverPortInputField.text), 10, Network.ConnectionType.TCP);
    }

    public void OnConnectionButton() {
        this.node = network.Connect(connectAddressInputField.text, int.Parse(connectPortInputField.text), Network.ConnectionType.TCP);
    }

    public void OnSendButton() {
        string text = sendTextInputField.text;
        network.SendTcp(this.node, new TalkPacket(text));
    }

    void OnPacketHAndled(int node, int packetType, byte[] data) {
        this.node = node;
        var packet = new TalkPacket(data);
        receivedText.text = packet.GetTalk().text;
    }

    void OnNetwork(Network.ConnectionType connectionType, NetworkState networkState) {
            this.node = networkState.node;
    }
}
