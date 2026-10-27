# Kassette

Reproductor de música web con estética retro de cassette. Su propósito es demostrar estructuras de datos
(pila, cola, listas, buffer circular, árbol y recursividad) funcionando en un sistema real.

Catálogo en MongoDB Atlas, archivos (MP3 y portadas) en Cloudflare R2, despliegue en Render con Docker.
Toda la explicación está en la carpeta Documentacion/ (empieza por 00-indice.txt).

Ejecutar en local (.NET 10 SDK):

    dotnet run
    dotnet test Kassette.Tests

Sin configurar MongoDB, la app arranca con un catálogo de demostración de solo lectura.
