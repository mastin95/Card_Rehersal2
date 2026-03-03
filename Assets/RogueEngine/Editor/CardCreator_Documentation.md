# Documentación de Diseño y Arquitectura: Generador Automático de Cartas (`CardCreator.cs`)

## 1. Visión General (Overview)
El script `CardCreator.cs` es una herramienta de **Editor de Unity** (no se ejecuta durante el juego o *runtime*). Su propósito principal es automatizar la creación de *assets* de cartas para el motor de juego (Rogue Engine). 

Utiliza el patrón de **Diseño Orientado a Datos (Data-Driven Design)** mediante el uso intensivo de `ScriptableObjects`. Esto permite definir el comportamiento, estadísticas y efectos de las cartas únicamente modificando datos, sin necesidad de programar lógica nueva para cada carta.

## 2. Las "Raíces": Arquitectura y Jerarquía de Datos
Para entender cómo este script habla con el resto del código, es fundamental comprender la jerarquía modular que exige el sistema de combate de Rogue Engine. La arquitectura sigue un modelo de **Composición sobre Herencia**.

La estructura de datos se divide en tres capas principales de mayor a menor jerarquía:

1. **`CardData` (La Carta):** Es el contenedor principal. Define la identidad de la carta (ID, coste de maná, rareza, tipo) y contiene una lista de habilidades.
2. **`AbilityData` (La Habilidad):** Define *qué* hace la carta de forma abstracta, *cuándo* lo hace y a *quién* afecta (*Triggers* y *Targets*). Contiene el valor numérico base y listas de Efectos y/o Estados (*Buffs/Debuffs*).
3. **`EffectData` / `StatusData` (La Ejecución):** 
   - **Efectos (`EffectData`):** Lógica instantánea (ej. `EffectDamage` hace daño). Reciben su poder numérico de la variable `AbilityData.value`.
   - **Estados (`StatusData`):** Lógica persistente en el tiempo (ej. *Evasive*, Veneno, Escudo).

### ¿Cómo se comunica `CardCreator` con estas raíces?
El script actúa como un "ensamblador". En lugar de crear estos archivos a mano haciendo clic derecho en el editor de Unity, el script llama al método `ScriptableObject.CreateInstance<T>()` para crear estas piezas en la memoria RAM. Primero las configura y conecta entre sí (asignando el `EffectDamage` a la `AbilityData`, y la `AbilityData` al `CardData`), y finalmente usa `AssetDatabase.CreateAsset()` para serializar (guardar) estas conexiones como archivos físicos `.asset` en el disco duro del proyecto.

---

## 3. Desglose del Script (Paso a Paso)

### 3.1. Inicialización Automática
```csharp
[InitializeOnLoadMethod]
private static void CreateCard()
```
El atributo `[InitializeOnLoadMethod]` es una directiva nativa de Unity. Le indica al motor: *"Ejecuta esta función automáticamente cada vez que el código termine de compilar o cuando abramos el proyecto"*.
La primera comprobación del script busca si la carta ya existe usando `AssetDatabase.LoadAssetAtPath`. Si ya existe, el script termina prematuramente (`return`) para no sobrescribir o deshacer el trabajo de balanceo si un diseñador ya ha ajustado los números de la carta a mano.

### 3.2. Paso 1: Creación de Efecto y Habilidad Ofensiva
Para que una carta haga daño, primero necesitamos instanciar el "concepto" de daño y luego insertarlo en una habilidad.
- Se instancia un `EffectDamage` vacío. Este objeto es agnóstico al daño; en sí mismo no sabe *cuánto* daño hará, solo sabe *cómo* hacerlo.
- Se serializa como un archivo `.asset` aparte.
- Se instancia un `AbilityData` (El Ataque). Aquí es donde el Diseñador define las reglas matemáticas:
  - **Cuándo ocurre (Trigger):** `OnPlay` (Al jugar la carta de la mano).
  - **A quién apunta (Target):** `SelectTarget` (Requiere que el jugador seleccione un objetivo en la pantalla).
  - **Cuánto poder tiene (Value):** `5`. Este es el número literal que el `EffectDamage` consumirá durante la partida.
  - **Qué hace:** Se vincula a la matriz de efectos `effects = new EffectData[] { effectDamage };`.

