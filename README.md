# Unity-Network

![unity_version](https://img.shields.io/badge/Unity-2020.1-blue.svg?style=flat-square) ![license](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)

Network module for Unity (or c# project with small fix).

## Getting Start

Create `Network` instance in your script.

### TCP

Start server on your device or connect to other tcp server.

### UDP

Because UDP doesn't have 'connection' with others, to communicate with others, you have to start server and call `Connect(string, int)` method.

### Send

To send your data to another, you have to know witch node is connected. By subscribing node state change with  `NodeStateHandler`, you can get connected node.

### Receiving

In receving function, there is a method that doesn't need to pass node to receving from node : `Receive()`.

I recommand to use this method to receive packet from others and receving method include this must called periodically.

It is recommended to use Unity's `Update()` method to call receiving method periodically.

... document being updated