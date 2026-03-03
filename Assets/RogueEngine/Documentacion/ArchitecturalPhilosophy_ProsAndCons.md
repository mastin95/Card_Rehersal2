# Filosofía de Arquitectura: Por Qué las Cartas están Despiezadas

Para tomar decisiones sólidas de Game Design, necesitas meterte en la cabeza del arquitecto que programó el *Rogue Engine*. La pregunta fundamental que te haces es: **¿Por qué una Carta no tiene todo su código dentro de ella misma? ¿Por qué separar CardData, AbilityData y EffectData en archivos diferentes?**

La respuesta corta es: **El Patrón de Diseño "Composición sobre Herencia" (Composition over Inheritance)** y el **Patrón Flyweight**.

Aquí te explico, apartado por apartado, los motivos exactos de esta filosofía, sus ventajas masivas y las (pocas) desventajas a las que te vas a enfrentar.

---

## 1. La Separación Fundamental: Card vs. Ability vs. Effect

El programador del Asset visualizó el juego en 3 capas de abstracción para evitar escribir código repetido:

### Capa 1: `CardData` (El Cascarón / El ID)
*   **¿Qué va aquí?** Solo información estática y metadatos: El Nombre ("Bola de Fuego"), cuánto de maná cuesta (3), de qué Rareza es (Épica), y su imagen (Sprite).
*   **¿Por qué NO van aquí las reglas del daño?** Porque si el "Daño" estuviera programado dentro de `CardData`, tendrías que escribir un archivo de script nuevo por cada una de las 300 cartas de tu juego (`BolaDeFuego.cs`, `DagaVenenosa.cs`, `Espadon.cs`). Eso sería inmanejable. `CardData` es solo un contenedor de cartón que el jugador sostiene en la mano. Para que la carta haga daño, el engine le dice a la carta: *"Oye, ¿qué Habilidades tienes enchufadas?"*.

### Capa 2: `AbilityData` (Las Reglas de Ejecución)
*   **¿Qué va aquí?** La Lógica de Juego (El *Cuándo* y *A Quién*). Define el Trigger (`OnPlay`), el Target (`SelectTarget`), y el Valor numérico de poder (`5`).
*   **¿Por qué existe separada de la Carta?** Para el reuso sistémico. Imagina que diseñas una carta que al jugarse cura 5 puntos, pero también quieres que un Monstruo Jefe cure 5 puntos cada turno. 
    *   Si la curación estuviese atrapada en la carta, el Monstruo no podría usarla. 
    *   Al estar separada en un `AbilityData`, el motor de combate simplemente le "enchufa" el mismo archivo `.asset` de curación tanto a la Carta del Jugador como al Personaje del Monstruo. ¡Acabas de ahorrarte programar una IA curativa para el monstruo!

