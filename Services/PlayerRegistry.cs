using System.Collections.Concurrent;
using Kassette.Data;
namespace Kassette.Services;

/// <summary>Un PlayerService por navegador, identificado por una cookie. Evita que dos visitantes compartan la cola.</summary>
public class PlayerRegistry(ISongRepository repo)
{
    private class Entry(PlayerService player) { public PlayerService Player { get; } = player; public DateTime LastSeen; }
    private readonly ConcurrentDictionary<string, Entry> _sessions = new();

    public PlayerService For(HttpContext ctx)
    {
        var sid = ctx.Request.Cookies["kassette_sid"];
        if (string.IsNullOrEmpty(sid) || sid.Length > 64)
        {
            sid = Guid.NewGuid().ToString("N");
            ctx.Response.Cookies.Append("kassette_sid", sid, new CookieOptions
            { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true, MaxAge = TimeSpan.FromDays(7) });
        }
        Prune();
        var e = _sessions.GetOrAdd(sid, _ => new Entry(new PlayerService(repo)));
        e.LastSeen = DateTime.UtcNow;
        return e.Player;
    }

    // Libera la memoria de sesiones inactivas por más de 2 horas
    private void Prune()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var kv in _sessions) if (kv.Value.LastSeen < cutoff) _sessions.TryRemove(kv.Key, out _);
    }
}
