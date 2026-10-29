# Tile Rendering — 图块地图背后的共享引擎

Tile Rendering 负责绘制图块地图。它接收一份准备好的地图描述——地面、物件、角色、光源——并把它变成画面，但并不决定你从哪个角度看。这个选择属于一个很小的**渲染器**模组：**Top-down Tiles** 从正上方显示地图，**Isometric Tiles** 则把它显示为菱形网格。同一张地图、同一批事件和存档，在两者之下都能正常工作。

你不会直接使用这个模组。它连同至少一个渲染器一起，存在于每个带图块地图的游戏中，例如 RPG Plus。

![同一张村庄地图由两个渲染器分别绘制：左边是方形俯视，右边是等距。](assets/same-map-two-projections.jpg)

## 分工

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

- **玩法始终是方格的。** 无论哪种投影，移动、碰撞、事件和存档都使用逻辑网格。渲染器只改变东西画在哪里、点击落在哪个格子上。
- **游戏和编辑器共用一条绘制路径。** RPG 地图编辑器用与游戏相同的渲染器绘制画布。
- **光照与昼夜**以格子为单位给出，所以一盏灯在两种投影里照亮的格子相同；图块与角色共用一个材质，其中混合了昼夜变化和点光源。

## 选择投影

投影是游戏的一项设置，即合集设置中**世界**组里的**地图投影**（`tile.renderer`）。可选值为**俯视**（`topdown`）和**等距**（`isometric`）；RPG Plus 使用 `topdown`。要修改它，请打开**模组与合集 → 合集 → RPG Plus → 编辑结构**，设置投影，并确认对应的渲染器模组已包含在游戏中。

- 如果设置指向的渲染器不在游戏里，地图不会退回到别的渲染器；日志会列出可用的渲染器。
- 如果设置为空且恰好只安装了一个渲染器，就用它。
- 只想用一次时，可以在命令行加 `--tile-renderer=isometric`（或 `topdown`）覆盖设置。

## 给模组作者：新的投影

渲染器模组依赖 `tile_render_base`，并包含一个实现 `ITileRendererFactory` 的类：

- `Id`——玩家在**地图投影**里选择的值，例如 `hex`；
- `Projection(tileSize)`——“格子到像素”的映射，游戏、编辑器和点击拾取都使用它；
- `Create()`——一个新的地图视图；共享的画布表现可以在你的投影下复用。

模组加载时，工厂会被自动发现；卸载时被移除。要让投影出现在**地图投影**里，请像 Top-down Tiles 那样，在自己的 `collection_settings.json` 中添加一个选项：

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

`name` 键在 `locale/collection_settings_<lang>.properties` 中翻译。

## 如果遇到问题

1. **地图是空的，日志说渲染器不可用。** 游戏的**地图投影**指定了一个不在游戏中的渲染器模组；把它加进来，或者改一下设置。
