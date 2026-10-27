using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
namespace Kassette.Data;

/// <summary>Cloudflare R2 (compatible con la API de S3). Devuelve la URL pública del archivo.</summary>
public class R2FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket, _publicBase;
    public string Name => "Cloudflare R2";

    public R2FileStorage(IConfiguration c)
    {
        _bucket = c["R2:Bucket"] ?? throw new InvalidOperationException("Falta R2:Bucket.");
        _publicBase = (c["R2:PublicBaseUrl"] ?? throw new InvalidOperationException("Falta R2:PublicBaseUrl.")).TrimEnd('/');
        var cfg = new AmazonS3Config
        {
            ServiceURL = $"https://{c["R2:AccountId"]}.r2.cloudflarestorage.com",
            AuthenticationRegion = "auto",
            // R2 no acepta las sumas de verificación nuevas del SDK: solo cuando son obligatorias
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };
        _s3 = new AmazonS3Client(new BasicAWSCredentials(c["R2:AccessKeyId"], c["R2:SecretAccessKey"]), cfg);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var req = new PutObjectRequest
        {
            BucketName = _bucket, Key = key, InputStream = content, ContentType = contentType,
            DisablePayloadSigning = true
        };
        req.Headers.CacheControl = "public, max-age=31536000";
        await _s3.PutObjectAsync(req);
        return _publicBase + "/" + string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
    }

    public async Task DeleteAsync(string url)
    {
        var prefix = _publicBase + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) return; // no es un archivo de este bucket
        var key = string.Join("/", url[prefix.Length..].Split('/').Select(Uri.UnescapeDataString));
        await _s3.DeleteObjectAsync(_bucket, key);
    }
}
