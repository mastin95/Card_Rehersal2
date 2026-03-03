# Desglose Exhaustivo de Código: `BattleLogic.cs`

A petición de un análisis profundo y arquitectónico, este documento recorre de forma secuencial todo el código fuente de `BattleLogic.cs`, explicando bloque por bloque su propósito dentro de Rogue Engine y cómo las piezas se interconectan. 

El archivo consta de más de 1700 líneas de código divididas en responsabilidades específicas.

---

## 1. Inicialización y Preparación de la Batalla (Líneas 1 - 300)

El archivo comienza definiendo decenas de `UnityAction` (Eventos visuales). Aunque la arquitectura no usa Eventos C# para interacciones de cartas (por rendimiento), **sí usa Eventos C# masivamente para avisar a la interfaz de usuario (UI)** (Ej: `onCardPlayed`, `onTurnStart`). De este modo, la UI escucha al motor, pero el motor no depende de la UI.

### 1.1 `StartBattle(World, EventBattle)`
El punto de entrada del combate (Línea `89`).
Su función es convertir los datos estáticos del mapa (`World`) en personajes físicos instanciados.
1. Crea los instanciamientos de `BattleCharacter` para los Héroes (Champions) y luego para los Enemigos.
2. Llama a **`SetChampionCards()`** (Línea `192`), que es vital: Aquí se separan matemáticamente las listas lógicas.
   - Si un Item es Consumible o Reliquia Pasiva -> Va a `cards_item`
   - Si la Carta es normal o Mágica -> Va a `cards_deck`
3. Finalmente, arroja las cartas base al generador: `ShuffleDeck(character.cards_deck)`.
4. El combate no empieza de forma inmediata. Mete un callback en la `resolve_queue` para que llame a `StartTurn()` pasados `2f` segundos, permitiendo a las animaciones de aparición terminar.

---

## 2. El Flujo del Turno (Líneas 300 - 550)

### 2.1 `StartTurn()` (Línea `300`)
La función de orquestación más importante. Administra el "Mantenimiento" (Upkeep) matemático del juego al inicio de la ronda.
1. Limpia la basura residual del turno anterior (`ClearTurnData()`).
2. Actualiza los contadores de duración (`turn_timer`, `turn_count`).
3. Ordena a los personajes por velocidad (`CalculateInitiatives()`).
4. **Restauración de Maná:** Resetea el maná a su valor máximo, suma el `delayed_energy` (Maná guardado de turnos previos, como la mecánica *Retain* de Slay the Spire) y regenera el escudo.
5. **Daños Pasivos:** Ejecuta el daño por veneno (`StatusEffect.Poisoned`) y Quemaduras antes de robar carta.
6. Llama por primera vez al devorador de recursos `UpdateOngoing()` para asegurar que todas las auras están aplicadas correctamente.
7. Llama a `DrawHand(character)` y ejecuta los Triggers de las reliquias (`AbilityTrigger.StartOfTurn`).
8. Pone a la cola la función `StartMainPhase()`.

### 2.3 `CalculateInitiatives()` (Línea `510`)
Un diseño peculiar. El engine no recalcula la iniciativa contantemente. Ordena a todos los personajes por Velocidad y bloquea esa lista para la ronda actual (`battle_data.initiatives`). 
Para los buffos de velocidad, recalcula una segunda lista en la sombra (`initiatives_next`) para la ronda que viene. Esto evita bloqueos extraños o cambios de turno repentinos por cartas que bajan la velocidad de un enemigo a mitad de turno.

---

## 3. Acciones de Gameplay Base (Líneas 550 - 850)

Esta sección abarca las funciones que son disparadas directamente por *Inputs* del jugador (Clicks) o por la IA del enemigo.

