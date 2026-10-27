namespace Kassette.Data;

using Kassette.Models;

/// <summary>Datos de una canción nueva (los archivos ya están subidos y solo llegan sus URLs).</summary>
public record NewSong(string Title, string Artist, string Album, int Track, int Duration, string AudioUrl, string? CoverUrl);

/// <summary>Cambios de una canción. CoverUrl null significa "dejar la portada como está".</summary>
public record SongEdit(string Title, string Artist, string Album, int Track, string? CoverUrl);

/// <summary>Resultado de editar: OrphanCoverUrl es la portada anterior si ya nadie la usa (hay que borrarla del almacenamiento).</summary>
public record EditResult(Song Song, string? OrphanCoverUrl);

/// <summary>Resultado de eliminar: archivos que hay que borrar del almacenamiento.</summary>
public record DeletedSong(string AudioUrl, string? OrphanCoverUrl);

/// <summary>Catálogo que además permite agregar, editar y eliminar canciones.</summary>
public interface ISongCatalog : ISongRepository
{
    Task<Song> AddAsync(NewSong song);
    Task<EditResult?> UpdateAsync(int id, SongEdit edit);
    Task<DeletedSong?> DeleteAsync(int id);
}

/// <summary>Dónde se guardan los archivos (MP3 y portadas). Cambiar de R2 a otro servicio solo toca una clase.</summary>
public interface IFileStorage
{
    string Name { get; }
    /// <summary>Sube el archivo y devuelve su URL pública.</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType);
    /// <summary>Borra el archivo a partir de su URL pública. Si la URL no es de este almacenamiento, no hace nada.</summary>
    Task DeleteAsync(string url);
}
