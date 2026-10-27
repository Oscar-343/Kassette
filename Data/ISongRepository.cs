using Kassette.DataStructures;
using Kassette.Models;
namespace Kassette.Data;

/// <summary>El resto del sistema solo conoce esta interfaz: cambiar la fuente de datos no lo afecta.</summary>
public interface ISongRepository
{
    TreeNode<LibraryItem> Library { get; }
    Song? GetById(int id);
}
