namespace Kassette.DataStructures;

/// <summary>Lista doblemente enlazada: guarda el contexto (playlist) y se recorre en ambos sentidos.</summary>
public class DoublyLinkedList<T>
{
    private DNode<T>? _head, _tail;
    public int Count { get; private set; }

    // O(1)
    public void AddLast(T value)
    {
        var n = new DNode<T>(value) { Prev = _tail };
        if (_tail == null) _head = n; else _tail.Next = n;
        _tail = n; Count++;
    }

    public void Clear() { _head = _tail = null; Count = 0; } // O(1)

    // O(n)
    public IEnumerable<T> Forward() { for (var n = _head; n != null; n = n.Next) yield return n.Value; }

    // O(n)
    public IEnumerable<T> Backward() { for (var n = _tail; n != null; n = n.Prev) yield return n.Value; }
}
