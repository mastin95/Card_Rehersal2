# Reporte de Dependencias de Red y Servidor

He realizado un barrido profundo de todo el proyecto para identificar las raíces del sistema Multijugador/Servidor. Como sospechabas, este Asset trae un sistema de red que, si deseas hacer un juego 100% Single Player (offline), es código muerto que solo añade peso y complejidad.

Sin embargo, **el sistema de red está profundamente arraigado en la Interfaz de Usuario (UI)**. Aquí tienes el mapa completo de lo que habría que aislar o eliminar.

## 1. Carpetas y Archivos 100% Red/Servidor (Borrables o Descartables)
Estos directorios existen pura y exclusivamente para gestionar el multijugador y la conexión de datos externa.

- **`/Assets/RogueEngine/Scripts/Network/`**: Contiene el núcleo de la red (`TcgNetwork.cs`, `NetworkMessaging.cs`, `NetworkData.cs`).
- **`/Assets/RogueEngine/Scripts/GameServer/`**: Contiene la lógica autoritativa si el jugador hace de anfitrión (`GameServer.cs`, `LobbyServer.cs`, `ServerManager.cs`).
- **`/Assets/RogueEngine/Scripts/GameClient/`**: El cliente de conexión (`GameClient.cs`, `LobbyClient.cs`).
- **Escenas**: `/Assets/RogueEngine/Scenes/Server/Server.unity` (Escena dedicada a levantar un servidor headless).

## 2. Archivos Gravemente Infectados (Acoplamiento de UI)
El creador del Asset diseñó la arquitectura bajo el patrón *"La UI pregunta al Cliente, y el Cliente pregunta a la Lógica"*. 

Actualmente, **casi todos los scripts de la carpeta `/UI/` dependen de `GameClient.cs` o `TcgNetwork.cs`**. 
En lugar de que un botón lea el archivo `BattleLogic` directamente, hace esto:
`GameClient.Get().GetBattle()` o `GameClient.Get().EndTurn()`.

Si borramos la carpeta `GameClient` ahora mismo, Unity lanzará cientos de errores de compilación porque estos scripts visuales se quedarán "ciegos":

- `BattleUI.cs` (Pide datos del turno, avisa cuando juegas una carta).
- `CardSelector.cs` / `CardPreviewUI.cs` / `ChoiceSelector.cs`
- `ChampionUI.cs` / `ChampionPanel.cs` / `CharacterUI.cs`
- `ChatUI.cs` (Sistema de chat multijugador, 100% inútil en singleplayer).

## 3. El Menú Principal
El `MainMenu.cs` tiene lógica y botones enteros dedicados a conectarse a servidores, iniciar Host/Relay o leer respuestas de `LobbyClient`.

---

## Estrategias de Eliminación (Sugerencias)

Dado que arrancar el multijugador de raíz rompería la Interfaz Gráfica temporalmente, tenemos dos caminos para crear tu "Versión Single Player Limpia":

### Estrategia A: "La Extracción Quirúrgica" (Recomendada)
En lugar de borrar `GameClient.cs`, lo vaciamos por dentro. Convertimos `GameClient.cs` en un simple "Manager Local" que se conecte directamente a `BattleLogic.cs` (tu motor local) sin pasar por ningún `TcgNetwork`. 
1. Borramos todo rastro de `TcgNetwork` e IPs.
2. La UI sigue funcionando sin cambiar ni una línea de código porque ella sigue llamando a `GameClient`, pero ahora `GameClient` es 100% offline y engaña a la UI.
3. Borramos las carpetas `Network` y `GameServer` con seguridad.

### Estrategia B: "La Purga Total" (Refactorización Agresiva)
1. Borramos `Network`, `GameServer` y `GameClient`.
2. Vamos script por script en la carpeta `/UI/` (unas 20-30 clases) y reemplazamos todas las llamadas de `GameClient.Get().X()` por llamadas directas al Motor de Juego (`GameLogic`).
3. Esto es más "limpio" a largo plazo para un juego singleplayer, pero dará muchos errores iniciales y requiere mucho cuidado.

¿Qué aproximación prefieres que tomemos para esta nueva versión sin servidor?
