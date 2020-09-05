using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UdpNetworkSystem : MonoBehaviour {

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
        network.ReceiveUdp(node);
    }

    public void OnStartServerButton() {
        network.StartServer(int.Parse(serverPortInputField.text), 10, Network.ConnectionType.UDP);
    }

    public void OnConnectionButton() {
        this.node = network.Connect(connectAddressInputField.text, int.Parse(connectPortInputField.text), Network.ConnectionType.UDP);
    }

    public void OnSendButton() {
        string text = sendTextInputField.text;
        network.SendUdp(this.node, new TalkPacket(text));
    }

    void OnPacketHAndled(int node, int packetType, byte[] data) {
        this.node = node;
        var packet = new TalkPacket(data);
        receivedText.text = packet.GetTalk().text;
    }

    void OnNetwork(Network.ConnectionType connectionType, int node, NetworkState networkState) {
            this.node = node;
    }
}
