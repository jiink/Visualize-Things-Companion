using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Realivation_Companion
{
    class Comms
    {
        enum Cmd
        {
            Debug,
            QuestConfirmation,
            FileTx,
            Heartbeat
        }
        private string _questIpAddr = string.Empty;
        const int REALIVATION_PORT = 33134;
        private async Task SendFileAsync(string filePath, NetworkStream stream)
        {
            string fileName = Path.GetFileName(filePath);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] fileNameLenBytes = BitConverter.GetBytes((UInt32)fileNameBytes.Length);
            using var fileStream = File.OpenRead(filePath);
            long fileContentLength = fileStream.Length;
            byte[] fileContentLenBytes = BitConverter.GetBytes((UInt32)fileContentLength);
            int totalPayloadLength = fileNameLenBytes.Length + 
                fileNameBytes.Length + fileContentLenBytes.Length + (int)fileContentLength;
            byte[] totalPayloadLenBytes = BitConverter.GetBytes((UInt32)totalPayloadLength);
            await stream.WriteAsync(new byte[] { (byte)Cmd.FileTx });
            await stream.WriteAsync(totalPayloadLenBytes);
            await stream.WriteAsync(fileNameLenBytes);
            await stream.WriteAsync(fileNameBytes);
            await stream.WriteAsync(fileContentLenBytes);
            await fileStream.CopyToAsync(stream);
        }
        public async Task SendFileToQuest(string filePath)
        {
            if (string.IsNullOrWhiteSpace(_questIpAddr))
            {
                throw new Exception("No Quest is paired");
            }
            using TcpClient client = new(_questIpAddr, REALIVATION_PORT);
            using NetworkStream stream = client.GetStream();
            await SendFileAsync(filePath, stream);
        }
        public static (IPAddress? ipAddr, IPAddress? subnetMask) GetLocalIPAddress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    // ones that don't have a default gateway probably arent the one we want
                    if (ipProperties.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork &&
                                                             !g.Address.Equals(IPAddress.Any)))
                    {
                        foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(ip.Address))
                            {
                                return (ip.Address, ip.IPv4Mask);
                            }
                        }
                    }
                }
            }
            return (null, null);
        }
        private async Task HandleQuestConfirmationAsync(byte[] payload, IPAddress questIp)
        {

        }
        
        private async Task HandleClientAsync(TcpClient client)
        {
            IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint 
                ?? throw new Exception("Client endpoint is null");
            IPAddress clientIp = remoteEndPoint.Address;
            Console.WriteLine($"Handling client {client.Client.RemoteEndPoint}");
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] cmdIdBuf = new byte[1];
                    await stream.ReadExactlyAsync(cmdIdBuf);
                    Cmd cmd = (Cmd)cmdIdBuf[0];
                    Console.WriteLine($"Got command {cmd}!");
                    byte[] payloadLenBuf = new byte[4];
                    await stream.ReadExactlyAsync(payloadLenBuf);
                    int payloadLen = BitConverter.ToInt32(payloadLenBuf, 0);
                    byte[] payload = new byte[payloadLen];
                    await stream.ReadExactlyAsync(payload);
                    switch (cmd)
                    {
                        case Cmd.QuestConfirmation:
                            await HandleQuestConfirmationAsync(payload, clientIp);
                            break;
                        case Cmd.Heartbeat:
                            //await HandleHeartbeatAsync(payload, clientIp);
                            break;
                        default:
                            Console.WriteLine($"I can't handle the {cmd} cmd");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling client: {ex.Message}");
                }
            }
        }
        public async Task StartListening(int port)
        {
            (IPAddress? localIp, IPAddress? subnetMask) = GetLocalIPAddress();
            if (localIp == null ||  subnetMask == null)
            {
                throw new Exception("Couldn't get local IP address");
            }
            byte[] localIpBytes = localIp.GetAddressBytes();
            byte[] maskBytes = subnetMask.GetAddressBytes();
            byte[] localNetId = new byte[localIpBytes.Length];
            for (int i = 0; i < localIpBytes.Length; i++)
            {
                localNetId[i] = (byte)(localIpBytes[i] & maskBytes[i]);
            }
            TcpListener server = new(localIp, port);
            server.Start();
            Console.WriteLine($"Listening on {localIp}:{port}. Only accepting connections on subnet: {new IPAddress(localNetId)}");

            while (true)
            {
                TcpClient client = new();
                try
                {
                    client = await server.AcceptTcpClientAsync();
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint 
                        ?? throw new Exception("Couldn't get remote end point on new connection");
                    IPAddress clientIp = remoteIpEndPoint.Address;
                    // Subnet Check
                    // Network ID is IP AND Subnet Mask
                    byte[] clientIpBytes = clientIp.GetAddressBytes();
                    byte[] clientNetId = new byte[clientIpBytes.Length];
                    for (int i = 0; i < clientIpBytes.Length; i++)
                    {
                        clientNetId[i] = (byte)(clientIpBytes[i] & maskBytes[i]);
                    }
                    bool isInSubnet = localNetId.SequenceEqual(clientNetId);

                    if (isInSubnet)
                    {
                        Console.WriteLine($"Connection accepted from {clientIp}");
                        _ = HandleClientAsync(client); // not awaiting on purpose
                    }
                    else
                    {
                        Console.WriteLine($"Connection rejected from {clientIp}");
                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    client?.Close();
                }
            }
        }
    }
}
