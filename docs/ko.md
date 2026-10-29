# Tile Rendering — 타일 맵 뒤의 공유 엔진

Tile Rendering은 타일 맵을 그립니다. 준비된 맵 묘사 — 지면, 물건, 캐릭터, 빛 — 를 받아 그림으로 바꾸지만, 어느 각도에서 볼지는 정하지 않습니다. 그 선택은 아주 작은 **렌더러** 모드의 몫입니다. **Top-down Tiles**는 맵을 곧바로 위에서 보여 주고, **Isometric Tiles**는 마름모 격자로 보여 줍니다. 같은 맵, 같은 이벤트, 같은 저장이 어느 쪽에서든 그대로 작동합니다.

이 모드를 직접 쓸 일은 없습니다. 타일 맵이 있는 모든 게임 — 예컨대 RPG Plus — 에는 최소 하나의 렌더러와 함께 이 모드가 들어 있습니다.

![같은 마을 맵을 두 렌더러로 그린 모습: 왼쪽은 정사각형 탑다운, 오른쪽은 아이소메트릭.](assets/same-map-two-projections.jpg)

## 누가 무엇을 맡는가

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

- **게임플레이는 언제나 정사각형입니다.** 이동, 충돌, 이벤트, 저장은 모든 투영에서 논리 격자를 사용합니다. 렌더러가 바꾸는 것은 무엇을 어디에 그리는지, 클릭이 어느 칸에 떨어지는지뿐입니다.
- **게임과 에디터가 하나의 그리기 경로를 씁니다.** RPG 맵 에디터는 게임과 같은 렌더러로 캔버스를 그립니다.
- **빛과 시간대**는 칸 단위로 주어지므로, 램프는 두 투영에서 같은 칸을 밝힙니다. 타일과 캐릭터는 시간대와 점광을 섞는 하나의 머티리얼을 공유합니다.

## 투영 고르기

투영은 게임 설정의 하나로, 컬렉션 설정의 **월드** 그룹에 있는 **지도 투영**(`tile.renderer`)입니다. 선택지는 **탑다운**(`topdown`)과 **아이소메트릭**(`isometric`)이며, RPG Plus는 `topdown`을 씁니다. 바꾸려면 **모드 및 컬렉션 → 컬렉션 → RPG Plus → 구성 편집**을 열어 투영을 정하고, 해당 렌더러 모드가 게임에 포함되어 있는지 확인하세요.

- 설정이 게임에 없는 렌더러를 가리키면 맵이 다른 렌더러로 넘어가지 않고, 로그에 사용 가능한 렌더러가 나열됩니다.
- 설정이 비어 있고 렌더러가 정확히 하나만 설치되어 있다면 그것을 씁니다.
- 한 번만 실행하려면 명령줄에서 `--tile-renderer=isometric`(또는 `topdown`)이 설정을 덮어씁니다.

## 모드 작성자용: 새 투영

렌더러 모드는 `tile_render_base`에 의존하며 `ITileRendererFactory`를 구현하는 클래스 하나를 담습니다.

- `Id` — 플레이어가 **지도 투영**에서 고르는 값. 예: `hex`.
- `Projection(tileSize)` — 칸을 화면 픽셀로 옮기는 대응. 게임·에디터·클릭 판정이 이것을 씁니다.
- `Create()` — 새 맵 뷰. 공용 캔버스 표현은 자기 투영에서도 재사용할 수 있습니다.

팩토리는 모드가 로드될 때 자동으로 발견되고, 모드를 내리면 제거됩니다. 투영을 **지도 투영**에 넣으려면 Top-down Tiles처럼 자기 모드의 `collection_settings.json`에 옵션을 추가하세요.

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

`name` 키는 `locale/collection_settings_<lang>.properties`에서 번역됩니다.

## 문제가 생겼을 때

1. **맵이 비어 있고 로그에 렌더러를 쓸 수 없다고 나온다.** 게임의 **지도 투영**이 게임에 없는 렌더러 모드를 가리킵니다. 그 모드를 넣거나 설정을 바꾸세요.
