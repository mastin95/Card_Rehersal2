# C# y Unity: Guía de Conceptos Avanzados para Game Design

Este documento sirve como enciclopedia personal para entender los conceptos de programación puros que sustentan la arquitectura profesional de *Rogue Engine* y nuestras refactorizaciones.

---

## 1. Deconstruyendo `UnityAction` y los Delegados

En arquitectura de videojuegos profesional, necesitamos que sistemas independientes hablen entre sí sin "mezclarse". El código de batalla (`BattleLogic`) no debería saber cómo se dibujan los botones de la interfaz, pero tiene que avisarles de cuándo dibujarse. 
Para solucionar esto usamos **Delegados** (El patrón arquitectónico *Observer*).

### ¿Qué es `UnityAction` en realidad?
Muchos confunden `UnityAction` con `UnityEvent`. 
- `UnityEvent`: Es la cajita visual en el Inspector de Unity donde configuras cosas arrastrando con el ratón. Es pesada, lenta, y desastrosa para el rendimiento de un Roguelike.
- `UnityAction`: Es simplemente un **disfraz del motor de Unity** para el tipo nativo súper veloz de C# llamado `System.Action`.

Es, en esencia, un "Megáfono". Es una variable que guarda una lista de punteros de memoria hacia funciones ajenas que están esperando a ser llamadas.

### El Operador de Rescate `?.` (Null-Conditional)

Fíjate en esta línea sagrada de la inicialización de nuestros turnos:
```csharp
onBattleStart?.Invoke();
```

1. **`Invoke()`**: Significa *"Gritar a través del megáfono"*. Ejecuta de forma simultánea e inmediata todas las funciones (audios, UI, animaciones) que se han suscrito al combate.
2. **`?.` (Operador Null-Conditional)**: Esto le dice a C#: *"Antes de gritar a la sala, comprueba si el megáfono existe y si la sala no está vacía"*.
   - Si no pones `?` y ejecutas el juego de forma técnica o de prueba (sin las ventanas gráficas instanciadas), `onBattleStart` estará completamente vacío (`null`). Al intentar hacer `Invoke()` a la nada, Unity entrará en colapso por `NullReferenceException` y se congelará.
   - Poniendo `?`, solucionas el error. Si no hay gráficos suscritos, la operación se aborta milisegundos antes del desastre y el juego sigue calculando matemáticas en la siguiente línea.

---

## 2. Colecciones: `List` vs `Dictionary` (Notación Big O)

La diferencia de velocidad y madurez en C# radica casi por completo en saber cuándo usar una Lista y cuándo un Diccionario.

### `List<T>` (El Autobús de Asientos Listados)
- **Concepto:** Una fila de elementos uno detrás de otro.
- **Rendimiento de Búsqueda:** Lento (`O(N)`). Si estás buscando al pasajero 85, el procesador está físicamente obligado a preguntar nombre por nombre a los asientos 1, 2, 3... hasta encontrarlo.
- **Caso de uso óptimo en Game Design:** Las Listas son divinas cuando necesitas mantener un **orden inamovible** (como saber quién fue el primero en entrar a la sala) o cuando solo necesitas consultar el primer puesto de todos. Por ejemplo: `cards_deck` (El Mazo) es una Lista genial porque para robar carta siempre miramos el asiento 0 (`cards_deck[0]`) sin tener que buscar nada más.
- **Caso de uso óptimo en Game Design:** Las colecciones (`List` y `HashSet`) son esenciales para manejar datos cambiantes, mientras que los Diccionarios (`Dictionary`) son la herramienta definitiva para búsquedas ultrarrápidas ($O(1)$) cuando necesitas enlazar una clave con un valor (como enlazar un Evento con una Lista de cartas).

---

## 3. Demistificando los Triggers (Event-Driven Design)

En Rogue Engine, las cartas no están constantemente pensando "¿Me toca hacer algo?". Eso destruiría el rendimiento. En su lugar, usamos el patrón de **Diseño Orientado a Eventos (Event-Driven Design)** a través de los `AbilityTrigger`.

### ¿Qué es exactamente un `AbilityTrigger` a nivel de código?
Es simplemente una enumeración o **Enum** en C#. En el archivo `AbilityData.cs` (línea 721), el autor definió una lista cerrada de palabras clave asociadas a un número:

```csharp
public enum AbilityTrigger
{
    None = 0,
    Ongoing = 2,
    OnPlay = 10,
    StartOfTurn = 20,
    OnDraw = 30,
    OnDamaged = 42,
    // ... etc
}
```
Un Enum es la forma elegante de evitar usar `strings` (textos). En lugar de preguntar si el evento es `"Empezar Turno"`, preguntamos si es `AbilityTrigger.StartOfTurn`. Es infinitamente más rápido para el procesador comparar números (`20` == `20`) que comparar textos letra por letra.

