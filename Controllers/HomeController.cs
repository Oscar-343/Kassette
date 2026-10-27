using Kassette.Data;
using Kassette.Models;
using Microsoft.AspNetCore.Mvc;
namespace Kassette.Controllers;

public class HomeController(ISongRepository repo) : Controller
{
    public IActionResult Index() => View(repo.Library);
    public IActionResult Library() => View(repo.Library);
    public IActionResult Playlist() => View(repo.Library);

    public IActionResult Artist(string name)
    {
        var n = repo.Library.Find(i => i.Kind == ItemKind.Artist && i.Name == name);
        return n == null ? NotFound() : View(n);
    }

    public IActionResult Album(string artist, string name)
    {
        var a = repo.Library.Find(i => i.Kind == ItemKind.Artist && i.Name == artist);
        var al = a?.Find(i => i.Kind == ItemKind.Album && i.Name == name);
        return al == null ? NotFound() : View(al);
    }
}
