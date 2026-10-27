namespace Kassette.Data;

/// <summary>Solo para desarrollo local. En Render el disco es efímero: los archivos se pierden al reiniciar.</summary>
public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    public string Name => "disco local (temporal)";
    private string Root => Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var path = Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var f = File.Create(path);
        await content.CopyToAsync(f);
        return "/uploads/" + string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
    }

    public Task DeleteAsync(string url)
    {
        if (!url.StartsWith("/uploads/", StringComparison.Ordinal)) return Task.CompletedTask;
        var rel = string.Join(Path.DirectorySeparatorChar, url["/uploads/".Length..].Split('/').Select(Uri.UnescapeDataString));
        var full = Path.GetFullPath(Path.Combine(Root, rel));
        if (full.StartsWith(Path.GetFullPath(Root), StringComparison.Ordinal) && File.Exists(full)) File.Delete(full); // sin salir de /uploads
        return Task.CompletedTask;
    }
}
