using Kassette.Data;
using Kassette.DataStructures;
using Kassette.Models;
namespace Kassette.Services;

public enum RepeatMode { Off, All, One }

public record PlayerState(Song? Current, bool Playing, string Repeat, bool Shuffle,
    Song[] Queue, Song[] History, Song[] Recent, int Nonce);

/// <summary>Único lugar donde se combinan las estructuras: cola, pila, buffer, listas y modos.</summary>
public class PlayerService
{
    private readonly ISongRepository _repo;
    private readonly object _gate = new();
    private readonly DoublyLinkedList<Song> _context = new();   // playlist actual
    private readonly MyQueue<Song> _queue = new();              // "A continuación"
    private readonly MyStack<Song> _history = new();            // botón "anterior"
    private readonly CircularBuffer<Song> _recent = new(10);    // últimas 10 escuchadas
    private readonly Random _rng = new();
    private Song? _current;
    private bool _playing, _shuffle;
    private RepeatMode _repeat = RepeatMode.Off;
    private int _nonce; // sube cada vez que hay que (re)iniciar el audio

    public PlayerService(ISongRepository repo) { _repo = repo; }

    public PlayerState Snapshot() { lock (_gate) return Build(); }

    private PlayerState Build() => new(_current, _playing, _repeat.ToString().ToLower(), _shuffle,
        _queue.Items().ToArray(), _history.Items().ToArray(), _recent.Items().ToArray(), _nonce);

    private void SetCurrent(Song s) { _current = s; _recent.Add(s); _playing = true; _nonce++; }

    // Fisher-Yates in situ. O(n)
    private void FisherYates(Song[] a)
    {
        for (int i = a.Length - 1; i > 0; i--) { int j = _rng.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
    }

    // Recarga la cola con lo que sigue después de la canción actual
    private void LoadQueue()
    {
        _queue.Clear();
        if (_current == null) return;
        var cur = _current;
        var rest = _shuffle
            ? _context.Forward().Where(s => s.Id != cur.Id).ToArray()
            : _context.Forward().SkipWhile(s => s.Id != cur.Id).Skip(1).ToArray();
        if (_shuffle) FisherYates(rest);
        foreach (var s in rest) _queue.Enqueue(s);
    }

    public PlayerState Play(int[] ids, int startId)
    {
        lock (_gate)
        {
            _context.Clear();
            foreach (var id in ids) { var s = _repo.GetById(id); if (s != null) _context.AddLast(s); }
            var start = _context.Forward().FirstOrDefault(s => s.Id == startId) ?? _context.Forward().FirstOrDefault();
            if (start == null) throw new InvalidOperationException("La lista a reproducir está vacía.");
            if (_current != null) _history.Push(_current);
            SetCurrent(start); LoadQueue();
            return Build();
        }
    }

    public PlayerState Toggle() { lock (_gate) { if (_current != null) _playing = !_playing; return Build(); } }

    public PlayerState Next(bool auto)
    {
        lock (_gate)
        {
            if (_current == null) return Build();
            if (auto && _repeat == RepeatMode.One) { _playing = true; _nonce++; return Build(); } // repite la misma
            if (_queue.Count == 0)
            {
                if (_repeat == RepeatMode.All && _context.Count > 0) RefillCircular();
                else { _playing = false; return Build(); } // se acabó la cola
            }
            _history.Push(_current);
            SetCurrent(_queue.Dequeue());
            return Build();
        }
    }

    // "Repetir todo": se arma una lista circular con el contexto y se da una vuelta completa
    private void RefillCircular()
    {
        var circ = new CircularList<Song>();
        foreach (var s in _context.Forward()) circ.Add(s);
        var cur = _current!;
        var lap = circ.EnumerateFrom(s => s.Id == cur.Id).Skip(1).Append(cur).ToArray();
        if (_shuffle) FisherYates(lap);
        foreach (var s in lap) _queue.Enqueue(s);
    }

    public PlayerState Previous()
    {
        lock (_gate)
        {
            if (_current == null) return Build();
            if (_history.Count == 0) { _nonce++; _playing = true; return Build(); } // sin historial: reinicia
            var prev = _history.Pop();
            // la canción actual vuelve al frente de la cola
            var rebuilt = new MyQueue<Song>();
            rebuilt.Enqueue(_current);
            foreach (var s in _queue.Items()) rebuilt.Enqueue(s);
            _queue.Clear();
            foreach (var s in rebuilt.Items()) _queue.Enqueue(s);
            SetCurrent(prev);
            return Build();
        }
    }

    public PlayerState Enqueue(int id)
    {
        lock (_gate)
        {
            var s = _repo.GetById(id) ?? throw new InvalidOperationException("La canción no existe.");
            if (_current == null) { _context.AddLast(s); SetCurrent(s); }
            else _queue.Enqueue(s);
            return Build();
        }
    }

    public PlayerState ToggleShuffle() { lock (_gate) { _shuffle = !_shuffle; LoadQueue(); return Build(); } }

    public PlayerState CycleRepeat()
    {
        lock (_gate) { _repeat = (RepeatMode)(((int)_repeat + 1) % 3); return Build(); }
    }
}
