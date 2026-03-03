# Guía de Refactorización Activa: `BattleLogic.cs` (Hacia el Event Bus O(1))

Este documento es una transcripción técnica viva de nuestra refactorización paso a paso del núcleo de Rogue Engine. Su objetivo es inmortalizar no solo *qué* código hemos cambiado, sino los **Patrones de Diseño** y el **Game Design** que justifican cada alteración.

---

## 1. El Problema Original: La Iteración Ciega O(N)

En el motor `BattleLogic_Legacy.cs` original, el sistema de eventos ("Triggers") funcionaba mediante un patrón de **Iteración Centralizada**.

Cuando ocurría un evento en el juego (por ejemplo, jugar una carta genérica como "Golpe de Espada" que no tiene sinergias), el motor llamaba a la función `TriggerCharacterAbilityType()`. El problema es que esta función estaba obligada a lanzar bucles `foreach` de "fuerza bruta" para preguntar a cada entidad del tablero si reaccionaba a la acción de "jugar carta".

### Limitación Técnica y de Rendimiento:
1.  **Iteraciones inútiles:** Si tenías 5 auras (`cards_power`) y 10 reliquias (`cards_item`), el motor lanzaba 15 bucles internos leyendo la memoria de todas las habilidades para comprobar si su `AbilityTrigger` coincidía. Si ninguna carta tenía sinergia, los milisegundos de CPU se tiraban a la basura.
2.  **El Muro de Diseño (Cartas en el Mazo):** Debido a que iterar es tan costoso computacionalmente, el creador original se vio forzado a **no iterar** jamás sobre tu Mazo oculto, ni sobre tu Mano, ni sobre tu Descarte para buscar *Triggers*.
    -   *Efecto de Game Design:* En la versión original de Rogue Engine es imposible crear mecánicas modernas como *"Mientras esta carta esté en tu mazo, recibes 1 menos de daño global"*, porque el motor físico del combate es ciego a esas tres zonas para ahorrar rendimiento.

---

## 2. La Solución Arquitectónica: El Caché de Diccionario (Event Bus)

Para destruir los cuellos de botella y permitir que los mazos, descartes y manos reaccionen al combate, implementamos un Caché basado en `Dictionary`.

**El Concepto de la Agenda Postal:**
En lugar de iterar por las cartas cada vez que alguien sufre daño, creamo un Índice matemático exacto al arrancar el combate.
-   **Key (Llave):** El Enum del trigger (`AbilityTrigger.OnDamaged`).
-   **Value (Valor):** Una `List<Card>` que contiene única y exclusivamente las direcciones de memoria de las cartas que tienen esa habilidad precisa.

Cuando ocurre el daño, en lugar de gritar "¡QUIÉN REACCIONA!" a todo el tablero, el sistema consulta su agenda interna (`trigger_cache[OnDamaged]`) y dispara las funciones directamente a las cartas apuntadas ahí, abortando instantáneamente en 1 milisegundo si la lista está vacía (O(1)).