### Capa 3: `EffectData` (La Física Molecular)
*   **¿Qué va aquí?** El código puro. Es el único lugar donde existe programación de verdad (C#). Un `EffectDamage.cs` literalmente dice: `VidaDelObjetivo -= PoderDeLaHabilidad`.
*   **¿Por qué está separada de la Ability?** Porque las reglas (*Cuándo y A Qué*) y las consecuencias (*Qué ocurre*) a menudo se combinan de formas impredecibles.
    *   Puedes tener la "Regla" (Apunta a todos los enemigos de la mesa).
    *   Y el "Efecto" (Haz daño).
    *   Mañana, quieres una carta que *Cure* a todos los enemigos de la mesa (una carta maldita). En vez de programar una "AbilityCurarTodos", simplemente enchufas la regla "Apunta a todos", pero le arrastras el efecto "Curar" en lugar del de "Daño".

Con 3 Efectos (Daño, Cura, Robar) y 3 Reglas (OnPlay, StartTurn, OnDeath), el sistema de separar las cosas matemáticamente te permite crear `3x3 = 9` habilidades distintas sin escribir código nuevo.

---

## 2. Ventajas y Desventajas (Pros and Cons)

Para que entiendas al arquitecto de este sistema estandarizado AAA, aquí tienes tu balance de decisiones:

### Ventajas Brutales (Pros)
1.  **Escalabilidad Exponencial:** Si este motor usara código dentro cada carta típica de juegos amateur, hacer un "Slay the Spire" de 350 cartas requeriría un equipo de 3 programadores semanas enteras para testear 350 scripts distintos. Con este sistema modular, 1 solo Game Designer (TÚ) puede crear las 350 cartas en 2 días sentándote frente al Inspector de Unity e intercambiando piezas, sin saber programar.
2.  **Economía de Memoria (Patrón Flyweight):** Si 50 cartas distintas aplican Veneno, tu juego no tiene el código de veneno clonado 50 veces en la memoria RAM de tu jugador (lo que lagearía los móviles). El juego solo tiene 1 archivo de Veneno guardado en memoria central, y esas 50 cartas apuntan (hacen referencia) a él como si miraran a un semáforo común.
3.  **Balanceo Centralizado:** Si el Veneno está muy OP y quieres nerfearlo para que haga 2 de daño en lugar de 3, no tienes que abrir 50 cartas a mano para bajarles el daño una por una. Abres el archivo central del Veneno, cambias un número, y tus 50 cartas de fuego quedan nerfeadas mágicamente en 3 segundos. Ese es el sueño de todo Director de Diseño.

### Desventajas Notorias (Cons)
1.  **Burocracia de Archivos Inicial (El Infierno de los Folders):** La maldición de la modularidad. Para hacer una simple carta de "Ataca 5", antes tenías 1 archivo con 1 línea de código. Ahora tienes que crear 3 archivos físicos en disco: el `CardData`, el `AbilityData` y el `EffectData`. (¡Esto es exactamente por lo que programamos la herramienta `CardAssemblerWindow` ayer para ti! Para que tú dibujes 1 carta y la máquina haga la burocracia horrible de los 3 archivos por detrás).
2.  **Curva de Aprendizaje Críptica:** Si no montaste el Asset, mirar los nombres abstractos en las listas vacías (`AbilityData`, `TriggerTargetArray`) es aterrador. Como diseñador, no puedes simplemente abrir un código C# y leer secuencialmente lo que hace una carta. Tienes que ir saltando de inspector en inspector siguiendo las "miguitas de pan" para entender cómo funciona un combo.
3.  **Dificultad en Lógica Hiper-Anidada (Edge Cases):** ¿Qué pasa si quieres hacer una carta absurdamente específica? Ej: *"Haz 5 de daño, PERO si el enemigo es Orco, haz 7 de daño, PERO si además está Lloviendo, cura 2 al Mago Aliado"*. 
    El diseñador del asset **no previó tu imaginación infinita**. Si intentas hacer eso usando los bloques modulares, acabarás con 15 `AbilityDatas` anidados creando una pesadilla visual en el inspector. Cuando una carta es tan única que rompe el molde, el sistema te obliga a ir al programador y decirle: *"Programame un EffectCustomizado hiper-rígido solo para esta carta"*, rompiendo un poco la filosofía modular.

---

## 3. Conclusión Práctica: Cómo Tomar Decisiones

Entendida la cabeza del creador del Asset, tus decisiones diarias de diseño en el `Card Assembler` deberían ser así:

*   **¿La regla es universal? (Cuidado Mantenimiento)** -> "Quiero una carta que Queme". No abuses creando 20 habilidades distintas llamadas `Burn_1`, `Burn_2`, `Burn_5` para cada carta individual. Trata de crear **UN SOLO EFFECTO** de Daño Fuego, **UN SOLO STATUS** de Quemar, e inyectárselo a muchas Cartas (Solo cambiando el `Value` numérico genérico en ellas). Abrazar el "Flyweight Pattern".
*   **¿La regla define a un Arquetipo? (Usa Stats y Traits)** -> Si todas las cartas tipo `Demonio` reaccionan mal contra `Magia Sagrada`, no programes la comprobación dentro de las habilidades de cada carta Demonio. Ponle el Meta-dato "Demonio" como un `Team` o `Trait` en la base de la `CardData`, y crea una habilidad Sagrada que lea masivamente ese Meta-dato mediante "Filters".
*   **¿Me frustra la modularidad (Complejidad Espagueti)?** -> Si intentas hacer una carta y ves que tienes que encadenar 5 habilidades seguidas en el inspector y agregar filtros condicionales a cada nivel para que funcione el combo... ¡Párate! Como diseñador de combate, eso es un foco rojo. Las cartas más elegantes no son las mecanizadas microscópicamente, sino la combinación de "Trigger Raro + Efecto Simple" (Ej: `OnDeathOther` -> `Draw_1`). 

Con este conocimiento bajo el brazo, ahora dominas la topografía abstracta de cualquier TCG (Trading Card Game) AAA del mercado, ya que todos (Legends of Runeterra, Marvel Snap, Hearthstone) usan variantes de esta misma macro-estructura bajo el capó.
