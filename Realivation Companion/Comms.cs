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
using Serilog;

namespace Realivation_Companion;

class Comms
{
    enum Cmd
    {
        Debug,
        QuestConfirmation,
        FileTx,
        Heartbeat
    }
    
    private readonly int _port;
    private IPAddress _questIpAddr = IPAddress.None;
    private readonly TimeSpan WATCHDOG_RESET = TimeSpan.FromSeconds(6);
    private readonly Watchdog _watchdog;
    private CancellationTokenSource? _questConnectionCts;
    public Comms(int port)
    {
        _port = port;
        _watchdog = new(WATCHDOG_RESET, () => {
            Log.Warning("Watchdog timer alarm, Quest {ip} unpaired", _questIpAddr);
            UnpairQuest();
            }
        );
    }
    private void UnpairQuest()
    {
        if (_questIpAddr == IPAddress.None)
        {
            return;
        }
        Log.Information("Unpairing from {ip}", _questIpAddr);
        _questIpAddr = IPAddress.None;
        _watchdog.Stop();
        _questConnectionCts?.Cancel();
        _questConnectionCts?.Dispose();
        _questConnectionCts = null;
    }
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
        if (_questIpAddr == IPAddress.None)
        {
            throw new Exception("No Quest is paired");
        }
        using TcpClient client = new(new IPEndPoint(_questIpAddr, _port));
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
        Version questVersion = new(payload[0], payload[1]);
        Log.Information("Quest connected with version {v}", questVersion);
        _questIpAddr = questIp;
        _watchdog.Reset();
        _watchdog.Start();
    }
    private async Task HandleHeartbeatAsync()
    {
        Log.Information("(heartbeat)");
        _watchdog.Reset();
        _watchdog.Start();
    }
    // this function will only exit when the connection is terminated
    private async Task HandleClientAsync(TcpClient client)
    {
        IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint 
            ?? throw new Exception("Client endpoint is null");
        IPAddress clientIp = remoteEndPoint.Address;
        Log.Information($"Handling client {client.Client.RemoteEndPoint}");
        if (_questIpAddr != IPAddress.None)
        {
            Log.Warning("{cip} rejected 'cause I am already paired with {qip}", clientIp, _questIpAddr);
        }
        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] cmdIdBuf = new byte[1];
                await stream.ReadExactlyAsync(cmdIdBuf);
                Cmd firstCmd = (Cmd)cmdIdBuf[0];
                Log.Information("Got {cmd} as first command", firstCmd);
                if (firstCmd != Cmd.QuestConfirmation)
                {
                    throw new Exception($"{clientIp}, you must send a {Cmd.QuestConfirmation} command first");
                }
                byte[] payloadLenBuf = new byte[4];
                await stream.ReadExactlyAsync(payloadLenBuf);
                int payloadLen = BitConverter.ToInt32(payloadLenBuf, 0);
                byte[] payload = new byte[payloadLen];
                await stream.ReadExactlyAsync(payload);
                UnpairQuest();
                _questConnectionCts = new();
                var cancelTok = _questConnectionCts.Token;
                await HandleQuestConfirmationAsync(payload, clientIp);
                while (client.Connected && !cancelTok.IsCancellationRequested)
                {
                    await stream.ReadExactlyAsync(cmdIdBuf, cancelTok);
                    Cmd cmd = (Cmd)cmdIdBuf[0];
                    await stream.ReadExactlyAsync(payloadLenBuf, cancelTok);
                    payloadLen = BitConverter.ToInt32(payloadLenBuf, 0);
                    payload = new byte[payloadLen];
                    await stream.ReadExactlyAsync(payload, cancelTok);
                    switch (cmd)
                    {
                        case Cmd.QuestConfirmation:
                            Log.Warning("awkward....");
                            break;
                        case Cmd.Heartbeat:
                            await HandleHeartbeatAsync();
                            break;
                        default:
                            Log.Error($"I can't handle the {cmd} cmd");
                            break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("I closed connection to {ip} on purpose.", clientIp);
        }
        catch (EndOfStreamException)
        {
            Log.Information("{ip} disconnected.", clientIp);
        }
        catch (Exception ex)
        {
            Log.Error($"Error handling client: {ex.Message}");
        }
        finally
        {
            if (clientIp == _questIpAddr)
            {
                UnpairQuest();
            }
        }
    }
    public async Task StartListening()
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
        TcpListener server = new(localIp, _port);
        server.Start();
        Log.Information("Listening on {localIp}:{_port}. " +
            "Only accepting connections on subnet: {id}",
            localIp, _port, new IPAddress(localNetId));

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
                    Log.Information($"Connection accepted from {clientIp}");
                    _ = HandleClientAsync(client); // not awaiting on purpose
                }
                else
                {
                    Log.Warning($"Connection rejected from {clientIp}");
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                client?.Close();
            }
        }
    }
}