### Código Implementado (Paso 1 y 2): Inicialización y Suscripción

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RogueEngine.Gameplay
{
    public class BattleLogic
    {
        // 1. NUESTRA CACHÉ: El corazón del nuevo sistema Event Bus
        private Dictionary<AbilityTrigger, List<Card>> trigger_cache = new Dictionary<AbilityTrigger, List<Card>>();

        private World world_data;
        private Battle battle_data;
        private ResolveQueue resolve_queue;

        public BattleLogic(bool is_instant = false)
        {
            resolve_queue = new ResolveQueue(is_instant);
            InitCache(); 
        }

        // 2. PREPARACIÓN DE LA AGENDA
        private void InitCache()
        {
            trigger_cache.Clear();
            
            // Crea una lista en blanco por cada Trigger en el juego.
            foreach (AbilityTrigger trigger in Enum.GetValues(typeof(AbilityTrigger)))
            {
                trigger_cache[trigger] = new List<Card>();
            }
        }

        // 3. LA SUSCRIPCIÓN OBLIGATORIA
        public void RegisterCardToCache(Card card)
        {
            if (card == null) return;
            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability != null && !trigger_cache[ability.trigger].Contains(card))
                    trigger_cache[ability.trigger].Add(card);
            }
        }

        // 4. EVITAR MEMORY LEAKS
        public void UnregisterCardFromCache(Card card)
        {
            if (card == null) return;
            foreach (AbilityData ability in card.GetAbilities())
            {
                if (ability != null && trigger_cache[ability.trigger].Contains(card))
                    trigger_cache[ability.trigger].Remove(card);
            }
        }
    }
}
```

---

## 3. Demolición de la Iteración: El Nuevo Disparador (`FireTrigger`)

Sustituyendo a las kilométricas y costosas funciones `TriggerCharacterAbilityType()` y `TriggerCardAbilityType()`, hemos programado una función unificada y matemática estricta que aniquila la necesidad de usar bucles `foreach` de búsqueda ciegos.

### Código Implementado (Paso 3):

```csharp
        // 5. EL NUEVO DISPARADOR UNIVERSAL (Costo Computacional Constante O(1))
        public void FireTrigger(AbilityTrigger triggerType, BattleCharacter caster, Card triggerer = null)
        {
            // Verificación de viabilidad del personaje origen
            if (caster == null || caster.IsDead() || !caster.CanTriggerAbilities()) return; 

            // Rendimiento mágico: Si nadie en todo el juego reacciona a este fuego, salimos en 1 milisegundo.
            if (!trigger_cache.ContainsKey(triggerType) || trigger_cache[triggerType].Count == 0)
                return; 

            // Hacemos una copia de protección ".ToList()" para prevenir 'Collection Was Modified Exceptions'
            // dado que una habilidad reactiva podría destruirse a sí misma y darse de baja de la caché a mitad lectura.
            List<Card> subscribers = trigger_cache[triggerType].ToList();

            foreach (Card card in subscribers)
            {
                // El filtro nativo asume el mismo propietario
                if (card.owner_uid == caster.uid) 
                {
                    foreach (AbilityData ability in card.GetAbilities())
                    {
                        if (ability != null && ability.trigger == triggerType)
                        {
                            // TriggerAbility (Herencia Legacy) filtrará si la vida < 50, etc.
                            TriggerAbility(ability, caster, card, triggerer);
                        }
                    }
                }
            }
        }
