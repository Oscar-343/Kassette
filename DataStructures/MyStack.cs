namespace Kassette.DataStructures;

/// <summary>Pila LIFO sobre nodos enlazados. Se usa para el historial y el botón "anterior".</summary>
public class MyStack<T>
{
    private Node<T>? _top;
    public int Count { get; private set; }

    // O(1): el nuevo nodo apunta al tope anterior
    public void Push(T value) { _top = new Node<T>(value) { Next = _top }; Count++; }

    // O(1)
    public T Pop()
    {
        if (_top == null) throw new InvalidOperationException("La pila está vacía: no hay nada que sacar.");
        var v = _top.Value; _top = _top.Next; Count--; return v;
    }

    // O(1)
    public T Peek() => _top != null ? _top.Value : throw new InvalidOperationException("La pila está vacía.");

    public void Clear() { _top = null; Count = 0; } // O(1)

    // O(n): del tope hacia el fondo
    public IEnumerable<T> Items() { for (var n = _top; n != null; n = n.Next) yield return n.Value; }
}
