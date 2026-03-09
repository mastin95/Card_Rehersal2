# Plan de Refactorización de UI y Corrección de Errores

## Estado Actual (Cuánto llevamos hecho)
Se ha completado la **fase más agresiva del reporte anterior ("Estrategia B: La Purga Total")**. Las carpetas `/Network/`, `/GameServer/` y `/GameClient/` han sido eliminadas exitosamente del proyecto. Esta es una excelente noticia para hacer el juego 100% Single Player.

Sin embargo, como advertía el reporte, esto ha dejado "ciega" a la interfaz de usuario. Los 17+ errores actuales (incluyendo `CS0246: The type or namespace name 'LobbyGame' could not be found`) se deben a que los scripts de los menús (como `LobbyRoomPanel.cs`, `LobbyPanel.cs`, `PlayerLine.cs`, `LanPanel.cs` y `MainMenu.cs`) siguen intentando conectarse a servidores multijugador, buscar salas IP o usar el `GameClient` que ya no existe.

## Siguientes Pasos (Resolución de errores)

Para que el proyecto vuelva a compilar y se adapte al diseño Single Player, propongo las siguientes acciones:

### 1. Eliminación de Menús Exclusivamente Multijugador
Estos scripts (y sus respectivos GameObjects en las escenas) solo sirven para el multijugador online o LAN. Como ya no hay red, son código muerto y la causa principal de los errores actuales.
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/LobbyPanel.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/LobbyRoomPanel.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/LobbyCreatePanel.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/LobbyLine.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/PlayerLine.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/Menu/LanPanel.cs`
- **[DELETE]** `Assets/RogueEngine/Scripts/UI/ChatUI.cs`

### 2. Limpieza del Menú Principal (`MainMenu.cs`)
El menú principal tiene referencias directas a `LobbyClient`, `LobbyPanel`, etc.
- **[MODIFY]** `Assets/RogueEngine/Scripts/Menu/MainMenu.cs`: Removeremos todas las funciones relacionadas con multijugador (`OnClickMultiLobby`, `OnClickMultiLAN`, `StartMatchmaking`, referencias a `LobbyClient`, `GameClient.connect_settings`, etc.). Dejaremos únicamente los botones de "Nueva Partida Local" y "Cargar Partida".

### 3. Ajuste Básico a Scripts Restantes de la UI
Una vez eliminados los menús de Lobby, quedarán algunos errores en la carpeta `UI/` (como `BattleUI.cs` o `CardSelector.cs`) que aún usan `using RogueEngine.Client;`. 
En esta fase, limpiaremos los `using` inválidos y prepararemos estos scripts para que, en una fase posterior, se conecten directamente al nuevo `GameManager` (o `BattleLogic`) en lugar de `GameClient`.

## Verificación
### Pruebas Manuales
1. Entrar a Unity y dejar que recompile los scripts.
2. Comprobar en la consola que los 17 errores de compilación (`CS0246`) relacionados con `LobbyGame`, `GameClient` o `RogueEngine.Client` en la carpeta `Menu/` han desaparecido por completo.
3. Verificar que la escena `MainMenu` se puede abrir y visualizar sin errores del tipo "Missing Script" graves que impidan su ejecución inicial en modo local.

> [!CAUTION]
> Eliminar estos archivos requerirá que posteriormente entremos a las escenas de Unity (como la escena del menú principal) para borrar o apagar los objetos de UI que tenían estos scripts asignados, ya que aparecerán como "Missing Script". ¿Estás de acuerdo con proceder a borrar los scripts mencionados?
