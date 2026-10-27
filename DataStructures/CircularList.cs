namespace Kassette.DataStructures;

/// <summary>Lista circular simple: el último nodo apunta al primero. Base del modo "repetir todo".</summary>
public class CircularList<T>
{
    private Node<T>? _tail; // _tail.Next es la cabeza
    public int Count { get; private set; }

    // O(1)
    public void Add(T value)
    {
        var n = new Node<T>(value);
        if (_tail == null) { n.Next = n; } else { n.Next = _tail.Next; _tail.Next = n; }
        _tail = n; Count++;
    }

    /// <summary>Da una vuelta completa empezando en el primer elemento que cumpla el predicado. O(n)</summary>
    public IEnumerable<T> EnumerateFrom(Func<T, bool> isStart)
    {
        if (_tail == null) throw new InvalidOperationException("La lista circular está vacía.");
        var start = _tail.Next!;
        for (int i = 0; i < Count; i++, start = start.Next!)
            if (isStart(start.Value)) break;
        var cur = start;
        for (int i = 0; i < Count; i++, cur = cur.Next!) yield return cur.Value;
    }
}
