# Class Responsibilities

プロジェクト内の主要クラスの責務を一覧化します。

## Entity系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| BaseEntityModel | 生物の状態（HP/攻撃力）管理 | status（HP等） | Damage(), OnDead() | - |
| BaseEntityController<TModel, TView> | Entity制御の基底 | model, view | Initialize(), Update() | BaseEntityModel, BaseEntityView |
| BaseEntityView | 生物の見た目・演出制御 | - | PlayAnimation(), PlayEffect() | - |
| PlayerModel | プレイヤー専用の状態管理 | - | - | BaseEntityModel継承 |
| PlayerController | プレイヤー制御 | inventoryModel, quickSlotHandler | Interact(), Attack(), UseItem() | BaseEntityController継承 |
| PlayerView | プレイヤー専用の演出 | - | - | BaseEntityView継承 |


## Inventory系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| InventoryModel | アイテム所持情報管理 | item_list | AddItem(), RemoveItem() | PlayerController |
| InventoryView | インベントリ画面表示制御 | - | Initialize(), UpdateInventoryUI() | InventoryModel |
| QuickSlotHandler | クイックスロット管理 | quick_slot_list | SetItemToSlot(), GetSelectedItem() | InventoryModel, PlayerController |


## UI系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| UIController | UI画面状態の管理 | screen_dict（CanvasGroup管理） | ShowScreen(), HideAllScreen() | PlayerController |


## Sound系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| SoundController | BGM・SE管理 | AudioPlayer | PlayBGM(), PlaySE() | - |

