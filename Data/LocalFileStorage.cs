namespace Kassette.Data;

/// <summary>Solo para desarrollo local. En Render el disco es efímero: los archivos se pierden al reiniciar.</summary>
public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    public string Name => "disco local (temporal)";

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "uploads", key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var f = File.Create(path);
        await content.CopyToAsync(f);
        return "/uploads/" + string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
    }
}