### 3.1 `PlayCard()` (Línea `557`)
Es el motor principal de interacción. El orden de ejecución es crítico:
1. Comprueba si tienes maná (`CanPlayCard`) y te lo cobra.
2. Saca la carta de tu mano (`cards_hand`) para moverla a poderes (`cards_power`) o al descarte (`cards_discard`).
3. Llama a `UpdateOngoing()` para re-aplicar auras matemáticas por si jugar esta carta sumó algún buff pasivo al tablero.
4. **Trigger Activo:** Invoca `TriggerCardAbilityType(AbilityTrigger.OnPlay, card)` (Dispara el rayo de ataque/cura).
5. **Trigger Reactivo:** Invoca `TriggerCharacterAbilityType(AbilityTrigger.OnPlayOther, owner, card)` (Pregunta a las reliquias si deben hacer algo porque has jugado una carta).
6. Al terminar la carta gráfica, lanza `AfterPlayCardResolved` en la cola, que vuelve a llamar a `UpdateOngoing()` para limpiar muertos.

### 3.2 Manipulación de Mazo (Robo y Descarte)
- **`DrawHand()` (Línea `663`):** Usado al inicio del turno. Descarta tu mano actual (Excepto las cartas que tienen el Status `Keep`, la mecánica "Retener").
- **`DrawCard(nb)` (Línea `685`):** El motor principal de robo. Si no quedan cartas, llama a `ShuffleDiscardToDeck` automáticamente. Por cada carta robada, lanza **dos** eventos reactivos de forma local: `OnDraw` (la carta robada avisa) y `OnDrawOther` (tu personaje avisa que ha robado).

### 3.3 Creación y Alteración
- **`SummonCharacter()` (Línea `732`):** Instancia personajes intermedios (Mascotas/Invocaciones). Fíjate que al invocar, también lanza `OnPlay` sobre el personaje invocado (Actuando como un Grito de Batalla o *Battlecry* de Hearthstone).
- **`TransformCard()` / `TransformCharacter()`:** Reemplaza los datos (`CardData` o `CharacterData`) de una entidad viva en memoria, cambiando su set de reglas sin destruir el objeto visual de Unity.

---

## 4. Resolución de Combate y Daño (Líneas 850 - 950)

### 4.1 Matemáticas Pre-Daño (`DamageCharacter`, Línea `878`)
Tiene un *Overload* (Sobrecarga de funciones). Cuando un Personaje A ataca al Personaje B, entra aquí para calcular modificadores globales ANTES de hacer el daño físico:
1. Revisa los `StatusEffect` del Atacante. Si el atacante es `Courageous` (+Daño), suma multiplicadores (`factor_self += 0.5f`). Si tiene Miedo (`Fearful`), resta.
2. Revisa los `StatusEffect` del Defensor. Si el defensor es Vulnerable, suma multiplicadores de dolor.
3. Multiplica el daño bruto por estos factores calculados y llama a la versión real de la función (`DamageCharacter` genérico).
4. **La mecánica de Espinas (`Thorn`):** Después de golpear, inmediatamente revisa si el defensor tiene "Espinas", de ser así, devuelve una versión de la función `DamageCharacter()` directa hacia el atacante original.

### 4.2 Impacto Físico (`DamageCharacter(Target)`, Línea `908`)
Aquí es donde caen los puntos de vida.
1. Calcula la armadura: Si el daño es `10` y el escudo (`shield`) es `3`, resta `3` al daño y deja el escudo a `0`.
2. Suma el daño residual a la propiedad `target.damage` (En Rogue Engine, la vida no disminuye, el daño se acumula. La vida actual es `GetHPMax() - damage`).
### 4.2 Impacto Físico (`DamageCharacter(Target)`, Línea `908`)
Aquí es donde caen los puntos de vida.
1. Calcula la armadura: Si el daño es `10` y el escudo (`shield`) es `3`, resta `3` al daño y deja el escudo a `0`.
2. Suma el daño residual a la propiedad `target.damage` (En Rogue Engine, la vida no disminuye, el daño se acumula. La vida actual es `GetHPMax() - damage`).
3. Golpear a un enemigo remueve forzosamente el status `Sleep`.
4. Lanza el evento masivo `TriggerCharacterAbilityType(AbilityTrigger.OnDamaged, target)`, avisando a todas las pasivas de C# que este target ha sufrido dolor.
5. Comprueba si el personaje ha llegado a 0 HP y si es así, llama a `KillCharacter()`.

---

## 5. Sistema de Triggers Abiertos (Líneas 950 - 1060)

