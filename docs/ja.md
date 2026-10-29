# Tile Rendering — タイルマップの共通エンジン

Tile Rendering はタイルマップを描画します。用意されたマップの記述——地面、物体、キャラクター、光——を受け取って絵に変えますが、どの角度から見るかは決めません。それを決めるのは小さな**レンダラー** Mod です。**Top-down Tiles** はマップを真上から、**Isometric Tiles** はひし形の格子で示します。同じマップ、同じイベント、同じセーブが、どちらでもそのまま働きます。

この Mod を直接使うことはありません。タイルマップのあるゲーム——たとえば RPG Plus——には、レンダラーを最低ひとつと一緒に必ず入っています。

![同じ村のマップを 2 つのレンダラーで描いたもの: 左が正方形の見下ろし、右がアイソメトリック。](assets/same-map-two-projections.jpg)

## 役割分担

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

- **ゲームプレイは常に正方形です。** 移動、衝突、イベント、セーブは、どの投影でも論理グリッドを使います。レンダラーが変えるのは、何をどこに描くかと、クリックがどのマスに当たるかだけです。
- **ゲームとエディターで描画経路はひとつ。** RPG マップエディターは、ゲームと同じレンダラーでキャンバスを描きます。
- **光と時間の流れ**はマス単位で与えられるため、ランプはどちらの投影でも同じマスを照らします。タイルとキャラクターは、時間の流れと点光源を混ぜる 1 つのマテリアルを共有します。

## 投影を選ぶ

投影はゲームの設定、**マップの投影**（`tile.renderer`）で、コレクション設定の **ワールド** グループにあります。選択肢は **見下ろし**（`topdown`）と **アイソメトリック**（`isometric`）です。RPG Plus は `topdown` を使います。変えるには **MOD とコレクション → コレクション → RPG Plus → 構成を編集** を開き、投影を設定して、対応する描画 Mod がゲームに含まれていることを確認してください。

- 設定がゲームに存在しないレンダラーを指している場合、マップは別のレンダラーに切り替わりません。利用できるレンダラーがログに並びます。
- 設定が空で、レンダラーがちょうど 1 つだけインストールされていれば、それが使われます。
- 1 回だけの実行なら、コマンドラインの `--tile-renderer=isometric`（または `topdown`）が設定に優先します。

## Mod 作者向け: 新しい投影

レンダラー Mod は `tile_render_base` に依存し、`ITileRendererFactory` を実装するクラスを 1 つ持ちます。

- `Id` — プレイヤーが **マップの投影** で選ぶ値。たとえば `hex`。
- `Projection(tileSize)` — マスから画面ピクセルへの対応付け。ゲーム・エディター・クリック判定がこれを使います。
- `Create()` — 新しいマップビュー。共通のキャンバス表示は自分の投影でも再利用できます。

ファクトリは Mod の読み込み時に自動で見つかり、Mod を外すと削除されます。投影を **マップの投影** に並べるには、Top-down Tiles のように自分の Mod の `collection_settings.json` に選択肢を追加します。

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

`name` キーは `locale/collection_settings_<lang>.properties` で翻訳されます。

## うまくいかないとき

1. **マップが空で、ログにレンダラーが利用できないと出る。** ゲームの **マップの投影** が、ゲームに含まれない描画 Mod を指しています。追加するか、設定を変えてください。
