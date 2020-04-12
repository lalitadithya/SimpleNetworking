# Getting Started
Using SimpleNetworking is extremely easy, but, first you have to choose the nature of connection between the two devices:

1. TCP connection
2. TLS connection
3. Mutual TLS connection

## TCP Connection
Using TCP is the most basic way to connect two devices. TCP does not encrypt the data that is sent over the network. You can find a working example of the code [here](https://github.com/lalitadithya/SimpleNetworking/tree/master/samples/BasicSample)

### Server
A TCP server is a simple listening socket that is listening for incomming connections

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a server
Use the static builder class to build a simple TCP server as shown below:
```cs
InsecureServer server = SimpleNetworking.Builder.Builder
                        .InsecureServer
                        .Build();
```

#### Step 3: Register for the client connected event
Register for the client connected event. When the client connected event is fired, you will get a client object which can be used to send or receive data. How to send and recieve data using a client is described in the client section. A simple client connected handler is shown below:
```cs
private static void Server_OnClientConnected(Client client1)
{
    Console.WriteLine("Client connected");
    client = client1;
}
```
Register for the event as shown below:
```cs
server.OnClientConnected += Server_OnClientConnected;
```

#### Step 4: Start Listening 
Start the sever by calling the Start Listening method as shown below. The start listenening method takes as input the IP address to bind to as well as the port. 
```cs
server.StartListening(IPAddress.Any, 9000);
```

### Client
A TCP client is used to connect to a TCP server. 

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a client
Use the static builder class to build a simple TCP client as shown below:
```cs
Client client = SimpleNetworking.Builder.Builder
                .InsecureClient
                .Build();
```

#### Step 3: Register for packet received, peer device disconnected, peer device reconnected events
The packet received event is fired when the other device sends a packet. A simple packet received event handler is shown below:
```cs
private static void Client_OnPacketReceived(object data)
{
    MyPacket packet = (MyPacket)data;
    Console.WriteLine("Got: " + packet.Data);
}
```
The peer device disconncted event is fired when the connectivity to the other device is lost and both the devices are trying to restablish connectivity. A simple peer device disconnected event handler is shown below:
```cs
private static void Client_OnPeerDeviceDisconnected()
{
    Console.WriteLine("Peer device disconnected");
}
```
The peer device reconnected event is fired when the connectivity to the other device has been restablished. This event will only be fired after peer device disconnected event is fired. A simple peer device reconnected handler is shown below
```cs
private static void Client_OnPeerDeviceReconnected()
{
    Console.WriteLine("Peer device reconnected");
}
```
Register for the events as shown below:
```cs
client.OnPacketReceived += Client_OnPacketReceived;
client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
```

#### Step 4: Connect to the server
Connect to the server by calling the Connect method. The connect method takes as input the IP address as well as the port. 
```cs
await client.Connect("127.0.0.1", 9000);
```

#### Step 5: Send some data
After the connection is successful, you can send data using the Send Data method as shown below:
```cs
await client.SendData(new MyPacket
{
    Data = $"{i++}"
});
```

## TLS connection

Using TLS is more secure when compared to TCP. In this method, the client will verify that it is connecting to the correct server and the data that is transferred between the client and the server is encrypted. You can find a working example of the code [here](https://github.com/lalitadithya/SimpleNetworking/tree/master/samples/SecureSample)

### Server
A TLS server is a simple listening socket that is listening for incomming connections. You will need a PFX certificate in order to start a TLS server

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a server
Use the static builder class to build a simple TLS server as shown below:
```cs
X509Certificate2Collection collection = new X509Certificate2Collection();
collection.Import("certificate.pfx", "password", X509KeyStorageFlags.PersistKeySet);

SecureServer server = SimpleNetworking.Builder.Builder.SecureServer
    .Build(collection[0]);
```

#### Step 3: Register for the client connected event
Register for the client connected event. When the client connected event is fired, you will get a client object which can be used to send or receive data. How to send and recieve data using a client is described in the client section. A simple client connected handler is shown below:
```cs
private static void Server_OnClientConnected(Client client1)
{
    Console.WriteLine("Client connected");
    client = client1;
}
```
Register for the event as shown below:
```cs
server.OnClientConnected += Server_OnClientConnected;
```

#### Step 4: Start Listening 
Start the sever by calling the Start Listening method as shown below. The start listenening method takes as input the IP address to bind to as well as the port. 
```cs
server.StartListening(IPAddress.Any, 9000);
```

### Client
A TLS client is used to connect to a TLS server. A TLS client needs to verify that it is connected to the correct server by validating the server's certificate.

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a client
Write a server certificate valiation callback as shown below:
```cs
private static bool ServerCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
{
    return true;
}
```
Use the static builder class to build a simple TLS client as shown below:
```cs
Client client = SimpleNetworking.Builder.Builder.SecureClient
                        .Build(ServerCertificateValidationCallback);
```

#### Step 3: Register for packet received, peer device disconnected, peer device reconnected events
The packet received event is fired when the other device sends a packet. A simple packet received event handler is shown below:
```cs
private static void Client_OnPacketReceived(object data)
{
    MyPacket packet = (MyPacket)data;
    Console.WriteLine("Got: " + packet.Data);
}
```
The peer device disconncted event is fired when the connectivity to the other device is lost and both the devices are trying to restablish connectivity. A simple peer device disconnected event handler is shown below:
```cs
private static void Client_OnPeerDeviceDisconnected()
{
    Console.WriteLine("Peer device disconnected");
}
```
The peer device reconnected event is fired when the connectivity to the other device has been restablished. This event will only be fired after peer device disconnected event is fired. A simple peer device reconnected handler is shown below
```cs
private static void Client_OnPeerDeviceReconnected()
{
    Console.WriteLine("Peer device reconnected");
}
```
Register for the events as shown below:
```cs
client.OnPacketReceived += Client_OnPacketReceived;
client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
```

#### Step 4: Connect to the server
Connect to the server by calling the Connect method. The connect method takes as input the IP address as well as the port. 
```cs
await client.Connect("127.0.0.1", 9000);
```

#### Step 5: Send some data
After the connection is successful, you can send data using the Send Data method as shown below:
```cs
await client.SendData(new MyPacket
{
    Data = $"{i++}"
});
```

## Mutual TLS connection

Using mutual TLS is the most secure way to send data between two devices. In this method, the client will verify that it is connecting to the correct server and the server will also verify that the correct client has connected. Additionally, the data that is transferred between the client and the server is encrypted. You can find a working example of the code [here](https://github.com/lalitadithya/SimpleNetworking/tree/master/samples/SuperSecureSample)

### Server
A TLS server is a simple listening socket that is listening for incomming connections. You will need a PFX certificate in order to start a TLS server. The TLS server will need to verify that the correct client has connected to it.

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a server
Write a client certificate validation callback as shown below:
```cs
private static bool ClientCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
{
    return true;
}
```
Use the static builder class to build a simple TLS server as shown below:
```cs
X509Certificate2Collection serverCollection = new X509Certificate2Collection();
serverCollection.Import("ServerCertificate.pfx", "password", X509KeyStorageFlags.PersistKeySet);

SecureServer server = SimpleNetworking.Builder.Builder.SecureServer
    .Build(serverCollection[0], true, ClientCertificateValidationCallback);
```

#### Step 3: Register for the client connected event
Register for the client connected event. When the client connected event is fired, you will get a client object which can be used to send or receive data. How to send and recieve data using a client is described in the client section. A simple client connected handler is shown below:
```cs
private static void Server_OnClientConnected(Client client1)
{
    Console.WriteLine("Client connected");
    client = client1;
}
```
Register for the event as shown below:
```cs
server.OnClientConnected += Server_OnClientConnected;
```

#### Step 4: Start Listening 
Start the sever by calling the Start Listening method as shown below. The start listenening method takes as input the IP address to bind to as well as the port. 
```cs
server.StartListening(IPAddress.Any, 9000);
```

### Client
A TLS client is used to connect to a TLS server. You will need a PFX certificate to build a TLS client. The TLS client needs to verify that it is connected to the correct server by validating the server's certificate.

#### Step 1: Installing SimpleNetworking
Run the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```
Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:
```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

#### Step 2: Build a client
Write a server certificate valiation callback as shown below:
```cs
private static bool ServerCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
{
    return true;
}
```
Use the static builder class to build a simple TLS client as shown below:
```cs
X509Certificate2Collection clientCollection = new X509Certificate2Collection();
clientCollection.Import("ClientCertificate.pfx", "password", X509KeyStorageFlags.PersistKeySet);

client = SimpleNetworking.Builder.Builder.SecureClient
    .Build(ServerCertificateValidationCallback, clientCollection);
```

#### Step 3: Register for packet received, peer device disconnected, peer device reconnected events
The packet received event is fired when the other device sends a packet. A simple packet received event handler is shown below:
```cs
private static void Client_OnPacketReceived(object data)
{
    MyPacket packet = (MyPacket)data;
    Console.WriteLine("Got: " + packet.Data);
}
```
The peer device disconncted event is fired when the connectivity to the other device is lost and both the devices are trying to restablish connectivity. A simple peer device disconnected event handler is shown below:
```cs
private static void Client_OnPeerDeviceDisconnected()
{
    Console.WriteLine("Peer device disconnected");
}
```
The peer device reconnected event is fired when the connectivity to the other device has been restablished. This event will only be fired after peer device disconnected event is fired. A simple peer device reconnected handler is shown below
```cs
private static void Client_OnPeerDeviceReconnected()
{
    Console.WriteLine("Peer device reconnected");
}
```
Register for the events as shown below:
```cs
client.OnPacketReceived += Client_OnPacketReceived;
client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
```

#### Step 4: Connect to the server
Connect to the server by calling the Connect method. The connect method takes as input the IP address as well as the port. 
```cs
await client.Connect("127.0.0.1", 9000);
```

#### Step 5: Send some data
After the connection is successful, you can send data using the Send Data method as shown below:
```cs
await client.SendData(new MyPacket
{
    Data = $"{i++}"
});
```