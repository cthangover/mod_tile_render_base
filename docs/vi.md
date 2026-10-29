# Tile Rendering — nền tảng chung đằng sau bản đồ ô

Tile Rendering vẽ các bản đồ ô. Nó nhận một bản mô tả bản đồ đã chuẩn bị — nền, vật thể, nhân vật, nguồn sáng — và biến thành hình ảnh, nhưng không quyết định bạn nhìn từ góc nào. Việc đó thuộc về một mod **kết xuất** nhỏ: **Top-down Tiles** hiển thị bản đồ thẳng từ trên xuống, **Isometric Tiles** hiển thị thành lưới hình thoi. Cùng một bản đồ, cùng sự kiện và save đều chạy với cả hai.

Bạn không dùng trực tiếp mod này. Nó nằm trong mọi game có bản đồ ô — ví dụ RPG Plus — cùng với ít nhất một trình kết xuất.

![Cùng một bản đồ làng được hai trình kết xuất vẽ: ô vuông nhìn từ trên ở bên trái, đẳng cự ở bên phải.](assets/same-map-two-projections.jpg)

## Ai làm gì

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

- **Lối chơi vẫn vuông vắn.** Di chuyển, va chạm, sự kiện và save đều dùng lưới logic ở mọi phép chiếu. Trình kết xuất chỉ thay đổi nơi vẽ từng thứ và ô nào nhận cú nhấp chuột.
- **Một đường vẽ chung cho game và trình soạn thảo.** Trình sửa bản đồ RPG vẽ khung vẽ bằng đúng trình kết xuất mà game dùng.
- **Ánh sáng và thời gian trong ngày** tính theo đơn vị ô, nên một ngọn đèn soi sáng cùng những ô ấy ở cả hai phép chiếu; ô và nhân vật dùng chung một vật liệu hòa trộn thời gian trong ngày với nguồn sáng điểm.

## Chọn phép chiếu

Phép chiếu là một cài đặt của game, **Phép chiếu bản đồ** (`tile.renderer`), nằm trong nhóm **Thế giới** của cài đặt bộ sưu tập. Các lựa chọn là **Nhìn từ trên** (`topdown`) và **Đẳng cự** (`isometric`); RPG Plus dùng `topdown`. Để đổi, mở **Mod & Bộ sưu tập → Bộ sưu tập → RPG Plus → Sửa sơ đồ**, đặt phép chiếu và bảo đảm mod kết xuất tương ứng có trong game.

- Nếu cài đặt nêu tên một trình kết xuất không có trong game, bản đồ sẽ không chuyển sang trình khác; nhật ký sẽ liệt kê các trình kết xuất khả dụng.
- Nếu cài đặt bỏ trống và chỉ cài đúng một trình kết xuất, trình đó được dùng.
- Cho một lần chạy, `--tile-renderer=isometric` (hoặc `topdown`) trên dòng lệnh đè lên cài đặt.

## Dành cho tác giả mod: phép chiếu mới

Mod kết xuất phụ thuộc vào `tile_render_base` và chứa một lớp cài đặt `ITileRendererFactory`:

- `Id` — giá trị người chơi chọn trong **Phép chiếu bản đồ**, ví dụ `hex`;
- `Projection(tileSize)` — phép ánh xạ ô sang điểm ảnh mà game, trình soạn thảo và chức năng chọn bằng chuột đều dùng;
- `Create()` — một khung xem bản đồ mới; cách trình bày khung vẽ chung có thể tái sử dụng với phép chiếu của bạn.

Nhà máy được tự động nhận khi mod nạp và bị gỡ khi mod dỡ. Để phép chiếu xuất hiện trong **Phép chiếu bản đồ**, hãy thêm một lựa chọn vào `collection_settings.json` của mod, như Top-down Tiles đã làm:

```json
{
  "options": [
    { "key": "tile.renderer", "value": "topdown", "name": "collection_settings/topdown" }
  ]
}
```

Khóa `name` được dịch trong `locale/collection_settings_<lang>.properties`.

## Khi có trục trặc

1. **Bản đồ trống trơn và nhật ký báo trình kết xuất không khả dụng.** **Phép chiếu bản đồ** của game đang nêu một mod kết xuất không có trong game; hãy thêm nó vào hoặc đổi cài đặt.
