# Arquitectura Core: Análisis en Profundidad de `BattleLogic.cs`

Este documento detalla el funcionamiento interno del componente más crítico del Rogue Engine: `BattleLogic.cs`. Actúa como el cerebro del juego, dictando cómo interaccionan las cartas, los personajes y el flujo del tiempo durante el combate.

---

## 1. El Paradigma de "Iteración Centralizada"

A simple vista, en el paradigma de Programación Orientada a Objetos (OOP), uno esperaría que cada Carta fuera un ente independiente que recibe notificaciones de eventos (mediante delegados C# como `Action` o `UnityEvent`). 

Sin embargo, el creador de Rogue Engine tomó la decisión consciente de **invertir el flujo de control**. En este motor, las cartas son "contenedores de datos pasivos" (Scripts `Card` con referencias a `AbilityData`), y **es el controlador central (`BattleLogic.cs`) quien itera activamente** sobre las entidades del tablero para preguntarles qué deben hacer.

### Flujo de Ejecución de un Trigger (Ej: `OnPlay`)
1. El jugador arrastra una carta al tablero.
2. `BattleLogic.PlayCard()` es invocado.
3. Se resta el Maná al jugador, se mueve la carta de la lista "Mano" a la lista "Descarte".
4. `BattleLogic` llama internamente a la función `TriggerCardAbilityType(AbilityTrigger.OnPlay, card)`.
5. Esta función lee todas las Habilidades (`AbilityData`) asociadas a la carta jugada y lanza `TriggerAbility()`.

### Flujo de Ejecución de un Trigger Reactivo (Ej: `OnDamaged`)
1. Un enemigo ataca y llama a `BattleCharacter.TakeDamage()`.
2. El daño afecta a los PS del personaje.
3. El sistema llama a `BattleLogic.TriggerCharacterAbilityType(AbilityTrigger.OnDamaged, character)`.
4. Aquí ocurre la Iteración Centralizada:
   * El código hace un `foreach` por los Traits pasivos del personaje.
   * Hace un **segundo** `foreach` por las cartas Activas (`cards_power`).
   * Hace un **tercer** `foreach` por las reliquias equipadas (`cards_item`).
5. Por cada elemento encontrado, evalúa si sus requisitos `AreTriggerConditionsMet()` se cumplen. Si es así, las envía al ejecutor.

### Implicaciones de Rendimiento y el "Punto Ciego" del Mazo
La decisión de iterar a mano es **muy segura a nivel lógico** (evita *Memory Leaks* causados por eventos C# no desuscritos y previene condiciones de carrera), pero tiene una restricción de diseño masiva:

> **El motor está programado para ignorar el Mazo (`deck`) y el Descarte (`discard`).**

Como iterar 80 cartas cada vez que hay daño destruiría los FPS (Fotogramas por Segundo) en dispositivos móviles, `BattleLogic.cs` literalmente no busca *Triggers* dentro de esas listas. 

#### 🛑 El caso práctico de la "Carta en el Mazo que Reacciona"
Imagina el siguiente escenario de Game Design: Creas una carta llamada *"Ira del Dragón"*. Su requisito es: *"Si esta carta está en tu mazo, y el jugador recibe Daño, haz 5 de daño al enemigo"*.

**A) Cómo actúa el Rogue Engine Actual (Iteración Activa):**
1. Recibes daño. 
2. El motor se dice a sí mismo: *"Voy a revisar las pasivas (`cards_power`) y las reliquias (`cards_item`) para ver si alguien tiene el trigger OnDamaged"*.
3. La carta *"Ira del Dragón"* está físicamente guardada en la lista `cards_deck`. 
4. Como el motor **nunca** hace un bucle `foreach` en la lista `cards_deck`, la carta nunca es leída. La carta jamás dispara su fuego. **El diseño falla.**

**B) Cómo actúa un Sistema Reactivo (Observer / Event Bus puro):**
En el sistema de Eventos suscritos (Que es la mecánica óptima que comentábamos y que usan juegos AAA combinada con Caché), el motor no itera buscando a las cartas. Son las cartas las que "se apuntan".
1. Al empezar el combate, la carta *"Ira del Dragón"*, que está en el mazo, se "Suscribe" al canal de eventos global: `EventManager.OnDamaged += DispararFuego`. (Se suscribe porque es su responsabilidad, no la del motor).
2. Recibes daño. El motor central no hace bucles. Solo grita un mensaje a ciegas: *"¡Me han hecho daño!"* (`EventManager.Invoke()`).
3. La carta *"Ira del Dragón"*, escondida en el fondo del mazo, oye el grito de la radio a la que estaba suscrita y se dispara a sí misma automáticamente. **El diseño funciona sin coste de rendimiento y sin importar en qué lista se encuentre la carta**.

