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


## Aquarium系（水族館）

レイアウトと展示内容の正本は `AquariumModel`（純C#）が持ち、シーン上のオブジェクトはその写像として生成する。設置物をシーンに手置きしない。

### 設定アセット

| クラス名 | 役割 | 主なフィールド | 定義ファイル |
|---------|------|----------------|-------------|
| AquariumPieceData | 設置できるもの全ての定義の基底 | icon, prefab, PieceID(GUID) | AquariumPieceData.cs |
| GridPieceData | セルを占有する設置物の基底 | footprint, walkable | GridPieceData.cs |
| TankPieceData | 水槽。収容条件と遊泳ボリューム | supportedHabitations, maxDisplaySize, capacity, allowsSchool, swimArea | TankPieceData.cs |
| PedestalPieceData | 収集アイテムの展示台 | slotCount, acceptedTypes | PedestalPieceData.cs |
| PathPieceData | 通路 | （GridPieceDataのみ） | PathPieceData.cs |
| DecorPieceData | 自由配置の装飾 | placeableInsideTank | DecorPieceData.cs |
| AquariumFloorData | 間取り。設置可能セルを部屋単位で定義 | rooms | AquariumFloorData.cs |
| AquariumRoomDefinition | 部屋1つ分の範囲と解放条件 | roomID, origin, size, unlockedFromStart | **AquariumFloorData.cs** |
| AquariumPieceRegistry | 全AquariumPieceDataへの参照（ビルド版のGUID解決用） | pieces | AquariumPieceRegistry.cs |

### モデル（シーン非依存）

| クラス名 | 役割 | 主なメソッド | 関連クラス |
|---------|------|--------------|------------|
| AquariumModel | レイアウトと展示を束ねる正本 | TryExhibitEntity(), TryExhibitItem(), FindTanksAccepting() | AquariumLayoutModel, ExhibitModel |
| AquariumLayoutModel | 設置物の配置とセル占有の管理 | CanPlace(), TryPlace(), TryMove(), RemovePiece(), UnlockRoom() | PlacedPiece, PlacedDecor |
| PlacedPiece | グリッドに設置済みの設置物1つ分 | EnumerateCells(), GetWorldPosition() | GridPieceData |
| PlacedDecor | 自由配置された装飾1つ分 | MoveTo() | DecorPieceData |
| ExhibitModel | どの設置物に何を展示しているかを保持 | GetEntities(), GetItems(), AddEntity(), AddItem() | - |
| ExhibitRule | 展示可否の判定を一手に引き受ける（静的） | EvaluateEntity(), EvaluateItem(), GetCost(), Describe() | TankPieceData, PedestalPieceData |
| IEntityStock | 生物を何匹持っているかを答える | GetOwnedCount() | - |
| CapturedEntityStock | 捕獲済みの生物を所持数とみなす | GetOwnedCount() | SaveDataConverter |
| AquariumGrid | セルとワールド座標の変換、回転の計算（静的） | CellToWorld(), WorldToCell(), EnumerateCells() | - |

判定を足すときは `ExhibitRule` / `AquariumLayoutModel.CanPlace()` に集約する。UIのグレーアウトも同じ判定結果（`ExhibitRejection` / `PlacementRejection`）を根拠にする。

展示しても所持数は減らない。ただし館全体で同時に展示できる数は所持数までで、超える場合は `ExhibitRejection.StockExhausted` を返す。出し入れは自由にしたいが、1匹の生物を何台もの水槽へ並べられるのは避けるため。上限の判定は `AquariumModel.CanExhibitEntity()` が水槽単体の条件を通したあとに行う。

### ビュー（シーン）

モデル→シーンの一方向。設置物をシーンに手置きせず、必ず `AquariumBuilder` が生成する。

| クラス名 | 役割 | 主なメソッド | 関連クラス |
|---------|------|--------------|------------|
| AquariumSceneBootstrap | シーンの入口。セーブからモデルを起こす | Save() | AquariumSaveConverter, AquariumBuilder |
| AquariumBuilder | モデルの内容をシーンに生成・破棄する | Bind(), Unbind() | AquariumModel, AquariumPieceView |
| AquariumPieceView | 生成された設置物1つ分の基底 | Bind(), ClearContents() | PlacedPiece |
| TankView | 展示中の生物を生成し、遊泳範囲を内寸に合わせる | RefreshContents() | BaseSwimmer, SchoolController |
| PedestalView | 飾っているアイテムのモデルを並べる | RefreshContents() | ItemData |
| AquariumDebugPlacer | 編集UIができるまでの動作確認用の設置 | - | AquariumSceneBootstrap |

動作確認用シーン（`AquariumTest.unity`）とその生成物は `Blue > Aquarium > Setup Test Scene` が作る。**これらはコミットしない。** コミットするとビルダーを変えるたびに古くなり、コードとシーンが食い違ったまま気づけないため。クローン後は一度メニューを実行する。水槽の `TankPieceData` は手で調整する前提なので、一度作られたら上書きされず、コミット対象に含める。

`TankView` は `BaseSwimmer.SetRoamCenter/SetRoamArea/SetMigrationEnabled` に水槽の内寸を渡して閉じ込める。群れは `SchoolController` が毎フレーム個体へ縄張りを配るため、生成前に `_positionSphere` 系と `_spawnSphere` 系を内寸から割り当てる。

### 編集モード

俯瞰でレイアウトを決める。設置可否の判断は持たず、`AquariumLayoutModel.CanPlace()` の結果を色と文言に変えるだけ。

| クラス名 | 役割 | 主なメソッド | 関連クラス |
|---------|------|--------------|------------|
| AquariumModeController | 見学↔編集の切り替え。編集を抜けるときに保存する | Toggle(), SetMode() | AquariumSceneBootstrap |
| AquariumEditController | 入力をモデルへの設置・撤去・回転に落とす | （Update内） | AquariumLayoutModel, PlacementGhost |
| AquariumEditCamera | 床の一点を注視する俯瞰カメラ | Frame(), Pan(), Zoom() | - |
| PlacementGhost | 設置前の下見表示。可否を色で示す | SetPiece(), UpdatePlacement() | AquariumPieceData |
| AquariumEditInput | 編集モードの入力（暫定・旧Input） | - | - |

`AquariumEditInput` は暫定。Aquarium の InputActionMap には `Move` / `Look` しか無く、設置・撤去・回転にあたるアクションが無いため旧 Input で読んでいる。差し替えるときはこのクラスだけを直す。

### ランタイム

| クラス名 | 役割 | 主なメソッド | 関連クラス |
|---------|------|--------------|------------|
| AquariumPieceCache | GUIDからAquariumPieceDataを引く（静的） | GetPieceByGUID(), GetGUID() | AquariumPieceRegistry |
| AquariumSaveConverter | AquariumModelとセーブデータの相互変換（静的） | SaveAquarium(), LoadAquarium() | SaveManager, AquariumSaveData |


## Sound系

| クラス名 | 役割 | 主なフィールド | 主なメソッド | 関連クラス |
|---------|------|----------------|--------------|------------|
| SoundController | BGM・SE管理 | AudioPlayer | PlayBGM(), PlaySE() | - |

