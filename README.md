# プロジェクト概要

このプロジェクトは、Unityを用いたゲーム開発のベーステンプレートです。  
MVC構成・クリーンアーキテクチャを意識し、再利用性・拡張性を重視した構成となっています。

---

## 開発環境

| 項目 | 内容 |
|------|------|
| Unity バージョン | 6000.0.27f1 以降を推奨 |
| パッケージ | 以下を使用（必要に応じて追加） |
| | - Input System |
| | - TextMeshPro |

---

## ディレクトリ構成

詳細は [DirectoryStructure.md](DirectoryStructure.md) を参照してください。

```
Scripts/
├── Attack/        # 攻撃関連処理
├── Audio/         # サウンド管理（BGM・SEなど）
├── Entity/        # フィールド上の生物（敵・プレイヤー含む）
├── Input/         # 入力処理
├── Interface/     # インターフェース定義
├── Inventory/     # インベントリ関連処理
├── Item/          # アイテムデータ管理（ScriptableObjectなど）
├── Object/        # フィールド上の物体（ドア、ギミックなど）
├── Player/        # プレイヤー関連処理
└── UI/            # UI制御
    ├── Common/        # 共通UIパーツ（ボタン、ラベルなど）
    ├── Inventory/     # インベントリ画面関連
    ├── QuickSlot/     # クイックスロットUI関連
    ├── Screen/        # 各画面制御クラス（ポーズ画面など）
    └── Setting/       # 設定画面関連
```

---

## 命名規則

詳細は [DirectoryStructure.md](DirectoryStructure.md) を参照。  

### フォルダ・クラス命名
- PascalCase
- 機能単位で分類

### 変数命名
| 種別 | 命名規則 |
|------|---------|
| フィールド | camelCase |
| メソッド内変数 | snake_case |
| 定数 | CONSTANT_CASE |

---

## Git運用ルール

### ブランチ命名規則
`作業種類/年月日/内容`

#### 作業種類
| 名前 | 内容 |
|------|------|
| feature | 新機能追加 |
| fix     | バグ修正 |
| refactor| 機能に影響しないリファクタリング |

### ブランチ構成
| ブランチ名 | 役割 |
|------------|------|
| master     | 完成版 |
| develop    | 開発版（基本作業ブランチはここから分岐） |

---

## マージルール
1. 作業ブランチは develop から作成  
2. 作業完了後 develop にマージ  
3. 機能完成後 develop を master にマージ（完成版反映）

---

## その他
- ゲームの拡張時は原則この構成・ルールに従うこと