### 3.3. Paso 2: Creación de Habilidad Defensiva (Estado)
La carta aplica un *Buff* al jugador (*Evasive* - Esquivar). A diferencia del daño directo (que requiere crear un asset de efecto específico para esta habilidad), los Estados suelen ser entidades universales y reutilizables por múltiples cartas o enemigos.
- **`Resources.Load<StatusData>`:** El script busca en las carpetas de recursos internos de Unity (`Resources`) un estado (ScriptableObject) preexistente llamado `evasive`. 
- Se crea una segunda capa de habilidad conceptual `AbilityData` (El Sigilo).
- Se configura para apuntar al lanzador (`CharacterSelf`) con un valor de `1` (representando la duración en turnos o los "acumuladores/stacks" del estado a ganar).
- En lugar de asignarse a los efectos, el estado que cargamos de la memoria se inyecta en la matriz correspondiente: `status = new StatusData[] { statusEvasive };`.

### 3.4. Paso 3: Ensamblaje de la Carta Principal
Se instancia y configura el activo final y maestro: `CardData`.
- Se asignan sus descriptores y estéticas (ID, nombre, descripción).
- Se inyectan de nuevo referencias a sistemas globales preexistentes utilizando `Resources.Load` (a qué Facción/Equipo pertenece, qué nivel de Rareza tiene para los *Drop Rates*).
- Se establecen directrices de diseño económico y restricciones (Coste en fase de tienda = 100, Coste de maná al jugarla = 1).
- **El puente vital:** Se asignan las dos habilidades creadas en los pasos anteriores a la lista de la carta para que asuma su comportamiento durante el combate:
  `card.abilities = new AbilityData[] { abilityAttack, abilityStealth };`

### 3.5. Guardado en Disco
```csharp
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```
El paso final en la programación de herramientas. Guardamos forzosamente todos los archivos `.asset` en el disco duro y luego obligamos al árbol de carpetas de Unity a refrescarse visualmente para que los nuevos archivos aparezcan y sean manipulables por los diseñadores en el inspector.

---

## 4. Estándares de Game Design Aplicados

Para alguien que no interviene en el código (Game o Level Designers), este script manifiesta los siguientes estándares fundamentales de la arquitectura en videojuegos modernos:

1. **Modularidad Inyectable (Plug and Play):** El daño y el estado *Evasive* no están programados *dentro* de la carta "Phantom Strike" mediante `if/else`. La carta es un cascarón vacío al que se le inyectan "ladrillos" de propiedades. Esto concede al diseñador combinaciones infinitas en el editor (ej: se podría crear una carta que Cure y de Veneno intercambiando arrastrando los `EffectData` correctos desde el inspector).
2. **Reutilización de Datos Comunes (Referencing):** Al usar `Resources.Load("Status/evasive")`, el script hace referencia al mismo objeto "Evasión" usado universalmente en todo el juego. Si un diseñador del combate más adelante decide nerfear la evasión para que mitigue un 50% del daño en lugar del 100%, solo altera ese archivo central, y todas las cartas y enemigos (incluida la generada por este script) heredarán el cambio automáticamente sin tocar una sola línea de código.
3. **Escalabilidad de Contenido (Pipelines):** En juegos con arcos masivos de progresión (como los *Roguelite Deckbuilders*), crear manualmente el cascarón de 300 cartas con sus habilidades y efectos anidados requiere cientos de "clics" propensos al error humano. Automatizar la "escritura base" o la importación masiva de contenido para luego realizar solamente un trabajo artesanal de balanceo de números en el inspector es el *pipeline* estándar y vital en producciones AA y AAA para reducir los tiempos muertos.
