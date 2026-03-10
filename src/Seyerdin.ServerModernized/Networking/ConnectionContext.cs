using System.Net.Sockets;
using Seyerdin.ServerModernized.Domain;
using Seyerdin.ServerModernized.Protocol;

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

    public LegacySessionState State { get; set; } = LegacySessionState.NotConnected;

    public byte ClientVersion { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UniqueId { get; set; } = string.Empty;

    public string IniUniqueId { get; set; } = string.Empty;

    public LegacyAccountRecord? Account { get; set; }

    public bool BootstrapStarted { get; set; }
}