---

## 2. El Sistema `ResolveQueue`: Determinismo Visual

Uno de los mayores retos en los juegos de cartas digitales (como *Slay the Spire* o *Hearthstone*) es la resolución simultánea. Si juegas una carta que hace daño en área, y mueren 3 enemigos a la vez, y sus muertes liberan veneno sobre ti... si todo ocurre en el mismo *milisegundo* del procesador, las animaciones se solapan y el jugador no entiende nada.

Para solucionar esto, todo el motor de habilidades pasa a través de la clase auxiliar **`ResolveQueue.cs`**.

### ¿Cómo funciona la Cola de Resolución?
`ResolveQueue` es, literalmente, una Cola FIFO (First In, First Out) estructurada con un *Object Pool* para no generar recolección de basura (*Garbage Collection*).

1. Cuando `BattleLogic` decide que la Habilidad A debe dispararse, no ejecuta el código del efecto (Ej: Curar).
2. En su lugar llama a: `resolve_queue.AddAbility(ability, caster, card, ResolveAbility)`.
3. Esto empaqueta la orden y la mete al final de la cola.
4. En el `Update()` de `BattleLogic`, se llama a `resolve_queue.ResolveAll(delay)`.
5. Esto pausa la ejecución del código `x` segundos (permitiendo que el motor de Unity haga las animaciones de ataque y partículas), y cuando el tiempo expira, saca el siguiente comando de la cola y lo ejecuta.

### Chaining (Encadenamiento de Habilidades)
El sistema permite crear cartas tremendamente complejas gracias a cómo se resuelve la cola:
En `BattleLogic.AfterAbilityResolved()`, el código mira si la habilidad actual (`AbilityData`) tenía configuradas "Habilidades en Cadena" (`chain_abilities`). Si es así, automáticamente hace un nuevo `TriggerAbility()` poniéndolas al final del tren de `ResolveQueue`.

---

## 3. `UpdateOngoing`: El Motor de Estadísticas Pasivas

A diferencia de los eventos explosivos (Daño, Robo, Curación), las reglas matemáticas pasivas (Ej: *"Esta carta cuesta 1 Maná menos por cada maldición en tu mano"*) las gestiona la función **`UpdateOngoing()`**.

Esta función es el eslabón más pesado del motor y **se invoca constantemente** (cada vez que juegas una carta, acaba un turno, o se resuelve una habilidad).

**Su flujo ineficiente pero seguro:**
1. Borra los bonus de stats de TODOS los personajes, cartas en mano y reliquias (`ClearOngoing`).
2. Recalcula a cero la armadura retrasada, mano extra y reducción de maná.
3. Itera a la fuerza bruta sobre TODAS las cartas de la mano del jugador, poderes y reliquias invocando `UpdateOngoingAbilities()`.
4. Si la carta tiene el trigger `AbilityTrigger.Ongoing`, evalúa matemáticamente el filtro de objetivos, y si acierta, aplica la modificación numérica (ej: `card.mana_cost -= 1`).

---

