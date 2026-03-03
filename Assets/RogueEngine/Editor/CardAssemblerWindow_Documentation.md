# Documentación de Diseño y Herramientas: Ensamblador de Cartas (`CardAssemblerWindow.cs`)

## 1. Visión General (Overview)
El script `CardAssemblerWindow.cs` es una **Herramienta de Editor Extendida (Custom Editor Tool)** para Unity. A diferencia de un script tradicional que define cómo salta un personaje o cómo se mueve la cámara, este script *existe únicamente para facilitarle la vida a los diseñadores*.

Su objetivo es proporcionar una interfaz visual (una ventana) dentro del propio motor de Unity donde el equipo de Game Design pueda fabricar nuevas cartas ensamblando piezas preexistentes (efectos, estados, estadísticas) sin necesidad de escribir una sola línea de código, sin errores de tipografía y manteniendo una organización automática de carpetas.

---

## 2. Los Pilares de la Herramienta (UX/UI y Arquitectura)

Para entender cómo funciona el Ensamblador, imagina que las cartas son coches. No fabricas el motor y las ruedas desde cero para cada coche nuevo; en su lugar, tienes un catálogo de "Piezas" (Efectos y Estados) y simplemente las eliges y las montas sobre el "Chasis" (La Carta). 

El script se divide en tres fases principales de funcionamiento: **Recolección, Interacción y Ensamblaje**.

### Fase 1: Recolección y Escaneo Automático (El Catálogo)
Cuando la ventana se abre en Unity (o cuando le das al botón "Refresh"), el script escanea de forma automática todo tu proyecto para averiguar de qué piezas dispones. Esto lo hace en el método `OnEnable()`:

*   **Lectura de Personajes (Carpetas):** El script lee la carpeta maestra `Resources/Cards` y revisa qué subcarpetas existen (`Adventurer`, `Witch`, `Enemies`, etc.). Las guarda en una lista desplegable para que puedas decidir a quién pertenece la nueva carta.
*   **Lectura de Estados (Statuses):** Usa `Resources.LoadAll` para encontrar todos los "Estados Alterados" (Veneno, Escudo, Evasión) que ya han sido diseñados y empaquetados previamente en el juego.
*   **Lectura Dinámica de Efectos (Reflection):** Esta es la magia más avanzada del script. En lugar de buscar archivos físicos, el script explora el código fuente del juego buscando cualquier clase programada que *herede* del concepto base `EffectData` (como `EffectDamage`, `EffectHeal`, `EffectSteal`). Esto significa que si mañana un programador añade un nuevo efecto llamado `EffectInstaKill`, el Ensamblador lo detectará y lo pondrá en la lista desplegable *automáticamente* sin necesidad de actualizar la herramienta.

### Fase 2: Interacción Visual (La Interfaz o GUI)
La función `OnGUI()` es la encargada de dibujar los menús desplegables, cajas de texto y botones en la ventana. Se divide en bloques lógicos:
1.  **Información Básica:** Tu ID interno, el nombre que verá el jugador, cuánto maná cuesta y de qué tipo es (Skill, Attack).
2.  **Organización:** Dónde se guardará (en qué carpeta de qué personaje), su facción y qué dibujo (Sprite) representará a la carta.
3.  **Habilidades Infinitas (Lista Dinámica):** Este es el corazón modular. Puedes decirle a la herramienta: *"Esta carta tiene 3 habilidades distintas"*. La herramienta clonará inmediatamente 3 "slots" en pantalla. Para cada slot, el diseñador puede configurar:
    *   **Trigger (Cuándo pasa):** Por ejemplo, `OnPlay` (al jugarla).
    *   **Target (A quién afecta):** Por ejemplo, `SelectTarget` (elige al enemigo).
    *   **Main Effect:** Eliges de la lista mágica generada en la Fase 1 (¿Hace daño? ¿Cura?).
    *   **Apply Status:** Opcionalmente, le pegas un estado alterado de la lista (¿Aplica veneno?).
    *   **Power/Value:** El número asociado a esa habilidad (Ej: 15 de daño, o 2 turnos de veneno).

Para evitar que la ventana se haga gigantesca y se salga del monitor al añadir muchas habilidades, la interfaz está envuelta en un **ScrollView**, que crea automáticamente una barra de desplazamiento vertical cuando es necesario.

### Fase 3: El Botón "Assemble & Create" (El Motor Interno)
Cuando el diseñador está feliz con la mezcla y pulsa el botón, ocurren las siguientes automatizaciones para prevenir errores humanos:

1.  **Enrutamiento Seguro:** El script averigua qué personaje elegiste y crea automáticamente una subcarpeta llamada `Abilities` dentro de la carpeta de ese personaje, asegurando que todos los archivos inyectados se guarden ordenados para no saturar la carpeta principal del catálogo de cartas.
2.  **Fabricación de Piezas (`BuildAbility`):** El script recorre los *slots* de habilidades que completaste. Para cada uno, fabrica físicamente un archivo `.asset` del Efecto y un archivo `.asset` de la Habilidad y los conecta entre sí en la memoria.
3.  **El Ensamblaje Final:** Acto seguido, crea el cascarón final, el archivo `CardData` de la carta, le inyecta las estadísticas básicas (Maná, Nombre, Imagen) y le "enchufa" las habilidades recién fabricadas en el paso anterior.
4.  **Confirmación Sensorial:** Finalmente, guarda los archivos físicos en el disco, envía un mensaje verde de éxito a la Consola para notificar al diseñador, e inteligentemente **selecciona la nueva carta creada**, haciendo que el Inspector de Unity salte directamente a ella para que el diseñador pueda revisar el resultado final de inmediato.

---

## 3. ¿Por qué esta herramienta es un estándar Profesional?

Si has de documentar el "Por Qué" existe esto para un Director de Proyecto, estas son las métricas de valor:

*   **Evita el "Cuello de Botella" de los Programadores:** El equipo de desarrollo de reglas (Game Designers) ya no necesita molestar ni depender de que un programador cree una nueva carta. Se independiza el trabajo creativo del trabajo técnico.
*   **A prueba de Tonterías (Foolproof Workflow):** Hacer una carta a mano requiere hacer 4 clics derechos, crear 4 archivos distintos, nombrarlos bien y arrastrarlos uno dentro del otro correctamente en el inspector. Un humano cometerá errores tras crear 50 cartas (como olvidarse de vincular un daño). El Ensamblador automatiza las vinculaciones, haciendo el proceso 100% seguro.
*   **Iteración Rápida (Rapid Prototyping):** En un juego de cartas, probar y balancear números es vital. Esta herramienta permite concebir y probar una carta nueva en 5 segundos, en contraste con los 3 minutos que requeriría hacerla manualmente.
*   **Sin Código Espagueti:** Al ser puramente orientada a datos modulares (el Daño y el Veneno son piezas separadas de la carta), el juego no requiere nuevos "If/Else" en el código fuente de combate cada vez que se estrena una carta distinta.
