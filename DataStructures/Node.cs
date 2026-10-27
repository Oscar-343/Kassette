namespace Kassette.DataStructures;

/// <summary>Nodo simple: base de la pila, la cola y la lista circular.</summary>
public class Node<T>
{
    public T Value;
    public Node<T>? Next;
    public Node(T value) { Value = value; }
}

/// <summary>Nodo con enlace en ambos sentidos.</summary>
public class DNode<T>
{
    public T Value;
    public DNode<T>? Prev, Next;
    public DNode(T value) { Value = value; }
}