## 4. Conclusión y Futuro Refactor

`BattleLogic.cs` es un *Framework* funcional y exento de errores asíncronos gracias al determinismo absoluto de su **Iteración Centralizada** y la **Cola de Resolución**. 

**Debilidades de Arquitectura:**
* Carece de la escalabilidad O(1) de un Bus de Eventos puro (Observer Cacheado por diccionarios).
* El bucle constante de `UpdateOngoing()` puede volverse pesado si el jugador alcanza un estado de "Late Game" con decenas de reliquias y manos de 15 cartas de tamaño.
---

## 5. Anatomía de un Turno: El Código de `PlayCard()`

Para entender cómo las piezas explicadas arriba encajan en un caso real, vamos a trazar la ruta de código literal que ejecuta el motor en el momento exacto en el que sueltas el ratón para jugar una carta.

### Paso 1: Pago y Limpieza (`BattleLogic.PlayCard`)
El punto de entrada es la línea `557` de `BattleLogic.cs`.
1. **Validación y Cobro:** Primero comprueba si puedes pagarla (`CanPlayCard`) y luego resta tu energía (`owner.PayMana(card)`).
2. **Movimiento Físico:** Llama a `RemoveCardFromAllGroups()`. Quita la carta de tu mano. Si el `card_type` es `Power`, la mete en la lista `cards_power` permanente. Si es un ataque o hechizo, la manda directamente a la basura (`cards_discard`).
   * *Aviso:* La carta ya está en el descarte incluso antes de que dispare sus rayos o haga daño en pantalla.
3. **Registro:** Guarda en tu historial de acciones que has jugado esta carta (`AddHistory`).

### Paso 2: Evaluación Pasiva Inmediata (`UpdateOngoing`)
En la línea `587`, asombrosamente, antes de disparar el efecto de la carta, el motor llama a `UpdateOngoing()`. Refresca todos los bonus de stats matemáticos de la mesa por si haber soltado esta carta ha roto alguna sinergia global pasiva.

### Paso 3: Activación de los Triggers
En la línea `596` ocurre el disparo dual que vimos en el patrón *"Iteración Centralizada"*:
* `TriggerCardAbilityType(AbilityTrigger.OnPlay, card)`: Dispara el efecto activo de la propia carta que acabas de soltar.
* `TriggerCharacterAbilityType(AbilityTrigger.OnPlayOther, owner, card)`: Pregunta al resto de la mesa y reliquias: *"¿Alguien tiene algún efecto que salte cuando juego cartas?"* (Ej: Una reliquia que te cura 1 HP cada vez que juegas algo).

### Paso 4: De las Habilidades a la Cola (`ResolveQueue`)
El motor viaja a `TriggerAbility` (Línea `1043`). Descubre que los requisitos de la carta se cumplen, pero en vez de ejecutar la curación/ataque, empaqueta el evento de C# y lo lanza a la clase encoladora:
`resolve_queue.AddAbility(iability, caster, card, ResolveAbility)`

### Paso 5: Resolución Final y Filtro de Targets (`ResolveAbility`)
El reloj del `Update()` avanza, el delay gráfico termina y la cola escupe el comando, llamando por fin a la función real `ResolveAbility` (Línea `1065`).
1. Comprueba si el tipo de Target es abstracto (Ej: *"Elige una carta"*) llamando a `ResolveCardAbilitySelector()`. Si es así, congela el turno y saca una ventana de selección.
2. Si el Target era el monstruo que clickaste, llama a `ResolveCardAbilityPlayTarget()`.
3. Esta función verifica las condiciones finales (`AreTargetConditionsMet`) y... ¡Bam! Ejecuta `ResolveEffectTarget`, que resta los puntos de vida.
4. Finalmente, llama a `AfterAbilityResolved`, recalcula `UpdateOngoing` otra vez para ver si alguien murió y procesa los `chain_abilities` (Combos en cadena).