Esta es la zona cero de la "Iteración Centralizada". Cuando el juego quiere saber si alguien reacciona a algo, entra aquí.

### 5.1 `TriggerCharacterAbilityType()` (Línea `992`)
El gran muro de comprobación. Recibe un Enum de tipo de Trigger (Ej: `OnDeath`).
1. Comprueba si el personaje de origen está silenciado/incapacitado (`!caster.CanTriggerAbilities()`).
2. Usa un bucle `foreach` para mirar las habilidades innatas del personaje.
3. Usa un bucle `foreach` para mirar los buffs en juego (`cards_power`).
4. Usa un bucle `foreach` para mirar las reliquias (`cards_item`).
5. Por cada coincidencia en el tipo de trigger, envía el pedazo de código C# a la empaquetadora `TriggerAbility()`.
*Aviso técnico:* Como hemos estudiado extensamente, aquí radica el defecto de no poder invocar habilidades desde el mazo o descarte.

### 5.2 Embudos hacia la Cola (`TriggerAbility()`, Línea `1043`)
Antes de invocar ninguna partícula de Unity, esta función llama a `AreTriggerConditionsMet()`. Esta función evalúa los filtros lógicos que tú configuraste en el inspector (Ej: *"Solo disparar si mi vida es menor a 20"*). 
Si el filtro se cumple, invoca `resolve_queue.AddAbility(..., ResolveAbility)`.

---

## 6. La Cola de Resolución y Objetivos Abstractos (Líneas 1060 - 1240)

Una vez que el tiempo de pausa gráfica ha terminado, `ResolveQueue` desempuja la función encolada y llama a `ResolveAbility()` (Línea `1065`).

### 6.1 `ResolveAbility()` y el flujo de los Targets
En Rogue Engine, las Habilidades no ejecutan el efecto inmediatamente porque la carta de origen "No sabe a quién apunta". Ese misterio se resuelve aquí:

1. **Selector de Parada (`ResolveCardAbilitySelector`):** Línea `1087`. Si cuando configuraste la carta el Target de la habilidad era "Select Target", el código entra aquí y devuelve un `TRUE`. Esto aborta inmediatamente la resolución y levanta la Interfaz de Usuario de la flecha roja.
2. Si no hubo interrupción de ventana gráfica, llama en cascada a todos los métodos posibles de filtrado:
   - `ResolveCardAbilityPlayTarget()`: Envía el Daño/Efecto al enemigo que clickaste directamente.
   - `ResolveCardAbilityCharacters()`: Filtra si es Daño en Área (`AllCharacters`) enviando Daño individual en un bucle foreach por todos los enemigos que cumplan los requisitos.
   - `ResolveCardAbilityNoTarget()`: Si la carta era un Buff de robar que no requiere víctimas.

### 6.2 Encadenamiento (`AfterAbilityResolved()`, Línea `1210`)
Una vez ejecutada la curación o el daño, el flujo muere aquí.
Las habilidades complejas pueden usar "Efectos en Cadena". Esta función comprueba si `chain_abilities` tiene más efectos, y de ser así, llama a `TriggerAbility` re-encolándolos al final del tren, asegurando que las animaciones ocurran secuencialmente. 

---

## 7. Mantenimiento Pesado (`UpdateOngoing()`) (Líneas 1240 - 1440)
La función matemática más brutal del juego, explicada por fin en detalle. Se invoca casi 20 veces por turno (cada vez que juegas una carta, destruyes algo o recibes pasivas).

1. Llama a `ClearOngoing()` (Línea `1254`) a todos los elementos del tablero, devolviendo variables temporales de mana y reducción al estado pre-matemático.
2. Itera sobre Habilidades, Cartas de Poder, Items y **Cartas en la Mano** buscando triggers del tipo `AbilityTrigger.Ongoing`. (Líneas `1316` a `1430`).
3. Por cada trigger que acierte, llama a `DoOngoingEffects()`. Aquí es donde ocurren los milagros como *"Reduce el coste de esta carta en 1 por cada carta tipo Fuego en combate"*.
4. Por último, actualiza los modificadores directos (Armadura o Aumento de maná temporal) leyendo la lista `CardStatus` de todas las unidades.
