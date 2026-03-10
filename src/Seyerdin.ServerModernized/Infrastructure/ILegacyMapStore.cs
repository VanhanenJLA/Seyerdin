namespace Seyerdin.ServerModernized.Infrastructure;

public interface ILegacyMapStore
{
    byte[] LoadMapOrDefault(short mapId);
}
