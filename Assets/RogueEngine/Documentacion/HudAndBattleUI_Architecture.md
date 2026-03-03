# Documentación Arquitectónica: Sistema de HUD y Battle UI

## 1. Visión General (Overview)
El sistema visual de la escena de batalla (`BattleForest`) está construido utilizando una **Arquitectura de Interfaz de Usuario Desacoplada** estándar en la industria. Esto significa que la HUD (Heads-Up Display) funciona como un "Lienzo Inteligente" separado espacial y lógicamente de lo que ocurre en el mundo 3D/2D del juego.

El responsable del dibujo y control de estos elementos es la jerarquía central que cuelga del `GameObject` maestro llamado `BattleUI` y de los contenedores complementarios como `HandsArea`.

---

## 2. Los Cuatro Pilares del Diseño de Interfaz

### Pilar 1: Separación de Mundo vs. Pantalla (Canvas Overlay)
En juegos complejos como Rogue Engine, los elementos se dividen en dos espacios:
*   **El Espacio de Mundo (World Space):** Donde vive el fondo pintado (el bosque), los efectos de partículas (`FX`), las luces y la cámara de batalla (`BattleCamera`). 
*   **El Espacio de Pantalla (Screen Space Overlay):** Donde viven los elementos con los que el jugador interactúa a través de sus periféricos (ratón/táctil), como las cartas en su mano, la esfera de Maná "99" o el botón de interactuar "End Turn". 

El objeto `BattleUI` funciona como un *Canvas Overlay*. Su utilidad principal es garantizar al diseñador que, sin importar cuánto tiemble la cámara del mundo o qué efectos visuales ocurran en el bosque, los botones y la información vital **siempre estarán dibujados por encima** y nítidamente sobre la pantalla del jugador.

### Pilar 2: Auto-Maquetado Flexbox (Layout Groups invisibles)
En el centro de la pantalla y en los laterales, la estructura se sostiene sobre cajas invisibles agrupadoras. Esto se observa en los recuadros rectangulares grises denominados `BoardSlot_X` o las listas verticales de personajes.

*   **El Problema:** Programar la coordenada (X, Y) exacta para una carta cuando el jugador roba 3 cartas, versus cuando tiene 10 cartas en la mano.
*   **La Solución (Automática):** Se utilizan componentes nativos de Unity como `HorizontalLayoutGroup` o `GridLayoutGroup`. Estos actúan de manera similar a contenedores de diseño web (Flexbox). 
*   Cuando en el código principal se ejecuta la orden "Robar una Carta", no se le indica a la carta dónde debe ir. Simplemente se la "instancia" (se invoca) y se la hace hija (Child) del objeto `HandsArea`. El propio contenedor invisible asume el control: detecta a la nueva hija, redimensiona el espacio, ajusta la separación (padding) y re-centra estéticamente todas las cartas de la mano sin necesidad de código adicional.

### Pilar 3: Responsividad (Anchors dinámicos)
Para garantizar que la HUD se vea estéticamente correcta en cualquier tipo de monitor (Panorámico, Ultrawide 21:9, o la pantalla cuadrada de un iPad Antiguo), el sistema no usa coordenadas fijas (Ej: "Pon el botón en la posición X=1000").
En su lugar, los bordes invisibles blancos o los RectTransforms de cada elemento utilizan **Anclajes (Anchors)**.

*   **Esfera de Maná (99):** Tiene un *Anchor* inferior izquierdo (`Bottom-Left`). 
*   **Botón Ends Turn:** Tiene un *Anchor* inferior derecho (`Bottom-Right`).
*   **Barra Superior Amarilla:** Tiene un *Anchor* superior-centro (`Top-Center`).
Si el jugador cambia la resolución de la ventana, es como si una goma elástica estirase la interfaz central; la esfera azul y el botón siempre se mantendrán "pegados" y a salvo en sus respectivas esquinas.

### Pilar 4: Control Centralizado (Controlador Único)
La HUD se gestiona a través de un patrón de diseño profesional conocido como *Model-View-Controller (MVC)*. 
En Unity, esto se traduce en que los botones y las esferas de vida **no son inteligentes por sí solos**, son entidades pasivas (La Vista o *View*).

*   **¿Cómo reaccionan los números?** El motor del juego (`GameClient` o `TheNetwork`) conoce las reglas verdaderas de la partida (Ej: "La carta cuesta 3 de Maná. Jugador tiene ahora 96"). 
*   En lugar de buscar esferas sueltas, el sistema reporta este cambio únicamente al script gobernador general: `Assets/RogueEngine/Scripts/UI/BattleUI.cs` (El Controlador). 
*   Es este guion directivo, y solo este guion, el que tiene "el mapa referencial" de dónde están ubicados los textos y las barras de vida en la pantalla, y se encarga de decirle ordenadamente a los componentes visuales: *"Chicos, actualizad los gráficos para que reflejen un 96 de maná"* o *"Oculta el slot de la izquierda porque esa criatura acaba de morir"*. 

Este flujo previene el temido efecto de "código espagueti" donde cientos de piezas visuales interactúan caóticamente con el código profundo del juego.