```

---

## 4. Discusión de Arquitectura: ¿Por qué mantener `UnityAction` en lugar de `System.Action`?
(Explicado en debate: Mantenemos la pureza nativa y la retrocompatibilidad visual de los scripts ya existentes en Rogue Engine que escuchan a esta variable delegada).

---

## 5. Profundidad: Las Funciones de Inicialización (Línea por Línea)

Para que el nuevo motor pueda arrancar, hemos portado las funciones de "Reparto de Cartas" del motor antiguo sin cambiar su matemática básica.

### 5.1 `AddEnemy()`
Instancia a un monstruo de la base de datos y le da vida física en el tablero.

```csharp
protected virtual void AddEnemy(CharacterData enemy, int pos_x, int level)
{
    // 1. Prevención básica de nulos
    if (enemy != null)
    {
        // 2. BattleCharacter.Create() es una "Fábrica" que coge los stats base del monstruo
        // y le aplica los multiplicadores de nivel (vida extra, daño extra de escalado).
        BattleCharacter character = BattleCharacter.Create(enemy, level);
        
        // 3. Le asigna su posición física (La primera fila enemiga o la trasera).
        character.slot = GetEnemyStartPos(pos_x);
        
        // 4. Lo guarda en la lista oficial de peleadores vivos de esta batalla.
        battle_data.characters.Add(character);

        // 5. Modificador global: Ajusta la dificultad si estás en un Modo Difícil (tipo Ascension).
        ScenarioData scenario = ScenarioData.Get(world_data.scenario_id);
        character.SetScenarioDifficulty(scenario);

        // 6. Llama a la funcion SetCharacterCards() para crearle su mini-mazo al monstruo.
        SetCharacterCards(character);
    }
}
```

### 5.2 `SetChampionCards()`
El código responsable de separar lo que es una "Carta a Robar" de una "Sinergia Pasiva" durante el inicio del combate.

```csharp
public virtual void SetChampionCards(Champion champion, BattleCharacter character)
{
    // 1. Limpieza absoluta por si el personaje viene manchado de la memoria RAM de batallas anteriores.
    character.cards_deck.Clear();
    character.cards_discard.Clear();
    character.cards_hand.Clear();

    // 2. CREANDO EL MAZO
    foreach (ChampionCard ccard in champion.cards)
    {
        // Busca en la "Base de Datos" de Unity la tarjeta de identificación teórica de esta carta.
        CardData icard = CardData.Get(ccard.card_id);
        if (icard != null)
        {
            // Instancia la carta real. Le pasa el Nivel (para calcular escalados de daño) 
            // y la vincula a este campeón como su dueño legítimo.
            Card card = Card.Create(icard, ccard.level, character, ccard.uid);
            // La guarda en el mazo boca abajo.
            character.cards_deck.Add(card);
        }
    }

    // 3. EQUIPANDO PASIVAS (Reliquias)
    // El motor procesa los inventarios en 2 pasadas (primero se equipa el acero, luego los consumibles).
    foreach (ChampionItem item in champion.inventory)
    {
        CardData iitem = CardData.Get(item.card_id);
        
        // Si el objeto es una Reliquia Pasiva (Ej: Un Anillo de Fuego permanente)
        if (iitem != null && iitem.item_type == ItemType.ItemPassive)
        {
            // Se crea como carta nivel 1 instanciada.
            Card card = Card.Create(iitem, 1, character, GameTool.GenerateRandomID());
            // Y de forma drástica, NO ENTRA AL MAZO. Entra directo a cards_item (la zona de auras/reliquias).
            character.cards_item.Add(card); 
        }
        
        // Ojo: Existen items extraños que no son pasivas absolutas, sino que introducen Cartas 
        // reales temporales a tu mazo mientras los tengas equipados.
        if (iitem != null && iitem.item_type == ItemType.ItemCard)
        {
            Card card = Card.Create(iitem, 1, character, GameTool.GenerateRandomID());
            character.cards_deck.Add(card); // Al mazo para poder ser robada.
        }
    }

    // 4. EQUIPANDO CONSUMIBLES (Pociones)
    foreach (ChampionItem item in champion.inventory)
    {
        CardData iitem = CardData.Get(item.card_id);
        if (iitem != null && iitem.item_type == ItemType.ItemConsumable)
        {
            Card card = Card.Create(iitem, 1, character, GameTool.GenerateRandomID());
            character.cards_item.Add(card); 
        }
    }

    // 5. El toque de gracia: Barajar el array de forma aleatoria matemática.
    ShuffleDeck(character.cards_deck);
}
```

---

## 6. Los Turnos y la Limpieza del O(N)

El hito de esta fase de refactorización ha sido portar el núcleo numérico de los Turnos (`StartTurn` y `EndTurn`) y extirpar de ellos el viejo sistema de disparos.

### 6.1 `StartTurn()`: La Velocidad de O(1) en Acción
En el Legacy, al arrancar el turno, el motor forzaba una lectura de absolutamente todas las variables de todos los personajes para ver si alguno reaccionaba a `StartOfTurn` (por ejemplo, veneno o auras curativas).
Ahora, nuestra función porta la matemática del escudo/energía, pero llama a la agenda postal.

```csharp
public virtual void StartTurn()
{
    // ... [Se calcula Maná, se limpia el Escudo Viejo (Armor)] ...
    
    // 1. Daño fijo incorporado
    // El veneno y quemadura son tan comunes y rápidos matemáticamente que se dejan fuera 
    // del sistema de cartas reactivas para evitar sobrecargar el ResolveQueue con cálculos obvios.
    if (character.HasStatus(StatusEffect.Poisoned))
    {
        int val = character.GetStatusValue(StatusEffect.Poisoned);
        DamageCharacter(character, val, true); // True ignora el escudo
    }

    // ... [Refrescar Interfaz y Robar Mano] ...

    // 2. ¡NUESTRO CÓDIGO REFRACTORIZADO O(1)!
    // En el Legacy, aquí había un 'TriggerCharacterAbilityType'. Significaba iterar ciega y masivamente.
    // Ahora, entramos directamente a la Casa Postal de "StartOfTurn" de nuestro Diccionario. 
    // Si la casa está vacía, no bajamos ni 1 FPS.
    if (battle_data.turn_count == 1)
        FireTrigger(AbilityTrigger.BattleStart, character); // Disparo Relámpago

    FireTrigger(AbilityTrigger.StartOfTurn, character); // Disparo Relámpago

    // Empieza la cascada visual (0.2s para que veas robar las cartas antes de la Fase Principal)
    resolve_queue.AddCallback(StartMainPhase);
    resolve_queue.ResolveAll(0.2f);
}
```

### 6.2 El Monstruo Incomprendido: `UpdateOngoing()`
Junto a los turnos, hemos portado en bloque 5 funciones llamadas `UpdateOngoing` y `AddOngoingStatusBonus` usando el patrón arquitectónico de **Sobrecarga de Métodos (Overloading)**.

**¿Por qué copiarlas tal cual si son iteraciones inmensas?**
Porque `Ongoing` es la excepción matemática. A diferencia de un evento de Daño (Que ocurre en un fotograma específico y pasa al pasado), las habilidades Ongoing ("A partir de ahora, todas tus espadas pegan +2 extra") alteran las estadísticas base del motor constantemente.

Si robas una carta, si un aliado muere, o si te pones veneno, el motor tiene que asegurarse de que ningún Aura se ha apagado. 
Por tanto, `UpdateOngoing` "resetea a 0" las estadísticas extra de todo el universo (`ClearOngoing()`), e inmediatamente vuelve a iterar por los tableros contando cuántas Auras brillantes hay vivas en la mesa y repone los bonificadores +2 a las cartas correspondientes. Este proceso requiere que Unity analice todo el escenario `O(N)`, por lo que no puede ser puesto en Caché.

---

## 4. Discusión de Arquitectura: ¿Por qué mantener `UnityAction` en lugar de `System.Action`?

Durante la refactorización profesional de un motor de cartas, surge una duda arquitectónica vital planteada a menudo por desarrolladores Senior: **¿Deberíamos usar `System.Action` (C# nativo pura) en lugar de `UnityAction` para desacoplar la lógica de la Interfaz Visual (UI)?**

**La respuesta técnica es que en el código de *Rogue Engine*, ya estamos usando exactamente lo mismo que un `System.Action`.** 

Para entender por qué el consejo genérico ("No uses UnityEvents para lógica compleja") es correcto pero aquí no aplica, hay que diferenciar tres cosas:

1.  **`UnityEvent`:** Es la clase pesada que se expone en el Inspector de Unity (las cajillas donde arrastras objetos en la interfaz visual). Es lenta, es serializada, y es infame por crear *"código espagueti in-debuggeable"*. **Totalmente desaconsejada para lógica de combate hiper-rápida (como un Roguelike).**
2.  **`System.Action`:** Es el delegado nativo de C#. Rápido, puro, y definido estricta y únicamente por código puro. Es el estándar de la industria.
3.  **`UnityAction`:** Es literalmente un "alias" o disfraz (`namespace UnityEngine.Events { public delegate void UnityAction(); }`) que Unity creó para las funciones vacías. A nivel de memoria y compilación, una variable `UnityAction` escrita plana en un script funciona **exactamente igual de rápida y asilada que un `System.Action`**.

### El veredicto para *Rogue Engine*
El creador original definió en `BattleLogic_Legacy.cs` variables como `public UnityAction onBattleStart;`. Estas variables **no son `UnityEvents`**. No aparecen en el Inspector. Todos los scripts de Interfaz de Usuario (UI) se suscriben a ellas mediante código duro (Ejemplo literal del código original: `BattleLogic.onBattleStart += MenuAparecer`).

Si hoy cambiáramos la palabra `UnityAction` por `System.Action` en nuestra nueva versión del motor:
- No ganaríamos ni un solo milisegundo de rendimiento.
- No mejoraríamos la limpieza de la arquitectura (el desacoplamiento total ya existe).
- Y el precio a pagar sería que **destrozaríamos todos los scripts visuales del juego** (`BattleUI.cs`, `PlayerUI.cs`, etc.) porque ellos dejarían de entender el tipo de variable.

Por tanto, la decisión arquitectónica es **mantener `UnityAction`** por razones de compatibilidad estricta con el entorno visual y evitar re-escrituras masivas sin beneficio real computacional. La pieza que suple a *UniTask/Awaitables* para animar las cartas en cascada está implementada mediante `ResolveQueue.cs` (Un despachador de cola a medida).
*(Nota: El código aún requiere portar `TriggerAbility(...)` desde el Legacy para estar libre de errores de sintaxis rojos en Visual Studio).*

---

## 8. Matemáticas de Combate y Resolución Numérica

El bloque final crítico de un motor de físicas de cartas reside en dos áreas: **El Sistema de Daño/Vida** y **El Despachador de Habilidades**. Al portarlos desde el código Legacy, hemos purgado las últimas iteraciones redundantes.

### 8.1 Físicas de Daño (`DamageCharacter`, `KillCharacter`)
El sistema de combate está altamente acoplado a los `StatusEffect` (Estados alterados de las cartas). 
Cuando llamas a `DamageCharacter(attacker, target, value)`, la arquitectura no resta vida inmediatamente. Primero, pasa por un **Filtro de Multiplicadores Puros**:
1.  **Multiplicadores del Atacante (`factor_self`)**: Comprueba estados como `Courageous` (+50% daño) o `Fearful` (-50%).
2.  **Multiplicadores del Defensor (`factor_other`)**: Comprueba `Vulnerable` (+50% daño recibido) o `Evasive` (-50%).
3.  **Reducción de Escudo**: Una vez calculado el daño final, el juego ataca a la variable `target.shield`. Solo la diferencia matemática restante afectará al `target.damage` (Puntos de Vida).
4.  **Rebote (Thorns)**: Inmediatamente después, si el defensor tenía la estadística `Thorn`, la función lanza recursivamente un ataque `DamageCharacter` en dirección inversa hacia el `attacker`.

La refactorización masiva de rendimiento en esta sección se ejecutó al eliminar todos los antiguos contadores ciegos y enganchar la matemática directamente a nuestra Caché Inmediata:
```csharp
// Al final de DamageCharacter() y KillCharacter()
FireTrigger(AbilityTrigger.OnDamaged, target); 
FireTrigger(AbilityTrigger.OnDeath, character);
```

### 8.2 El Despachador Electrónico (`TriggerAbility` y Sub-funciones)
A partir de la línea 1000 del archivo original nos encontramos con la pared de funciones `ResolveCardAbility...`. 

**¿Por qué hay 15 funciones distintas si "hacen lo mismo"?**
Aquí brilla el diseño del Arquitecto Original de Rogue Engine. Una habilidad de carta en su base de datos (ScriptableObject) tiene un Enum `AbilityTarget` que dicta quién sufre el efecto (Ej: `SelectTarget`, `AllCharacters`, `CardSelf`).

Para no crear un código de 5000 líneas ilegible con `if/else` masivos anidados, el autor dividió la lógica. Cuando tú mandas que una Habilidad salte:

1.  El motor entra en `ResolveAbility(AbilityData iability, ...)`.
2.  Inmediatamente delega la validación de selección a `ResolveCardAbilitySelector`. (Si querías seleccionar un monstruo a mano con el ratón, el motor pausa aquí y espera tu Input).
3.  Si no era un selector manual, lanza una batería automática de funciones ciegas (`ResolveCardAbilityCharacters`, `ResolveCardAbilityCards`, `ResolveCardAbilitySlots`...). **Cada una** se encarga exclusivamente de preguntar a la Base de Datos la lista matemática pura de los objetivos que casan con ese filtro.
4.  Si coinciden, llaman a su sub-empleado final: `ResolveEffectTarget`, el cual coge al objetivo (Personaje, Carta o Bloque del campo de batalla) y finalmente le aplica las Partículas y la Animación.

**El Vínculo con nuestro Motor de Eventos:**
Nuestra nueva arquitectura basada en `FireTrigger` es simplemente la "recepcionista VIP". Una vez averigua instantáneamente en `0.00ms` qué reliquias o atributos pasivos deben saltar en este fotograma exacto... simplemente las enjaula y las mete de golpe en la cinta transportadora del despachador:
```csharp
// La función que vincula nuestro Caché con el Core Numérico
public virtual void TriggerAbility(AbilityData iability, BattleCharacter caster, Card card)
{
    if (iability.AreTriggerConditionsMet(battle_data, caster, card))
    {
        resolve_queue.AddAbility(iability, caster, card, ResolveAbility); // Añadir a la cola!
    }
}
```

---

## 9. Registro de Debugging: Bugfixes de la Arquitectura Event Bus

Durante la implementación final de la arquitectura O(1), nos enfrentamos a tres errores (bugs) críticos interconectados que bloqueaban el flujo normal del juego. A continuación se documenta el proceso de diagnóstico y resolución de cada uno, ya que representan lecciones valiosas sobre el acoplamiento de Game Design y Sistemas de Caché.

### Bug 1: El Turno Bloqueado y la Mano Vacía
**El Síntoma:** Al darle a Play, los personajes aparecían en escena, pero el turno no comenzaba. Nunca se llegaba a la Fase Principal y el jugador no robaba sus cartas iniciales. El juego se quedaba congelado en un estado transitorio.
**El Diagnóstico:** La función `StartTurn()` no estaba fallando directamente, pero estaba bloqueada matemáticamente. Descubrimos que la refactorización había omitido copiar las funciones `CalculateInitiatives()`, `RemoveFromInitiativeCurrent()` y `RemoveFromInitiativeNext()`.
**La Raíz Físico-Matemática:** En Rogue Engine, el paso de turnos está dictaminado por un array de Iniciativas (`battle_data.initiatives`). Como el array estaba vacío porque nadie lo calculaba, al llegar a la instrucción `battle_data.GetFirstInitiative()`, el motor devolvía nulo y abortaba la función silenciosamente antes de llegar a la orden `DrawHand()`.
**La Solución:** Restituir las funciones de cálculo matemático de Iniciativa en el archivo `BattleLogic.cs` para revivir el motor de colas de turno.

### Bug 2: El Bucle Infinito de Efectos Visuales (El Perro Mágico)
**El Síntoma:** Una vez reparada la iniciativa y al arrancar el combate, sobre el personaje principal (el Perro) comenzaban a activarse en bucle docenas de efectos visuales sin sentido, ralentizando el Engine y bloqueando de nuevo el `ResolveQueue`.
**El Diagnóstico:** Al analizar la pila de llamadas, observamos que todos esos efectos provenían de un único evento inicial: `FireTrigger(OnPlay)`.
**El Error de Arquitectura:** En la función `StartBattle`, cometimos el error de suscribir a la nueva Caché de Eventos **todas las cartas del mazo** (`cards_deck`). 
-   *¿Por qué falló?* La arquitectura clásica asume que las cartas en el mazo están "dormidas". Nosotros las dimos de alta en el Event Bus. Cuando el motor disparó un inofensivo `OnPlay` al inicio, el Mega-Diccionario activó literalmente todos los ataques y hechizos de las decenas de cartas escondidas en el mazo de forma simultánea.
**La Solución:** Extirpar `cards_deck` de la fase de Suscripción Temprana (`RegisterCardToCache`). Únicamente las Reliquias (`cards_item`) o Poderes (`cards_power`) deben residir en la caché global.

### Bug 3: Ataques que disparan Pociones (Pérdida de Identidad)
**El Síntoma:** Ya con el mazo domado, al robar mano e intentar usar una carta genérica (como "Mordisco"), en vez de atacar, saltaba curiosamente el efecto de una Poción guardada en el inventario.
**El Diagnóstico:** El problema residía en cómo se refactorizó la función `PlayCard`. Sustituimos la llamada unívoca a la carta (`TriggerCardAbilityType`) por un cañonazo indiscriminado a la caché global (`FireTrigger(OnPlay)`).
**El Efecto Mariposa:** 
1. Las cartas de la mano NO están en Caché (Corregido en Bug 2). 
2. Al jugar la carta, `FireTrigger` preguntaba a la Caché quién reaccionaba a un `OnPlay`.
3. Una **Poción del inventario SÍ estaba inscrita** bajo `OnPlay`.
4. El motor activaba la poción, ignorando el Ataque jugado.
**La Solución 3A:** Restaurar la función focalizada `TriggerCardAbilityType`, que opera solo sobre la carta sin dialogar con la Caché. Aplicarla en `PlayCard` y `UseItem`.
**La Solución 3B:** En `StartBattle`, añadir un filtro estricto para matricular sólo Items Pasivos (`ItemPassive`), dejando que los consumibles genéricos dependan 100% de la lógica manual del jugador.

---

*(Continuará según progresemos en el código...)*
