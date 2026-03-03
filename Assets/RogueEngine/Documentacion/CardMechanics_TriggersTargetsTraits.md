# Diseccionando la Lógica Interna del Engine: Triggers, Targets y Traits

Es normal que términos como `Traits` o `Triggers` parezcan abstractos al principio. Para entender por qué el desarrollador del *Rogue Engine* los diseñó así, he analizado directamente el código fuente del motor (específicamente *AbilityData.cs* y *TraitData.cs*). 

A continuación te explico la mentalidad arquitectónica exacta detrás de cada uno y cómo puedes adaptarlos para tu juego.

---

## 1. Triggers (Cuándo ocurre la magia)

En programación de cartas, un *Trigger* (Desencadenante o Disparador) es la respuesta a la pregunta: **"¿En qué momento exacto del tiempo debo ejecutar esta habilidad?"**

En lugar de que el programador tenga que escribir código para cada posible evento del juego, el *Rogue Engine* centraliza el tiempo en un menú desplegable (`Enum AbilityTrigger`).

*   **`OnPlay`:** Es el más común. La habilidad ocurre en el milisegundo exacto en el que sueltas el clic del ratón habiendo arrastrado la carta desde tu mano hasta el tablero.
*   **`StartOfTurn` / `EndOfTurn`:** La carta brilla y ejecuta su efecto sola cuando pulsas el botón "End Turn" (ideal para cartas tipo veneno o reliquias pasivas).
*   **`OnDraw`:** Magia negra. El efecto ocurre *en el momento en que la robas del mazo*, antes siquiera de que decidas si quieres jugarla o no. (Por ejemplo, una "Carta Maldita" que te hace 1 punto de daño nada más robarla).
*   **`OnDeath` / `OnDamaged`:** Efectos Reactivos. Solo se disparan si la entidad que posee esta habilidad muere o recibe una bofetada.

**💡 Cómo adaptarlo a tu Game Design:** Si quieres crear una carta trampa o un encantamiento que el jugador se equipa y que salta mágicamente cuando un enemigo le ataca, no necesitas a un programador. Simplemente creas una `AbilityData`, le pones el efecto de Daño, y le cambias el Trigger a `OnDamaged`. 

---

## 2. Targets (A quién o qué le pego)

Si el *Trigger* define el *Cuándo*, el *Target* (Objetivo) define el **"A quién va dirigida la bala"**.

El motor del juego es tremendamente flexible aquí, porque no solo permite apuntar a "Enemigos", permite apuntar a conceptos abstractos del tablero. He extraído las dianas exactas del código:

*   **`SelectTarget`:** El juego pausa, saca una flecha roja desde la carta y te obliga a hacer clic con el ratón sobre un Enemigo o Héroe físico en la pantalla.
*   **`CharacterSelf`:** Egoísta. El efecto te impacta a ti mismo (Curaciones, Buffos de escudo).
*   **`AllCharacters`:** Bombardeo general (AoE). Hace un *for-loop* a toda la vida de la pantalla.
*   **`AllCardsHand`:** Magia pura de cartas. El objetivo de la habilidad no es un personaje con vida, ¡sino otras cartas! Útil para robar una carta que diga: "Mejora +1 todas las cartas que sostienes en tu mano ahora mismo".
*   **Dianas Temporales (`LastPlayed`, `LastDestroyed`):** El motor tiene "memoria". Puedes crear una carta nigromante y ponerle de *Target* `LastDestroyed` (El último que murió). La carta resucitará matemáticamente al último muerto sin que tengas que programar quién fue.

**💡 Cuidado de Diseño:** Nunca mezcles agua con aceite. Si creas una Habilidad con Efecto `Damage` (Daño a puntos de vida), no puedes ponerle un Target de `AllCardsHand` (Todas las cartas de la mano). ¡Las cartas no tienen puntos de vida! Dará un error lógico.

---

## 3. Traits (Los Rasgos Estáticos y la Semántica)

Aquí es donde el Engine se pone interesante. He leído el `TraitData.cs` y, a diferencia de las Habilidades (que tienen Triggers y Targets complejos de código activo), un *Trait* **apenas tiene código**. Funciona estrictamente como un sistema de **Etiquetas (Tags) Semánticas** ("Pegatinas") para que *otras* partes del juego puedan leer e interactuar. La carta por sí misma no hace nada solo por tener un Trait determinado; necesita que otro sistema o carta reaccione a esa pegatina.

Podemos dividir los Traits en dos grandes grupos de diseño:

### A. Pegatinas Estructurales (Reglas de Motor)
Son Traits que los sistemas subyacentes del juego (el loop de la partida) buscan de forma automática para aplicar mecánicas base.
*   **Ejemplos:** `Exhaust` (Consumible), `Ethereal` (Etérea), `Innate` (Innata).
*   **Cómo funcionan:** El motor de turnos, al terminar tu turno dice: *"A ver, voy a descartar la mano. ¿Alguna carta de la mano tiene la pegatina `Ethereal`? Entonces a tí te destruyo en vez de descartarte"*.

