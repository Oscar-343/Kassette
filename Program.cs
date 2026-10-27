using Kassette.Data;
using Kassette.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddControllersWithViews();

// Sesión del panel de administración (cookie cifrada; contraseña en Admin:Password)
builder.Services.AddAuthentication("Admin").AddCookie("Admin", o =>
{
    o.LoginPath = "/Admin/Login";
    o.Cookie.Name = "kassette_admin";
    o.ExpireTimeSpan = TimeSpan.FromHours(4);
});
builder.Services.AddAuthorization();

// Catálogo: MongoDB si hay cadena de conexión; si no, catálogo de demostración (solo lectura)
if (!string.IsNullOrWhiteSpace(cfg["Mongo:ConnectionString"]))
{
    builder.Services.AddSingleton<MongoSongRepository>();
    builder.Services.AddSingleton<ISongRepository>(sp => sp.GetRequiredService<MongoSongRepository>());
    builder.Services.AddSingleton<ISongCatalog>(sp => sp.GetRequiredService<MongoSongRepository>());
}
else builder.Services.AddSingleton<ISongRepository, FolderSongRepository>();

// Archivos subidos: Cloudflare R2 si está configurado; si no, disco local (solo para desarrollo)
if (!string.IsNullOrWhiteSpace(cfg["R2:AccountId"])) builder.Services.AddSingleton<IFileStorage, R2FileStorage>();
else builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Un reproductor independiente por navegador (cookie), en lugar de uno global
builder.Services.AddSingleton<PlayerRegistry>();

// Render indica el puerto con la variable PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
