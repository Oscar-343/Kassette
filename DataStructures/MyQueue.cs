namespace Kassette.DataStructures;

/// <summary>Cola FIFO con punteros a frente y final. Se usa para "A continuación".</summary>
public class MyQueue<T>
{
    private Node<T>? _head, _tail;
    public int Count { get; private set; }

    // O(1): se enlaza al final gracias al puntero _tail
    public void Enqueue(T value)
    {
        var n = new Node<T>(value);
        if (_tail == null) _head = _tail = n; else { _tail.Next = n; _tail = n; }
        Count++;
    }

    // O(1)
    public T Dequeue()
    {
        if (_head == null) throw new InvalidOperationException("La cola está vacía: no hay siguiente canción.");
        var v = _head.Value; _head = _head.Next;
        if (_head == null) _tail = null; // quedó vacía
        Count--; return v;
    }

    public T Peek() => _head != null ? _head.Value : throw new InvalidOperationException("La cola está vacía.");
    public void Clear() { _head = _tail = null; Count = 0; } // O(1)

    // O(n)
    public IEnumerable<T> Items() { for (var n = _head; n != null; n = n.Next) yield return n.Value; }
}