### B. Pegatinas Narrativas / de Sinergia (Reglas de Diseño)
Sirven exclusivamente para que **tú como Game Designer** construyas combos y filtren objetivos, sin que el motor core tenga reglas programadas sobre ellos obligatoriamente.
*   **Ejemplos:** `Gunpowder` (Pólvora), `DragonSkin` (Piel de Dragón), `Demon` (Demonio).
*   **Cómo funcionan:** Si le pones el Trait `Gunpowder` a una carta, no ocurrirá nada automático. Pero luego puedes crear una Reliquia que diga: *"Cada vez que juegues una carta con la pegatina `Gunpowder`, aplica 2 de Quemadura"*.

#### ¿El juego se lía al buscar entre tantas pegatinas? (Ej: Robar algo Narrative del Enemigo)
**No, nunca.** A la hora de buscar cartas, el motor no escanea todo el universo del juego a lo loco. Sigue un embudo de **Filtros Combinados** y **Targets (Alcance)**:
1.  **Scope (Ubicación Primaria):** Si tu carta roba o busca ataques, su propiedad `Target` primero limitará la búsqueda física: `Tu Mazo`, `Tu Mano` o `Pila de Descartes`. Las cartas con Traits narrativos propias de los enemigos (ej: los ataques del Dragón) no existen en las listas de tu mazo, por lo que el motor ni las llega a mirar.
2.  **Condiciones AND:** La búsqueda interna es restrictiva. Busca iterativamente: `(Propietario == Jugador) AND (Ubicación == Mazo) AND (HasTrait == Ataque)`. Esta arquitectura permite poner miles de Traits exóticos sin miedo a "entrecruzar" cartas accidentalmente.

---

## 4. `card_type` vs `Traits`: ¿Por qué existen los dos si categorizan?

Si los Traits y los `card_type` (Ataque, Skill, Power...) sirven para catalogar, ¿por qué molestarse en programar ambos? La clave está en la diferencia arquitectónica entre **Rendimiento/Rigidez** y **Flexibilidad**.

