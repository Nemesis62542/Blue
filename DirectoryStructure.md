# Directory Structure

プロジェクトのディレクトリ構成および命名ルールについて記載します。

## ディレクトリ構成

```
Scripts/
├── Attack/                   # 攻撃関連処理
├── Audio/                    # サウンド管理（BGM・SEなど）
├── Entity/                   # フィールド上の生物（敵・プレイヤー含む）
├── Input/                    # 入力処理
├── Interface/                # インターフェース定義
├── Inventory/                # インベントリ関連処理
├── Item/                     # アイテムデータ管理（ScriptableObjectなど）
├── Object/                   # フィールド上の物体（ドア、ギミックなど）
├── Player/                   # プレイヤー関連処理
└── UI/                       # UI制御
    ├── Common/               # 共通UIパーツ（ボタン、ラベルなど）
    ├── Inventory/            # インベントリ画面関連
    ├── QuickSlot/            # クイックスロットUI関連
    ├── Screen/               # 各画面制御クラス（ポーズ画面など）
    └── Setting/              # 設定画面関連
```


## 命名ルール

| 種別 | 命名規則 | 備考 |
|------|---------|------|
| フォルダ名 | PascalCase | 機能単位で分類（Entity, Inventory, UIなど） |
| クラス名 | PascalCase | 機能名＋役割名（PlayerController, InventoryModelなど） |
| 変数名（クラス内） | camelCase | 基本 private、Inspector表示は [SerializeField] private |
| メソッド名 | PascalCase | 基本 private、外部公開時は public 許容 |
| 定数 | CONSTANT_CASE | 不変値（const / readonly） |
| メソッド内変数 | snake_case | ローカル変数に使用 |
| namespace | {プロジェクト名}.{領域名} | 例：ProjectName.Entity |