### ¿Qué se verifica en cada Trigger Común?
Cuando ocurre un evento en el juego, nuestra función `FireTrigger` hace sonar la campana de esa "oficina postal". Si hay cartas registradas ahí, se ejecutan:

1.  **`OnPlay` / `OnPlayOther`**: Ocurre cuando arrastras físicamente una carta de tu mano a la mesa. El motor avisa: *"¡Alguien ha jugado algo!"*. Las pasivas de reliquias comprueban: *"¿El que la jugó fui yo o el enemigo? ¿La carta era de Fuego?"* Si cumplen la condición, saltan.
2.  **`OnDraw`**: Ocurre al pasar una carta del Mazo a la Mano. Ciertas reliquias o cartas pasivas que dicen *"Cuando robes una carta de ataque, gana 1 de Fuerza"* están suscritas a este Trigger.
3.  **`OnDamaged`**: Ocurre justo después de restar la vida a un personaje. Una armadura pasiva suscrita aquí se activará para reflejar daño (Thorns) o darte robo adicional.
4.  **`StartOfTurn`**: Ocurre al inicio del turno, ideal para curaciones periódicas (Regeneración) o pasivas que envanecen escudos.

### ¿Podría existir un `OnDiscard`?
**¡Absolutamente SÍ!**
Si quisieras crear una mecánica de Sinergia de Descartes (como en muchos juegos de cartas donde descartar te da bonificaciones), solo tendrías que:
1. Abrir `AbilityData.cs` y añadir `OnDiscard = 35,` a la lista del `enum`.
2. Ir a la función `DiscardCard()` en `BattleLogic.cs` (que programaremos pronto).
3. Añadir justo antes de que la carta toque el abismo: `FireTrigger(AbilityTrigger.OnDiscard, personaje);`.
De repente, tu motor entero soporta mecánicas de descarte. ¡Así de potente es esta arquitectura!

### ¿Por qué obligamos a enviar un `BattleCharacter caster`?
Nuestra función pide imperativamente saber **quién** causó el evento: `FireTrigger(AbilityTrigger trigger, BattleCharacter caster)`.

¿Por qué es vital? Imagina un combate 1vs1. Tienes la reliquia *"Cuando recibes daño (`OnDamaged`), roba 1 carta"*.
El enemigo te ataca. Tu personaje sufre daño. Llamamos a:
`FireTrigger(AbilityTrigger.OnDamaged, tu_personaje);`

El motor busca a todos los que reaccionan a `OnDamaged`, **incluidas las pasivas del enemigo**, porque el enemigo quizás tiene *"Cuando RECIBO daño, me curo"*. 
Al enviarle `caster = tu_personaje` en la función, cuando el motor evalúe la reliquia del Enemigo, dirá: *"El evento de daño acaba de ocurrir, pero le ha pasado a `tu_personaje`. Mi reliquia dice que solo salta si el daño me lo como YO (`enemigo`). Así que no me activo"*.

El `caster` es el **Sujeto del Evento**. Sin él, todas las cartas del tablero, tanto tuyas como del enemigo, saltarían a la vez provocando un caos.

---

## 4. Estructuras de Datos: Listas vs Diccionarios

### `List<T>` (La Pila de Asientos)
- **Concepto:** Una fila de elementos uno detrás de otro.
- **Rendimiento de Búsqueda:** Lento (`O(N)`). Si estás buscando al pasajero 85, el procesador está físicamente obligado a preguntar nombre por nombre a los asientos 1, 2, 3... hasta encontrarlo.
- **Caso de uso óptimo en Game Design:** Las Listas son divinas cuando necesitas mantener un **orden inamovible** (como saber quién fue el primero en entrar a la sala) o cuando solo necesitas consultar el primer puesto de todos. Por ejemplo: `cards_deck` (El Mazo) es una Lista genial porque para robar carta siempre miramos el asiento 0 (`cards_deck[0]`) sin tener que buscar nada más.

### `Dictionary<Key, Value>` (El Sistema Postal)
- **Concepto:** Datos emparejados matemáticamente. Tienes una "Llave" (Dirección o Código Postal) que te lleva mediante un salto de memoria directo al "Valor" (La carta real).
- **Rendimiento de Búsqueda:** Velocidad constante garantizada (`O(1)`). C# usa algoritmos `Hash` para teletransportarse directamente a la puerta que pides sin mirar las zonas anteriores.
- **Caso de uso óptimo en Game Design:** Bases de datos, cachés y el Event Bus de nuestro Refactor: 

```csharp
// Creación del Diccionario en RAM: 
// La Llave es el Enum de Evento, el Valor es el Grupo de cartas a las que les importa.
Dictionary<AbilityTrigger, List<Card>> trigger_cache;

// Búsqueda a la velocidad de la luz:
if (trigger_cache.ContainsKey(AbilityTrigger.OnDamaged)) // Esto es O(1).
{
    // ¡Entramos a procesar daño sabiendo al 100% que hay cartas interesadas
    // en lugar de iterar inútilmente sobre gente a la que no le importa!
}
```
