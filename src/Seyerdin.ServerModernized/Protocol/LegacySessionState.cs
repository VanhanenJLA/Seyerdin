namespace Seyerdin.ServerModernized.Protocol;

public enum LegacySessionState : byte
{
    NotConnected = 0,
    Connected = 1,
    Playing = 2,
}
