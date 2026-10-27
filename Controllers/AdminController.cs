using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kassette.Data;
using Kassette.Models;
using Kassette.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Kassette.Controllers;

[Authorize(AuthenticationSchemes = "Admin")]
public class AdminController(IServiceProvider sp, ISongRepository repo, IFileStorage storage,
    IConfiguration cfg, PlayerRegistry players, ILogger<AdminController> log) : Controller
{
    private const long MaxAudio = 25_000_000, MaxCover = 3_000_000;
    private ISongCatalog? Catalog => sp.GetService<ISongCatalog>();

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => View();

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? password)
    {
        var expected = cfg["Admin:Password"];
        if (string.IsNullOrEmpty(expected))
        { ViewBag.Error = "El panel está desactivado: falta configurar Admin__Password."; return View(); }
        // Comparación en tiempo constante, sobre hashes del mismo largo
        var ok = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(password ?? "")), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        if (!ok) { ViewBag.Error = "Contraseña incorrecta."; return View(); }
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Admin"));
        await HttpContext.SignInAsync("Admin", user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Admin");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.CatalogReady = Catalog != null;
        ViewBag.Storage = storage.Name;
        return View(repo.Library.Songs());
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(40_000_000), RequestFormLimits(MultipartBodyLengthLimit = 40_000_000)]
    public async Task<IActionResult> Add(string? title, string? artist, string? album, int track, int duration,
        IFormFile? audio, IFormFile? cover, bool enqueue)
    {
        var catalog = Catalog;
        if (catalog == null) return Fail("MongoDB no está configurado.");
        title = (title ?? "").Trim(); artist = (artist ?? "").Trim(); album = (album ?? "").Trim();
        if (title.Length is 0 or > 100 || artist.Length is 0 or > 100 || album.Length is 0 or > 100)
            return Fail("Título, artista y álbum son obligatorios (máximo 100 caracteres).");
        if (audio == null || audio.Length == 0 || audio.Length > MaxAudio)
            return Fail("Elige un archivo MP3 de hasta 25 MB.");
        if (!audio.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || !LooksLikeMp3(await Head(audio, 3)))
            return Fail("El archivo de audio no es un MP3 válido.");
        string? coverExt = null;
        if (cover is { Length: > 0 })
        {
            if (cover.Length > MaxCover) return Fail("La portada pesa más de 3 MB.");
            coverExt = CoverExt(await Head(cover, 4));
            if (coverExt == null) return Fail("La portada debe ser JPG o PNG.");
        }
        track = Math.Clamp(track, 0, 999); duration = Math.Clamp(duration, 0, 3 * 3600);

        try
        {
            var id = Guid.NewGuid().ToString("N")[..8];
            string audioUrl, coverUrl = null!;
            await using (var s = audio.OpenReadStream())
                audioUrl = await storage.UploadAsync($"music/{Slug(artist)}/{Slug(album)}/{track:00}-{Slug(title)}-{id}.mp3", s, "audio/mpeg");
            if (coverExt != null)
                await using (var s = cover!.OpenReadStream())
                    coverUrl = await storage.UploadAsync($"covers/{Slug(artist)}/{Slug(album)}-{id}{coverExt}", s,
                        coverExt == ".png" ? "image/png" : "image/jpeg");
            var song = await catalog.AddAsync(new NewSong(title, artist, album, track, duration, audioUrl, coverUrl));
            if (enqueue) players.For(HttpContext).Enqueue(song.Id); // entra al final de "A continuación"
            TempData["Ok"] = enqueue ? $"«{song.Title}» agregada a la biblioteca y a la cola." : $"«{song.Title}» agregada a la biblioteca.";
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al subir canción");
            return Fail("No se pudo guardar la canción. Revisa la configuración de MongoDB y R2.");
        }
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Fail(string msg) { TempData["Error"] = msg; return RedirectToAction(nameof(Index)); }

    private static async Task<byte[]> Head(IFormFile f, int n)
    {
        await using var s = f.OpenReadStream();
        var b = new byte[n];
        var read = await s.ReadAtLeastAsync(b, n, throwOnEndOfStream: false);
        return b[..read];
    }
    // Firma de MP3: etiqueta "ID3" o cabecera de trama (11 bits en 1)
    private static bool LooksLikeMp3(byte[] h) =>
        h.Length >= 3 && ((h[0] == 'I' && h[1] == 'D' && h[2] == '3') || (h[0] == 0xFF && (h[1] & 0xE0) == 0xE0));
    private static string? CoverExt(byte[] h) =>
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF ? ".jpg" :
        h.Length >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 ? ".png" : null;

    // Nombre seguro para rutas: sin tildes, símbolos ni espacios
    private static string Slug(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c < 128 && char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }
        var r = Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return r.Length == 0 ? "x" : r.Length > 40 ? r[..40] : r;
    }
}
