# Tile Rendering — the shared engine behind tile maps

Tile Rendering draws tile maps. It takes a prepared description of a map — ground, objects, characters, lights — and turns it into a picture, but it does not decide the angle you look from. That choice belongs to a small **renderer** mod: **Top-down Tiles** shows the map straight from above, **Isometric Tiles** shows it as a diamond grid. The same map, events and saves work with either one.

You do not use this mod directly. It is part of every game with tile maps, such as RPG Plus, together with at least one renderer.

![The same village map drawn by both renderers: square top-down on the left, isometric on the right.](assets/same-map-two-projections.jpg)

## Who does what

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

- **Gameplay stays square.** Movement, collision, events and saves use the logical grid in every projection. A renderer only changes where things are drawn and which cell a click lands on.
- **One drawing path for game and editor.** The RPG map editor draws its canvas with the same renderer as the game.
- **Lights and time of day** are given in cell units, so a lamp lights the same cells in both projections; the tiles and characters share one material that blends the time of day and point lights.

## Choosing the projection

The projection is a setting of the game, **Map projection** (`tile.renderer`), in the **World** group of the collection's settings. The choices are **Top-down** (`topdown`) and **Isometric** (`isometric`); RPG Plus uses `topdown`. To change it, open **Mods & Collections → Collections → RPG Plus → Edit scheme**, set the projection, and make sure the matching renderer mod is in the game.

- If the setting names a renderer that is not in the game, the map does not fall back to another one; the log lists the available renderers.
- If the setting is empty and exactly one renderer is installed, that one is used.
- For a single run, `--tile-renderer=isometric` (or `topdown`) on the command line overrides the setting.

## For mod authors: a new projection

A renderer mod depends on `tile_render_base` and contains one class implementing `ITileRendererFactory`:

- `Id` — the value players choose in **Map projection**, for example `hex`;
- `Projection(tileSize)` — the cell-to-pixel mapping used by the game, the editor and picking;
- `Create()` — a new map view; the shared canvas presentation can be reused with your projection.

The factory is found automatically when the mod loads and removed when it unloads. To offer the projection in **Map projection**, add an option in your mod's `collection_settings.json`, as Top-down Tiles does:

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

The `name` key is translated in `locale/collection_settings_<lang>.properties`.

## If something goes wrong

1. **The map is empty and the log says the renderer is unavailable.** The game's **Map projection** names a renderer mod that is not part of the game; add it or change the setting.
