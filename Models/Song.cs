namespace Kassette.Models;

public record Song(int Id, string Title, string Artist, string Album, int Track, int Duration, string? Url, string? Cover);
public enum ItemKind { Root, Artist, Album, Song }
public record LibraryItem(ItemKind Kind, string Name, Song? Song = null);
