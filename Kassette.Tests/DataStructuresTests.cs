using Kassette.DataStructures;
using Xunit;
namespace Kassette.Tests;

public class DataStructuresTests
{
    [Fact] public void Stack_IsLifo() { var s = new MyStack<int>(); s.Push(1); s.Push(2); Assert.Equal(2, s.Pop()); Assert.Equal(1, s.Pop()); }
    [Fact] public void Stack_Empty_Throws() => Assert.Throws<InvalidOperationException>(() => new MyStack<int>().Pop());
    [Fact] public void Queue_IsFifo() { var q = new MyQueue<int>(); q.Enqueue(1); q.Enqueue(2); Assert.Equal(1, q.Dequeue()); Assert.Equal(2, q.Dequeue()); Assert.Equal(0, q.Count); }
    [Fact] public void Queue_Empty_Throws() => Assert.Throws<InvalidOperationException>(() => new MyQueue<int>().Dequeue());
    [Fact] public void Buffer_OverwritesOldest() { var b = new CircularBuffer<int>(3); for (int i = 1; i <= 5; i++) b.Add(i); Assert.Equal(new[] { 5, 4, 3 }, b.Items().ToArray()); }
    [Fact] public void CircularList_WrapsAround() { var c = new CircularList<int>(); foreach (var i in new[] { 1, 2, 3 }) c.Add(i); Assert.Equal(new[] { 2, 3, 1 }, c.EnumerateFrom(x => x == 2).ToArray()); }
    [Fact] public void DoublyList_BothDirections() { var l = new DoublyLinkedList<int>(); l.AddLast(1); l.AddLast(2); Assert.Equal(new[] { 2, 1 }, l.Backward().ToArray()); }
    [Fact] public void Tree_RecursiveCountAndFind()
    {
        var r = new TreeNode<string>("root"); var a = r.AddChild("a"); a.AddChild("x"); r.AddChild("b");
        Assert.Equal(4, r.CountWhere(_ => true)); Assert.NotNull(r.Find(v => v == "x"));
    }
}
