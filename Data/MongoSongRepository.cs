using Kassette.DataStructures;
using Kassette.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
namespace Kassette.Data;

/// <summary>Documento guardado en MongoDB. Solo texto: los archivos viven en Cloudflare R2.</summary>
public class SongDoc
{
    [BsonId] public ObjectId Id { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public int Track { get; set; }
    public int Duration { get; set; }
    public string AudioUrl { get; set; } = "";
    public string? CoverUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Lee el catálogo de MongoDB al iniciar y lo mantiene en memoria como árbol n-ario.
/// Al agregar una canción se inserta en MongoDB y también en el árbol, sin reiniciar.</summary>
public class MongoSongRepository : ISongCatalog
{
    private readonly IMongoCollection<SongDoc> _col;
    private readonly Dictionary<int, Song> _byId = new();
    private readonly object _gate = new();
    private int _nextId = 1;
    public TreeNode<LibraryItem> Library { get; }

    public MongoSongRepository(IConfiguration cfg)
    {
        var client = new MongoClient(cfg["Mongo:ConnectionString"]);
        _col = client.GetDatabase(cfg["Mongo:Database"] ?? "kassette").GetCollection<SongDoc>("songs");
        Library = new TreeNode<LibraryItem>(new LibraryItem(ItemKind.Root, "Biblioteca"));
        // Acceso a datos: aquí sí se permite la lista que devuelve el driver
        foreach (var d in _col.Find(Builders<SongDoc>.Filter.Empty)
                     .SortBy(x => x.Artist).ThenBy(x => x.Album).ThenBy(x => x.Track).ToList())
            AddToMemory(d);
    }

    public Song? GetById(int id) { lock (_gate) return _byId.TryGetValue(id, out var s) ? s : null; }

    public async Task<Song> AddAsync(NewSong n)
    {
        string? cover = n.CoverUrl;
        if (cover == null) // si el álbum ya tiene portada, se reutiliza
            lock (_gate)
                cover = _byId.Values.FirstOrDefault(s => s.Artist == n.Artist && s.Album == n.Album && s.Cover != null)?.Cover;
        var doc = new SongDoc
        {
            Title = n.Title, Artist = n.Artist, Album = n.Album, Track = n.Track, Duration = n.Duration,
            AudioUrl = n.AudioUrl, CoverUrl = cover, CreatedAt = DateTime.UtcNow
        };
        await _col.InsertOneAsync(doc);
        return AddToMemory(doc);
    }

    // Busca (o crea) artista y álbum en el árbol y cuelga la canción. O(artistas + álbumes)
    private Song AddToMemory(SongDoc d)
    {
        lock (_gate)
        {
            var song = new Song(_nextId++, d.Title, d.Artist, d.Album, d.Track, d.Duration, d.AudioUrl, d.CoverUrl);
            _byId[song.Id] = song;
            var artist = Library.Children().FirstOrDefault(c => c.Value.Name == d.Artist)
                         ?? Library.AddChild(new LibraryItem(ItemKind.Artist, d.Artist));
            var album = artist.Children().FirstOrDefault(c => c.Value.Name == d.Album)
                        ?? artist.AddChild(new LibraryItem(ItemKind.Album, d.Album));
            album.AddChild(new LibraryItem(ItemKind.Song, song.Title, song));
            return song;
        }
    }
}
