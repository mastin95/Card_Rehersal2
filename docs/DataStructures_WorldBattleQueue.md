# Arquitectura de Datos: World, Battle y el Caché de Eventos

Este documento resuelve las preguntas planteadas sobre las estructuras de datos fundamentales del motor (`World`, `Battle`, `ResolveQueue`) y explica la lógica detrás de la inicialización de diccionarios en C#.

---

## 1. El Concepto del Diccionario: Llaves (Keys) y Valores (Values)

Una pregunta excelente fue: *"¿Por qué hace falta guardar los enums en el diccionario si ya los tenemos en la clase?"*

Esta es la respuesta técnica de Arquitectura: **El Diccionario NO guarda el Enum para saber qué es el Enum. El Diccionario usa el Enum como una "Dirección de Correo Postal".**

### El Problema de la Iteración
Imagina que eres un cartero (`BattleLogic`). Tienes una carta que dice: "Debes entregar 5 puntos de daño a quien reaccione al Daño".
En el sistema antiguo (Sin Diccionario):
El cartero tiene que ir **casa por casa** (iterando `foreach` sobre todas las cartas en juego) y tocar el timbre:
- *"Hola carta de la mano nº1, ¿tienes una habilidad de daño?"* -> *"No"*.
- *"Hola carta de escudo nº2, ¿tienes una habilidad de daño?"* -> *"No"*.
- *"Hola carta de espinas nº3, ¿tienes una habilidad de daño?"* -> *"¡SÍ! Toma mis 5 de daño"*.
Si hay 100 cartas, hiciste 99 preguntas inútiles.

### La Solución del Diccionario (Hash Map)
Un `Dictionary<Key, Value>` crea un índice matemático directo (Caché de O(1)).
Definimos nuestro diccionario como: `Dictionary<AbilityTrigger, List<Card>>`.
- **Key (La Llave / La Dirección):** El `AbilityTrigger` (Ej: el enum `OnDamaged`).
- **Value (El Valor / Los Habitantes):** Una Lista de Cartas (`List<Card>`).

Al inicio del combate, el Diccionario genera "casas vacías" con direcciones perfectas:
- Casa 1: Dirección `[OnPlay]` -> Habitantes: {Nadie}
- Casa 2: Dirección `[OnDamaged]` -> Habitantes: {Nadie}

Cuando juegas la "Carta de Espinas", la función `RegisterCardToCache` lee su perfil, ve que reacciona al daño, y la mete como "Habitante" en la casa `[OnDamaged]`.
Ahora, cuando el Cartero tiene que repartir daño, simplemente dice: **"Llévame directo a la casa `[OnDamaged]`"**. Abre la puerta, y las Cartas que están dentro reciben el impacto instantáneamente. 0 preguntas inútiles. Rendimiento absoluto.

---

## 2. Estructura Global: `World.cs`

`World.cs` es el **Estado Global Persistente** del juego. Es la caja fuerte matemática.

### Propósito
Representa todo lo que debe sobrevivir cuando cierras el juego o cambias de escena (del Mapa a una Batalla). De hecho, **la clase entera de `World` es lo que se guarda en el archivo de guardado (`.tcg`)**.
Si miras la línea `10`, la clase empieza con `[System.Serializable]`, lo cual le dice a Unity: "Convierte todo esto en texto/bytes para guardarlo en el disco duro".

### Responsabilidades
1. **Los Jugadores y Servidores:** Gestiona si estás conectado online (`public bool online;`, `public List<Player> players;`).
2. **El Meta-Juego:** Guarda en qué nodo del mapa estás (`map_location_id`), tus aliados, tus estadísticas custom (`custom_ints`), los niveles y XP de los campeones.
3. **El Árbitro de Escenas:** A través de su variable `WorldState state` (Mapa, Batalla, Tienda, Recompensa), le dice a la Interfaz de Usuario qué pantalla tiene que dibujar.

---

## 3. Estructura de Combate: `Battle.cs`

`Battle.cs` es el **Estado Temporal de Combate**. Si `World` es el disco duro, `Battle` es la memoria RAM.

### Propósito
A diferencia de `BattleLogic` (que es el cerebro/calculadora), `Battle.cs` es puramente el contenedor de los datos del tapete de juego actual. Todo lo que ocurre en una pelea se guarda aquí.

### Responsabilidades
1. **El Tablero Físico:** Contiene la lista de personajes físicos invocados en la pelea (`public List<BattleCharacter> characters`).
2. **Los Turnos:** Sabe a quién le toca jugar (`active_character`) y en qué fase del turno estamos (`phase`, `turn_count`).
3. **El Selector:** Mantiene apuntado si estás en mitad de elegir un objetivo con la flecha roja (`selector_caster_uid`, `selector_card_uid`).
4. **Buscador de Cartas:** Es el lugar inteligente para buscar dónde está una carta físicamente, por ejemplo `GetDeckCard(uid)` o `IsInHand(card)`.

### ¿Por qué están separados `World` y `Battle`?
Por modularidad y limpieza de memoria. Cuando completas un combate y vuelves al mapa, todo el archivo `Battle` completo se borra de la faz de la tierra de la memoria RAM del ordenador (Garbage Collection), dejando el mundo limpio. Al entrar en el siguiente monstruo, se crea un `Battle` vacío y nuevo. Si combinas los dos archivos, la basura de las batallas anteriores corrompería el progreso.

---

*La arquitectura de `ResolveQueue.cs` está explicada exhaustivamente en el documento principal [BattleLogic_FullCodeBreakdown](file:///C:/Users/Usuario/.gemini/antigravity/brain/2805a7d7-d6f4-4763-a4a1-68627f276b67/BattleLogic_FullCodeBreakdown.md), en la sección 6.*
