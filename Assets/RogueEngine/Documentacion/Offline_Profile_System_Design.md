# Documento de Diseño Arquitectónico: Sistema de Perfiles Offline (Sustituto de Authenticator)

Este documento expone el análisis de viabilidad y el diseño técnico propuesto para crear un sistema **nativo offline** que sustituya definitivamente al "Zombi" actual (`Authenticator.cs`) y asuma el control absoluto de la progresión de los jugadores.

---

## 1. Análisis de Viabilidad (El Estado Actual)

Tras analizar los archivos base de guardado del motor (`UserData.cs` y `ProgressManager.cs`), la buena noticia es que **el sistema es 100% viable y sorpresivamente fácil de implementar**. 

Actualmente, a pesar de que el Asset Original tenía un sistema de login online, la clase `UserData.cs` (que almacena las cartas desbloqueadas, avatares y misiones) **ya incluye código nativo para guardarse físicamente en tu disco duro**. Posee funciones como `UserData.Save()` y `UserData.Load()` que usan un encriptador local (`SaveTool.cs`). 

El antiguo `Authenticator` no era más que un "intermediario" burocrático que cogía esos archivos locales y los intentaba sincronizar con una base de datos externa. Al quitarlo, tenemos vía libre para crear un gestor de perfiles local al estilo de cualquier juego Single Player clásico (como *Slay the Spire* o *Hollow Knight*).

---

## 2. El Nuevo Concepto: El `ProfileManager` (Gestor de Perfiles Locales)

El objetivo es crear un nuevo script central (*Singleton*) llamado `ProfileManager.cs` que erradicará el concepto de "Iniciar Sesión" y lo sustituirá por "Slots de Guardado" o "Perfiles Locales".

### 2.1 Misiones del `ProfileManager`
1. **Punto Único de Verdad:** Será el script al que todos los demás (Tiendas, Inventarios, Batalla) preguntarán: *"¿Qué cartas tiene desbloqueadas este jugador?"*
2. **Auto-Guardado (Autosave):** Sustituir las funciones asíncronas de subida a red por un simple guardado en el disco C:/ del ordenador al terminar cada combate.
3. **Gestión de Slots:** Permitir que si tu hermano quiere jugar en el mismo PC, pueda crearse el "Perfil 2" con sus cartas bloqueadas y sus propios progresos.

---

## 3. RoadMap de Implementación Técnica (Cómo hacerlo paso a paso)

Si aprobamos la construcción de este sistema, el plan de trabajo arquitectónico sería el siguiente:

### Fase 1: Creación del `ProfileManager.cs` (El Archivo Funcional)
Crearemos un script limpio en la carpeta `Scripts/Core/` que gestionará la memoria RAM del jugador activo.
```csharp
namespace RogueEngine
{
    public class ProfileManager : MonoBehaviour
    {
        private static ProfileManager instance;
        public UserData current_profile;

        // Carga un archivo .user del disco duro
        public void LoadProfile(string profile_name) 
        {
            current_profile = UserData.Load(profile_name);
            if (current_profile == null) {
                current_profile = UserData.NewUser(profile_name, profile_name); // Genera uno nuevo
            }
        }

        // Guarda localmente
        public void SaveCurrentProfile() 
        {
            if (current_profile != null) current_profile.Save();
        }
    }
}
```

### Fase 2: El Gran Reto - Cirugía a Corazón Abierto (Extraer Authenticator)
**Aquí reside el verdadero peso y peligro técnico de esta mutación.**
Borrar `Authenticator.cs` no es simplemente apretar 'Delete'. Como demostramos en el Reporte de Dependencias (punto 1 de esta sesión), existen más de 15 scripts fundamentales altamente acoplados a él:
*   Gestores Matemáticos: `World.cs`, `ProgressManager.cs`
*   Gestores de Datos: `DataLoader.cs`, `SaveData.cs`
*   Decenas de Paneles Gráficos: `MainMenu`, `AvatarPanel`, `CollectionPanel`, `DeckPanel`, `ChampionPanel`.

**Los Desafíos Ocultos a resolver en esta Fase:**
1.  **Refactorización Masiva:** Tendríamos que abrir y manipular minuciosamente cada uno de estos 15+ scripts dispersos por toda la arquitectura.
2.  **Sincronización:** Sustituir líneas anticuadas de lectura (`Authenticator.Get().UserData`) por nuestra nueva arquitectura `ProfileManager.Get().current_profile`.
3.  **El Peligro Asíncrono (async/await):** El `Authenticator` antiguo forzaba a que funciones críticas del juego usaran programación asíncrona (`await`). Por ejemplo, la pantalla de "Game Over" o de "Recompensas" esperan X segundos preguntando a `Authenticator` si ya se subió tu oro ganado a la nube. Quitar `Authenticator` implica tener que recodificar de forma experta funciones `async` complejas en `ProgressManager` y adaptarlas a guardados instantáneos de un disco duro local, sin generar cuellos de botella ("*Freezes*") ni romper los "Tiempos" (*Timing*) que la Interfaz Gráfica espera para dibujar los paneles. Un error mínimo en un `await` puede bloquear la pantalla para siempre.

### Fase 3: Refactorización de Interfaz (De Login a Seleccionador de Slots)
El último paso se centra en la Experiencia de Usuario (Menús) y en el Diseño Visual.
- **Adiós Correos y Contraseñas:** El menú actual de "Login / Register" y las validaciones de texto asociadas se eliminarán o reciclarán de la UI.
- **Hola Selector de Partidas:** En su lugar, el Menú Principal tendría que ser rediseñado, creando 3 botones directos ("Slot 1", "Slot 2", "Slot 3") y campos de texto para escribir un "Nombre de Jugador" (Ej: "Mastin95") que inicie directamente el GameLoop y cargue instantáneamente el `ProfileManager` asociado.

---

## Conclusión Estratégica Revisada
Sustituir el stub `Authenticator` por un gestor nativo como `ProfileManager` es el **paso evolutivo y definitivo** a crear. 

No obstante, **no es una tarea de código rápida de "Buscar y Reemplazar"**. Constituye la etapa final del proceso de Limpieza Estructural de este motor (el "The Final Boss" de las dependencias), obligándonos a alterar decenas de archivos a lo largo y ancho del proyecto, y debiendo gestionar con pinzas las firmas de métodos asíncronos (`Tasks`) incrustadas en el Gestor de Progresión.
Solamente cuando este trabajo de "Cirugía Mayor" de la Fase 2 sea consumada con paciencia script por script, habremos transformado enteramente un Mánager de TCG Multiplayer en un Motor Profesional puramente Offline libre de deudas de red.
