namespace Application.Services.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(string path, Stream stream, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
