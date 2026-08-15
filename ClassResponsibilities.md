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


## World系（地形）

制作手順は [StageAuthoring.md](StageAuthoring.md) を参照。手で編集するアセットは `StageGeneratorSettings`（地形の形）と `StageRecipe`（ベイクの入力）の2つだけで、残りは生成物。

### 設定アセット

| クラス名 | 役割 | 主なフィールド | 定義ファイル |
|---------|------|----------------|-------------|
| StageGeneratorSettings | 地形の形を決める生成パラメータ | 断面(shelf/slope/basin)、起伏、regionProfiles、features | StageGeneratorSettings.cs |
| StageRecipe | ベイクの入力一式 | layout, heightmap, biomes, scatterLayers | StageRecipe.cs |
| BiomeLayerBinding | TerrainLayer 1枚と、それを塗るマスク・チャンネルの対応 | terrainLayer, mask, channel, weight | **StageRecipe.cs** |
| StageRegionProfile | リージョン1つ分の地形の性格 | ridgeScale/Height, depthBias, talusAngle | StageRegionProfile.cs |
| StageFeature | 座標指定で置く造作（海丘・海嶺・海溝） | shape, blend, position, path, radius, height | StageFeature.cs |
| StageTileLayout | タイル分割とワールド座標の変換 | worldSize, tilesPerAxis, min/maxHeight | StageTileLayout.cs |

### ランタイム

| クラス名 | 役割 | 主なメソッド | 関連クラス |
|---------|------|--------------|------------|
| StageRegionField | 位置からリージョンと重みを求める | SampleDistances(), ToWeights() | StageGeneratorSettings |
| StageTileManifest | ベイク結果の一覧（生成物） | Find() | StageLoader |
| StageLoader | タイルシーンの加算ロード／アンロード | SetMode() | StageTileManifest |
| StageTile | タイル1枚の識別情報 | Setup() | StageLoader |

### エディタ

| クラス名 | 役割 | 入口 |
|---------|------|------|
| StageScaffolder | ステージのフォルダとレシピの雛形を作る | Blue > World > Stage Scaffolder |
| StageHeightmapGenerator | ハイトマップとマスクを生成して Source/ に書き出す | 設定アセットの Generate ボタン |
| StagePreviewWindow | 生成結果の俯瞰・断面表示、リージョン割当、造作の配置 | Blue > World > Stage Preview |
| StageTerrainBaker | レシピから TerrainData とタイルシーンを生成 | レシピの Bake ボタン |
| StageScatterBaker | 散布物をベイク | レシピの Bake Scatter ボタン |
| StageSourceTextureImporter | Source/ のテクスチャのインポート設定を強制 | 自動（AssetPostprocessor） |


## Sound系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| SoundController | BGM・SE管理 | AudioPlayer | PlayBGM(), PlaySE() | - |