### `card_type` (El ADN Inmutable)
*   **Estructura Técnica:** Es una simple variable directa (`Enum` en C#) incrustada en el código base del Asset de la carta.
*   **Propósito:** Define las reglas inquebrantables del juego y la Interfaz Gráfica (UI).
*   **Rendimiento:** Escanearlo tiene **coste cero** de memoria.
*   **Ejemplo:** El código visual dice *"Si el `card_type` es Ataque, pinta la carta con un marco de color Rojo"*. O el controlador de turnos dice *"Si el jugador tiene el estado Silencio, no le permitas jugar ninguna carta cuyo `card_type` sea Skill"*. Cosas que deben ocurrir cada milisegundo sin ralentizar Unity.

### `Traits` (El Equipaje Modificable)
*   **Estructura Técnica:** Es un *array* o lista dinámica de archivos vinculados (`List<TraitData>`).
*   **Propósito:** Define mecánicas exóticas y combos complejos creados por el diseñador.
*   **Rendimiento:** Para encontrar una pegatina, el motor tiene que hacer un bucle (*For loop*) a través de todo el equipaje de la carta hasta dar con la correcta. Es más lento para el procesador (imperceptible para el jugador, pero distinto computacionalmente).
*   **Ejemplo:** Un combo que te da +1 de Vida por cada carta jugada con el trait `Agua`.

**La Regla de Oro:** 
Usa **`card_type`** como el "molde estructural y visual" de la carta que aplican restricciones globales, y usa **`Traits`** como accesorios dinámicos para construir mecánicas, sinergias e inventarte nuevos combos de Game Design.

---

## 5. El Patrón Observer: Cómo escuchan las Cartas a los Triggers

Es natural pensar: *"Si los triggers son momentos del juego (jugar carta, robar, recibir daño)... ¿el motor hace un bucle enorme preguntando a TODAS las cartas de mi mazo, pila de descartes y mano cada vez que recibo un golpe?"*

**No. Eso destruiría el rendimiento.** Los juegos de cartas profesionales (y arquitecturas como *Rogue Engine*) utilizan un patrón de diseño llamado **Observer (Event System o Bus de Eventos)**.

### Cómo funciona realmente (El "Tablón de Anuncios")
Imagina el motor del juego como un gran "Tablón de Anuncios" central.

1.  **La Suscripción (Listener):** Cuando robas una carta y entra físicamente a tu **Mano**, la carta revisa sus propias `Abilities`. Si ve que tiene una habilidad con el Trigger `OnDamaged` (Al recibir daño), la carta levanta la mano y le dice al Controlador central: *"Oye Tablón de Anuncios, anota mi nombre. Si el jugador recibe daño mientras estoy aquí, avísame"*.
2.  **El Evento (El Grito):** El enemigo te ataca. El sistema de batalla resta 5 HP y, en ese mismo microsegundo, grita al Tablón de Anuncios: **`¡EVENTO: OnDamaged EJECUTADO!`**.
3.  **La Reacción (Callback):** El Tablón de Anuncios no mira las 80 cartas de tu mazo. Solo mira su pequeña lista de "Suscritos a OnDamaged" y ejecuta directamente el efecto de esa carta específica que estaba en tu mano esperando.
4.  **Des-suscripción:** Si juegas esa carta o la descartas, la carta le dice al Tablón: *"Bórrame de la lista de OnDamaged, ya no estoy en la Mano"*.

### ¿Por qué es brillante esta arquitectura?
*   **Rendimiento absoluto:** Cuando ocurre un evento pasivo (como `OnEndOfTurn`), el juego no gasta memoria iterando. Solo avisa a las 2 o 3 cartas/reliquias que se habían apuntado previamente a la lista de ese evento.
*   **Desacoplamiento:** El monstruo que te ataca no tiene ni idea de qué cartas o escudos tienes interactuando. Él solo lanza el grito `OnDamaged` al vacío, y el Event System se encarga de despertar a las habilidades que corresponda.
*   **Las Reliquias (Artefactos):** Esta misma arquitectura exacta es la que usan los "Artefactos" o "Reliquias". Cuando coges una reliquia, esta se "suscribe" permanentemente a un Trigger de la partida hasta que mueres o ganas.

---

## 6. La Implementación Real en Rogue Engine (Iteración Centralizada)

En el punto anterior te expliqué cómo se diseña un **Event System puro** (El Patrón Observer tradicional con suscripciones). Sin embargo, es vital que sepas que **el creador de Rogue Engine tomó una ruta ligeramente distinta por diseño arquitectónico**.

En lugar de usar suscripciones puras de C# (delegados `Action`), el Rogue Engine utiliza lo que podríamos llamar **"Iteración Centralizada en el Controlador"**.

Esto significa que cuando ocurre un evento, el script central (`BattleLogic.cs`) detiene el juego, coge la lupa y busca a mano quién debería reaccionar. 

A continuación tienes el código real del Asset extraído de `BattleLogic.cs`:

### El Disparador Centralizado
Cuando ocurre algo en el juego, como jugar una carta o alguien muriendo, no se dispara un Evento de C#, sino que se llama a una función centralizada de Triggers:

```csharp
// Extraído de BattleLogic.cs (Línea ~992)
// El propio motor recibe el aviso: "Oye, ha ocurrido este Trigger (ej: OnDamaged)"
public virtual void TriggerCharacterAbilityType(AbilityTrigger type, BattleCharacter caster, Card triggerer = null)
{
    if (!caster.CanTriggerAbilities()) return;

    // 1. Revisa las habilidades innatas del personaje
    foreach (AbilityData iability in caster.GetAbilities()) {
        if (iability && iability.trigger == type)
            TriggerAbility(iability, caster, triggerer, triggerer);
    }

    // 2. Revisa sus cartas de Poder activo (Cartas en juego)
    foreach (Card acard in caster.cards_power) {
        foreach (AbilityData iability in acard.GetAbilities()) {
            if (iability && iability.trigger == type)
                TriggerAbility(iability, caster, acard, triggerer);
        }
    }

    // 3. Revisa sus Reliquias o Items pasivos equipados
    foreach (Card acard in caster.cards_item) {
        if (acard.CardData.item_type == ItemType.ItemPassive) {
            foreach (AbilityData iability in acard.GetAbilities()) {
                if (iability && iability.trigger == type)
                    TriggerAbility(iability, caster, acard, triggerer);
            }
        }
    }
}
```

### ¿Por qué lo ha programado así el autor?
Te podrías preguntar: *"Un momento, ¿antes me dijiste que hacer bucles era malo para el rendimiento? ¿Por qué el creador del engine hace tres bucles `foreach` cada vez que alguien ataca?"*

Razones arquitectónicas de esta decisión:
1.  **Iteración Restringida:** Fíjate bien en el código. El bucle **NO busca en el mazo ni en la pila de descartes**. Solo itera sobre `cards_power` (cartas activas) e `item_passives` (reliquias). Son listas diminutas (a lo sumo 5 o 10 elementos). El impacto en rendimiento es del 0%.
2.  **Facilidad de Debugging:** El patrón Observer puro con Eventos de C# es una pesadilla de debugear. Si te olvidas de Desuscribirte (`-= OnDamaged`), creas "fugas de memoria" (*memory leaks*) que rompen el juego invisiblemente. Con su enfoque centralizado, el autor elimina de raíz la posibilidad de *memory leaks*. El motor siempre sabe quién ejecuta qué.
3.  **Determinismo de Cola (Resolve Queue):** Al buscar él mismo las cartas (en vez de que ellas salten automáticamente), puede ordenar el resultado de esas respuestas en una **Cola de Resolución (`resolve_queue.AddAbility()`)**, garantizando que los combos visuales ocurran uno tras otro de forma ordenada (como en Slay the Spire), y no todos a la vez rompiendo las animaciones.

**Conclusión:** 
El concepto "Observer" a nivel *Game Design* es idéntico a lo que hemos hablado (La carta es una carpeta, el Trigger marca el cuándo). Pero a nivel *Código*, el autor ha priorizado la **estabilidad y el control absoluto de los turnos** mediante un administrador central (`BattleLogic`) en lugar de usar eventos puros y libres de C#.
