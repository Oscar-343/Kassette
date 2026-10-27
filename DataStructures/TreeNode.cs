namespace Kassette.DataStructures;

/// <summary>Árbol n-ario (primer hijo + siguiente hermano). Biblioteca Artista → Álbum → Canción.</summary>
public class TreeNode<T>
{
    private TreeNode<T>? _lastChild;
    public T Value { get; }
    public TreeNode<T>? FirstChild { get; private set; }
    public TreeNode<T>? NextSibling { get; private set; }

    public TreeNode(T value) { Value = value; }

    // O(1)
    public TreeNode<T> AddChild(T value)
    {
        var c = new TreeNode<T>(value);
        if (FirstChild == null) FirstChild = c; else _lastChild!.NextSibling = c;
        _lastChild = c; return c;
    }

    public IEnumerable<TreeNode<T>> Children() { for (var c = FirstChild; c != null; c = c.NextSibling) yield return c; }

    // Recursivo, en profundidad: O(n) sobre el subárbol
    public IEnumerable<T> Descendants()
    {
        yield return Value;
        foreach (var c in Children()) foreach (var v in c.Descendants()) yield return v;
    }

    // Recursivo: cuenta nodos que cumplen el predicado. O(n)
    public int CountWhere(Func<T, bool> p) { int n = p(Value) ? 1 : 0; foreach (var c in Children()) n += c.CountWhere(p); return n; }

    // Recursivo: suma un valor numérico de todos los nodos. O(n)
    public int Sum(Func<T, int> f) { int s = f(Value); foreach (var c in Children()) s += c.Sum(f); return s; }

    // Recursivo: búsqueda en profundidad. O(n)
    public TreeNode<T>? Find(Func<T, bool> p)
    {
        if (p(Value)) return this;
        foreach (var c in Children()) { var r = c.Find(p); if (r != null) return r; }
        return null;
    }
}
