namespace Kassette.Data;

using Kassette.Models;

/// <summary>Datos de una canción nueva (los archivos ya están subidos y solo llegan sus URLs).</summary>
public record NewSong(string Title, string Artist, string Album, int Track, int Duration, string AudioUrl, string? CoverUrl);

/// <summary>Catálogo que además permite agregar canciones.</summary>
public interface ISongCatalog : ISongRepository
{
    Task<Song> AddAsync(NewSong song);
}

/// <summary>Dónde se guardan los archivos (MP3 y portadas). Cambiar de R2 a otro servicio solo toca una clase.</summary>
public interface IFileStorage
{
    string Name { get; }
    /// <summary>Sube el archivo y devuelve su URL pública.</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType);
}
