using Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var root = configuration["Storage:Local:RootPath"] ?? "storage/uploads";
        _rootPath = Path.Combine(Directory.GetCurrentDirectory(), root);
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    public async Task<string> SaveAsync(string path, Stream stream, CancellationToken ct = default)
    {
        // Prevent path traversal
        var fileName = Path.GetFileName(path);
        var subDir = Path.GetDirectoryName(path) ?? "";

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, subDir, fileName));

        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted to access path outside of storage root.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.CopyToAsync(fileStream, ct);

        return path; // Return the relative path as key
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted to access path outside of storage root.");
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted to access path outside of storage root.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
