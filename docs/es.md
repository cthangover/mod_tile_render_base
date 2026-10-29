# Tile Rendering — el motor común detrás de los mapas de tiles

Tile Rendering dibuja los mapas de tiles. Recibe una descripción del mapa ya preparada —suelo, objetos, personajes, luces— y la convierte en una imagen, pero no decide desde qué ángulo se mira. Esa elección corresponde a un pequeño mod de **renderizado**: **Top-down Tiles** muestra el mapa directamente desde arriba y **Isometric Tiles**, como una cuadrícula de rombos. El mismo mapa, los mismos eventos y las mismas partidas guardadas funcionan con cualquiera de los dos.

Este mod no se usa directamente: forma parte de todo juego con mapas de tiles, como RPG Plus, junto con al menos un renderizador.

![El mismo mapa de la aldea dibujado por los dos renderizadores: vista superior cuadrada a la izquierda, isométrica a la derecha.](assets/same-map-two-projections.jpg)

## Quién hace qué

```text
 content mods            maps, tilesets, events, characters (rpg_demo_game, village_pack…)
      │
 RPG mod (rpg)           walking, collisions, events, the camera target, lighting rules
      │  prepares a "render scene": visuals in cell space, lights, the current frame
      ▼
 Tile Rendering          shared drawing: ground chunks, autotiles, sorting, shading,
 (this mod)              asset loading, the interaction marker, picking
      │  asks the selected renderer for a projection
      ▼
 renderer mod            Square (topdown)  ·  Isometric (isometric)
                         how a cell maps to screen pixels
```

- **La jugabilidad sigue siendo cuadrada.** El movimiento, las colisiones, los eventos y el guardado usan la cuadrícula lógica en todas las proyecciones. Un renderizador solo cambia dónde se dibuja cada cosa y sobre qué casilla cae un clic.
- **Un solo camino de dibujo para el juego y el editor.** El editor de mapas RPG dibuja su lienzo con el mismo renderizador que el juego.
- **Las luces y la hora del día** se dan en unidades de casilla, así que una lámpara ilumina las mismas casillas en ambas proyecciones; los tiles y los personajes comparten un material que mezcla la hora del día con las luces puntuales.

## Elegir la proyección

La proyección es un ajuste del juego, **Proyección del mapa** (`tile.renderer`), en el grupo **Mundo** de los ajustes de la colección. Las opciones son **Vista superior** (`topdown`) e **Isométrica** (`isometric`); RPG Plus usa `topdown`. Para cambiarla, abre **Mods → Colecciones → RPG Plus → Editar esquema**, elige la proyección y asegúrate de que el mod de renderizado correspondiente esté en el juego.

- Si el ajuste nombra un renderizador que no está en el juego, el mapa no recurre a otro; el registro enumera los renderizadores disponibles.
- Si el ajuste está vacío y hay exactamente un renderizador instalado, se usa ese.
- Para una sola partida, `--tile-renderer=isometric` (o `topdown`) en la línea de comandos anula el ajuste.

## Para autores de mods: una proyección nueva

Un mod de renderizado depende de `tile_render_base` y contiene una clase que implementa `ITileRendererFactory`:

- `Id` — el valor que los jugadores eligen en **Proyección del mapa**, por ejemplo `hex`;
- `Projection(tileSize)` — el mapeo de casilla a píxeles que usan el juego, el editor y la selección con el ratón;
- `Create()` — una vista de mapa nueva; la presentación común del lienzo puede reutilizarse con tu proyección.

La fábrica se detecta automáticamente al cargar el mod y se retira al descargarlo. Para ofrecer la proyección en **Proyección del mapa**, añade una opción al `collection_settings.json` de tu mod, como hace Top-down Tiles:

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

La clave `name` se traduce en `locale/collection_settings_<lang>.properties`.

## Si algo falla

1. **El mapa está vacío y el registro dice que el renderizador no está disponible.** La **Proyección del mapa** del juego nombra un mod de renderizado que no forma parte del juego; instálalo o cambia el ajuste.
