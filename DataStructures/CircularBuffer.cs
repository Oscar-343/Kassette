namespace Kassette.DataStructures;

/// <summary>Buffer circular de capacidad fija: al llenarse sobrescribe lo más antiguo.</summary>
public class CircularBuffer<T>
{
    private readonly T[] _items;
    private int _start; // posición del elemento más antiguo
    public int Count { get; private set; }

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0) throw new InvalidOperationException("La capacidad debe ser mayor que cero.");
        _items = new T[capacity];
    }

    // O(1): si está lleno, el nuevo ocupa el lugar del más antiguo y el inicio avanza
    public void Add(T value)
    {
        if (Count < _items.Length) _items[(_start + Count++) % _items.Length] = value;
        else { _items[_start] = value; _start = (_start + 1) % _items.Length; }
    }

    // O(n): del más reciente al más antiguo
    public IEnumerable<T> Items()
    {
        for (int i = Count - 1; i >= 0; i--) yield return _items[(_start + i) % _items.Length];
    }
}
