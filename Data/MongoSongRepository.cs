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
/// Al agregar, editar o eliminar se actualiza MongoDB y el árbol, sin reiniciar.
/// El id numérico de cada canción no cambia al editarla (las colas de los visitantes siguen funcionando).</summary>
public class MongoSongRepository : ISongCatalog
{
    private readonly IMongoCollection<SongDoc> _col;
    private readonly Dictionary<int, Song> _byId = new();
    private readonly Dictionary<int, SongDoc> _docs = new();
    private readonly object _gate = new();
    private int _nextId = 1;
    public TreeNode<LibraryItem> Library { get; private set; }

    public MongoSongRepository(IConfiguration cfg)
    {
        var client = new MongoClient(cfg["Mongo:ConnectionString"]);
        _col = client.GetDatabase(cfg["Mongo:Database"] ?? "kassette").GetCollection<SongDoc>("songs");
        Library = NewRoot();
        // Acceso a datos: aquí sí se permite la lista que devuelve el driver
        foreach (var d in _col.Find(Builders<SongDoc>.Filter.Empty)
                     .SortBy(x => x.Artist).ThenBy(x => x.Album).ThenBy(x => x.Track).ToList())
            AddToMemory(d);
    }

    private static TreeNode<LibraryItem> NewRoot() => new(new LibraryItem(ItemKind.Root, "Biblioteca"));

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

    public async Task<EditResult?> UpdateAsync(int id, SongEdit e)
    {
        SongDoc? doc;
        lock (_gate) { if (!_docs.TryGetValue(id, out doc)) return null; }
        var update = Builders<SongDoc>.Update.Set(x => x.Title, e.Title).Set(x => x.Artist, e.Artist)
            .Set(x => x.Album, e.Album).Set(x => x.Track, e.Track);
        if (e.CoverUrl != null) update = update.Set(x => x.CoverUrl, e.CoverUrl);
        await _col.UpdateOneAsync(x => x.Id == doc.Id, update);

        lock (_gate)
        {
            var oldCover = doc.CoverUrl;
            doc.Title = e.Title; doc.Artist = e.Artist; doc.Album = e.Album; doc.Track = e.Track;
            if (e.CoverUrl != null) doc.CoverUrl = e.CoverUrl;
            var song = new Song(id, doc.Title, doc.Artist, doc.Album, doc.Track, doc.Duration, doc.AudioUrl, doc.CoverUrl);
            _byId[id] = song;
            Rebuild(); // puede haber cambiado de álbum o de artista
            string? orphan = e.CoverUrl != null && oldCover != null && oldCover != e.CoverUrl && !IsCoverUsed(oldCover) ? oldCover : null;
            return new EditResult(song, orphan);
        }
    }

    public async Task<DeletedSong?> DeleteAsync(int id)
    {
        SongDoc? doc;
        lock (_gate) { if (!_docs.TryGetValue(id, out doc)) return null; }
        await _col.DeleteOneAsync(x => x.Id == doc.Id);
        lock (_gate)
        {
            _byId.Remove(id); _docs.Remove(id);
            Rebuild();
            return new DeletedSong(doc.AudioUrl, doc.CoverUrl != null && !IsCoverUsed(doc.CoverUrl) ? doc.CoverUrl : null);
        }
    }

    private bool IsCoverUsed(string cover) => _byId.Values.Any(s => s.Cover == cover); // O(n)

    // Cuelga la canción en el árbol, creando artista y álbum si faltan. O(artistas + álbumes)
    private static void InsertIntoTree(TreeNode<LibraryItem> root, Song song)
    {
        var artist = root.Children().FirstOrDefault(c => c.Value.Name == song.Artist)
                     ?? root.AddChild(new LibraryItem(ItemKind.Artist, song.Artist));
        var album = artist.Children().FirstOrDefault(c => c.Value.Name == song.Album)
                    ?? artist.AddChild(new LibraryItem(ItemKind.Album, song.Album));
        album.AddChild(new LibraryItem(ItemKind.Song, song.Title, song));
    }

    // Arma un árbol nuevo ordenado por artista, álbum y pista, y lo intercambia. O(n log n)
    private void Rebuild()
    {
        var root = NewRoot();
        foreach (var s in _byId.Values.OrderBy(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Track))
            InsertIntoTree(root, s);
        Library = root;
    }

    private Song AddToMemory(SongDoc d)
    {
        lock (_gate)
        {
            var song = new Song(_nextId++, d.Title, d.Artist, d.Album, d.Track, d.Duration, d.AudioUrl, d.CoverUrl);
            _byId[song.Id] = song; _docs[song.Id] = d;
            InsertIntoTree(Library, song);
            return song;
        }
    }
}
