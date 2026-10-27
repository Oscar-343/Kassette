using System.Text.RegularExpressions;
using Kassette.DataStructures;
using Kassette.Models;
namespace Kassette.Data;

/// <summary>Carga el catálogo recorriendo recursivamente wwwroot/music/{Artista}/{Álbum}/NN - Título.mp3.
/// Si no hay audios, carga un catálogo de demostración (sin sonido, reproducción simulada).</summary>
public class FolderSongRepository : ISongRepository
{
    private readonly Dictionary<int, Song> _byId = new();
    private readonly string _baseUrl;
    private int _nextId = 1;
    public TreeNode<LibraryItem> Library { get; }

    public FolderSongRepository(IWebHostEnvironment env, IConfiguration cfg)
    {
        _baseUrl = (cfg["Audio:BaseUrl"] ?? "/music").TrimEnd('/'); // nunca escrita a mano en el código
        Library = new TreeNode<LibraryItem>(new LibraryItem(ItemKind.Root, "Biblioteca"));
        var root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "music");
        if (Directory.Exists(root)) Scan(root, root, Library, null, null);
        if (_byId.Count == 0) SeedDemo();
    }

    public Song? GetById(int id) => _byId.TryGetValue(id, out var s) ? s : null;

    // Recursión: cada nivel de carpeta es un nivel del árbol. O(archivos)
    private void Scan(string root, string dir, TreeNode<LibraryItem> node, string? artist, string? album)
    {
        foreach (var sub in Directory.GetDirectories(dir).OrderBy(x => x))
        {
            var name = Path.GetFileName(sub);
            if (artist == null) Scan(root, sub, node.AddChild(new LibraryItem(ItemKind.Artist, name)), name, null);
            else if (album == null) Scan(root, sub, node.AddChild(new LibraryItem(ItemKind.Album, name)), artist, name);
        }
        if (artist == null || album == null) return;
        var coverFile = Path.Combine(dir, "cover.jpg");
        var cover = File.Exists(coverFile) ? UrlFor(root, coverFile) : null; // sin portada → portada por defecto
        foreach (var f in Directory.GetFiles(dir, "*.mp3").OrderBy(x => x))
        {
            var m = Regex.Match(Path.GetFileNameWithoutExtension(f), @"^(\d+)\s*-\s*(.+)$");
            int track = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            string title = m.Success ? m.Groups[2].Value : Path.GetFileNameWithoutExtension(f);
            AddSong(node, new Song(_nextId++, title, artist, album, track, 0, UrlFor(root, f), cover));
        }
    }

    private string UrlFor(string root, string file) =>
        _baseUrl + "/" + string.Join("/", Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Select(Uri.EscapeDataString));

    private void AddSong(TreeNode<LibraryItem> albumNode, Song s)
    {
        _byId[s.Id] = s;
        albumNode.AddChild(new LibraryItem(ItemKind.Song, s.Title, s));
    }

    private void SeedDemo()
    {
        void Album(string artist, string album, params (string t, int d)[] songs)
        {
            var a = Library.Find(i => i.Kind == ItemKind.Artist && i.Name == artist)
                    ?? Library.AddChild(new LibraryItem(ItemKind.Artist, artist));
            var al = a.AddChild(new LibraryItem(ItemKind.Album, album));
            int n = 1;
            foreach (var (t, d) in songs) AddSong(al, new Song(_nextId++, t, artist, album, n++, d, null, null));
        }
        Album("Neon Tapes", "Lado A", ("Fósforo", 185), ("Cinta vieja", 212), ("Rebobinar", 164));
        Album("Neon Tapes", "Lado B", ("Estática", 198), ("Carrete", 176));
        Album("Duo Analógico", "Voltaje", ("Corriente alterna", 221), ("Cable pelado", 143), ("Tierra", 190));
        Album("Ruido Blanco", "Sintonía", ("Dial", 205), ("Onda media", 168));
    }
}
