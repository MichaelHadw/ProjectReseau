class UdpReceiver
{
    private const int BufferSize = 1024;
    private static UdpClient udpClient;
    private static IPEndPoint remoteEndPoint;
    private static FileStream fileStream;
    
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: UdpReceiver <port> <output file>");
            return;
        }
        
        int port = int.Parse(args[0]);
        string outputFile = args[1];
        
        udpClient = new UdpClient(port);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        
        Console.WriteLine($"Listening on port {port}...");
        ReceivePackets();
    }
    
    private static void ReceivePackets()
    {
        try
        {
            while (true)
            {
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                if (receivedBytes.Length > 0)
                {
                    ProcessPacket(receivedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            fileStream.Close();
            udpClient.Close();
        }
    }
    
    private static void ProcessPacket(byte[] packet)
    {
        if (packet.Length < 4) return; // Minimum packet size
        
        int packetSize = BitConverter.ToUInt16(packet, 0);
        int sequenceNumber = BitConverter.ToUInt16(packet, 2);
        byte flags = packet[4];
        
        if ((flags & 0b00000001) != 0) // SYN flag
        {
            SendAck(sequenceNumber, true);
            Console.WriteLine("SYN received, sending SYN-ACK");
        }
        else if ((flags & 0b00000010) != 0) // ACK flag
        {
            Console.WriteLine("ACK received, connection established");
        }
        else if ((flags & 0b00000100) != 0) // FIN flag
        {
            SendAck(sequenceNumber, true);
            Console.WriteLine("FIN received, closing connection");
            return;
        }
        else // Data packet
        {
            byte[] data = new byte[packetSize - 5]; // Subtract header size
            Array.Copy(packet, 5, data, 0, data.Length);
            fileStream.Write(data, 0, data.Length);
            SendAck(sequenceNumber, false);
        }
    }
    
    private static void SendAck(int sequenceNumber, bool finAck)
    {
        byte[] ackPacket = new byte[6];
        BitConverter.GetBytes((ushort)6).CopyTo(ackPacket, 0); // Packet size
        BitConverter.GetBytes((ushort)sequenceNumber).CopyTo(ackPacket, 2); // Sequence number
        ackPacket[4] = finAck ? (byte)0b00000110 : (byte)0b00000010; // FIN+ACK or just ACK
        udpClient.Send(ackPacket, ackPacket.Length, remoteEndPoint);
    }
}

