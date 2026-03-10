using System.Net.Sockets;

namespace Seyerdin.ServerModernized.Networking;

public sealed class ConnectionContext
{
    public ConnectionContext(TcpClient client)
    {
        Client = client;
        Buffer = new List<byte>(4096);
    }

    public TcpClient Client { get; }

    public List<byte> Buffer { get; }

    public byte PacketsRead { get; set; }
}
