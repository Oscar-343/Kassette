using Kassette.DataStructures;
namespace Kassette.Models;

public static class LibraryExtensions
{
    public static Song[] Songs(this TreeNode<LibraryItem> n) =>
        n.Descendants().Where(i => i.Kind == ItemKind.Song).Select(i => i.Song!).ToArray();
    public static string Fmt(int s) => s <= 0 ? "--:--" : $"{s / 60}:{s % 60:00}";
}
