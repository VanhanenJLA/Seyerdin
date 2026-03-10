namespace Seyerdin.ServerModernized.Infrastructure;

public interface ILegacyMapStore
{
    byte[] LoadMapOrDefault(short mapId);

    void Save(short mapId, byte[] data);
}
