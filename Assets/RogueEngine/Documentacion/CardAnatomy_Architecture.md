# Anatomía de una Carta: Guía Detallada de Arquitectura y Componentes

## 1. El Concepto Global: El Modelo de "Muñecas Rusas"
Al observar la imagen de la carta `Dark Magic Ward`, es normal sentirse abrumado. Para entender su profundidad, debemos dejar de pensar en una carta como "un trozo de código que hace cosas". En su lugar, el juego utiliza un patrón arquitectónico de **Composición de Datos (ScriptableObjects)**.

Piensa en una carta como si fuera **el chasis de un ordenador de sobremesa vacío**. Por sí sola, la carta no sabe hacer nada. Para que funcione, debes enchufarle componentes independientes (tarjeta gráfica, procesador, memoria RAM). 

En Rogue Engine, esos componentes son `Traits` (Rasgos), `Stats` (Estadísticas Base) y `Abilities` (Habilidades). Al enchufarlos en el inspector de Unity, la carta adquiere reglas de comportamiento.

---

## 2. Desglose Componente a Componente

### A. Stats Principales y Clasificación (El ADN de la Carta)
Esta sección define las reglas de acceso a la carta: *Quién puede usarla, cuánto cuesta y si es un ataque o un hechizo.*

*   **Card_type (`Skill`):** Define categóricamente a la carta. El motor del juego puede tener reglas globales como *"Las Skills no pueden ser jugadas si estás Silenciado"*.
*   **Team (`witch`):** Espectacular ejemplo de organización. En vez de escribir la palabra "witch", le estás conectando un archivo `TeamData`. Esto significa que el juego puede filtrar automáticamente que esta carta solo aparezca como recompensa si juegas con el personaje Bruja.
*   **Mana (`1`):** El recurso que el Controlador de Batalla restará al jugarla.

### B. Traits (Los Rasgos Pasivos Universales)
Los *Traits* son modificadores pasivos de las reglas del juego que afectan a cómo la carta es tratada por el motor. **No son la habilidad que se ejecuta al jugar la carta**, sino reglas pasivas adheridas a ella.

*   **Ejemplo en tu imagen:** Vemos que tiene el Trait `skill (Trait Data)`. Esto avisa al sistema subyacente de cómo categorizarla internamente. 
*   **Otros ejemplos prácticos de Traits (Si los creáramos):**
    *   **Trait: *Exhaust* (Consumible):** Un componente que le dice a la pila de descarte: *"Oye, cuando esta carta se juegue, no vayas al descarte, destrúyete de la partida actual"*.
    *   **Trait: *Innate* (Innata):** Le dice al repartidor de mazo: *"Cuando empiece la batalla, fuérzame siempre a estar en la mano inicial del jugador"*.
    *   **Trait: *Ethereal* (Etérea):** Le dice al motor de turnos: *"Si estoy en tu mano cuando termine tu turno, agótame automáticamente"*.
  
*   **Por qué es tan potente:** Si quieres que 50 cartas distintas se agoten al jugarse, no programas "destruir" 50 veces. Simplemente arrastras el archivo único `Exhaust Trait` a la lista de Traits de esas 50 cartas. Si mañana decides que Exhaust en vez de destruir, destierra a otra dimensión, solo cambias el archivo `Exhaust Trait` base.

### C. Abilities (El Motor de Ejecución Activa)
Aquí es donde ocurre la acción real. Cuando haces clic y arrastras la carta sobre un enemigo, se leen estas `Abilities` en orden. Como vemos en tu imagen `Dark Magic Ward`, tiene **dos habilidades separadas**. ¿Por qué no una sola que haga todo? Porque separarlas las hace reutilizables.

**Habilidad 1: `heal2`**
*   Esta habilidad (`AbilityData`) seguramente tenga configurado un *Trigger* = `OnPlay` y un *Efecto* = `EffectHeal` con valor de vida fija (Ej: 5 HP).
*   *Reutilización:* Esta misma bola de datos `heal2` la podrías enchufar en otra carta llamada "Poción Menor" o en una habilidad pasiva del personaje, sin reescribir código.

**Habilidad 2: `heal_per_curse2`**
*   Esta habilidad es más compleja. Probablemente su *Efecto* no cure un valor fijo, sino que haga un recuento de cartas tipo "Maldición" en la mano, y luego multiplique ese número por su valor de curación base.

### D. El Texto Dinámico (Card Text)
Fíjate en esta genialidad en tu imagen:
`Exhaust all curses in your hand. Heal <value0> hp + <value1> for each curse removed.`
*   El texto **NO** dice "Cura 5 hp + 2 hp por cada maldición".
*   El motor utiliza un parseo de texto (Parsing). `<value0>` mira a la **primera habilidad** de la lista (`heal2`) y extrae su poder numérico para escribirlo en pantalla. `<value1>` mira a la **segunda habilidad** (`heal_per_curse2`).
*   **¿Cuál es la magia de esto?** Si un Game Designer decide balancear la carta y aumenta la curación base de la Habilidad 1 de `5` a `7`, no tiene que acordarse de modificar el campo de texto de la carta también. ¡El texto se actualizará y escribirá mágicamente un `7` solo dentro del juego!

### E. Upgrades (El Sistema de Subida de Nivel)
Los roguelikes de cartas (tipo Slay the Spire) permiten ir a una hoguera y mejorar una carta. Aquí la composición vuelve a brillar.
*   **Level_max (`2`):** Permite subirla a nivel 2.
*   **Upgrade_mana (`-1`):** Al mejorarla, en lugar de aumentar su daño o curación en sus habilidades (que requeriría modificar las habilidades en sí), las reglas de la carta mutan para que su invocación cueste 1 maná menos (costaría 0 maná). 

---

## 3. Ejemplos Prácticos Finales

Para asentar este ecosistema de *Piezas Lego*, veamos 3 ejemplos de Game Design de cómo fabricarías cartas complejas únicamente enchufando componentes en el inspector:

### Ejemplo 1: El Ataque Venenoso que Sangra (Cartas Híbridas)
**Concepto:** Carta de 2 de Maná que hace daño fuerte, aplica veneno al rival, y si no la juegas y pasas turno, te quita vida a ti.
*   **Abilities [1]:** `Damage_8` (Habilidad con *EffectDamage*).
*   **Abilities [2]:** `ApplyPoison_3` (Habilidad con *ApplyStatus* -> Estado `Poison`, Trigger `OnPlay`).
*   **Traits [1]:** `Bleeding_Hand` (Un Trait programado como: "Trigger `OnEndOfTurn_InHand` -> *EffectDamage* al jugador").

### Ejemplo 2: El Escudo Gélido Reactivo
**Concepto:** Carta que te da escudo. Si te pegan mientras tienes el escudo, el atacante se congela.
*   **Abilities [1]:** `GainArmor_10` (Habilidad normal).
*   **Abilities [2]:** `ApplyFrost_WhenHit` (Habilidad cuyo *Trigger* no es `OnPlay`, sino `OnReceiveDamage`. Su efecto es aplicar el Estado `Frost`).

### En Resumen:
La "profundidad" de este sistema, diseñado en la arquitectura del Asset, radica en su **hiperconectividad**. Cada campo en el inspector no es una orden hardcodeada, sino un enchufe ("socket") esperando a que le conectes un módulo preconstruido. Esto permite crear interacciones sistémicas extremadamente complejas entre cartas y artefactos casi sin que el programador tenga que intervenir nunca.
