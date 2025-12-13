# GuiBuddy

AI駆動のGUI自動化アシスタント

## 概要

GuiBuddyは、自然言語でウィンドウ内のUI要素を探索・操作できるAI駆動のデスクトップアプリケーションです。Google Gemini AIを活用し、ウィンドウのUI構造を解析してユーザーの質問に答え、該当する要素をビジュアルにハイライト表示します。

## 主な機能

- **AI駆動のUI探索**: 自然言語（日本語）でUI要素を探索
- **ビジュアルハイライト**: 該当するUI要素をオーバーレイで強調表示
- **リアルタイム更新**: チャット送信時に自動的にウィンドウ情報を再取得
- **セキュアなAPIキー管理**: Windows DPAPIを使用した安全な認証情報保存
- **マルチディスプレイ対応**: 複数モニター環境でも正確な座標変換

## システム要件

- **OS**: Windows 10/11
- **.NET**: .NET 10.0以降
- **APIキー**: Google Gemini API キー（[Google AI Studio](https://aistudio.google.com/)で取得）

## インストール

### ビルド方法

```bash
git clone <repository-url>
cd GuiBuddy
dotnet restore
dotnet build
```

### 実行

```bash
dotnet run --project GuiBuddy.App
```

## 使い方

### 1. APIキーの設定

1. アプリケーションを起動
2. ウィンドウ選択パネル右上の設定ボタン（⚙）をクリック
3. Google Gemini APIキーを入力して保存

### 2. ターゲットウィンドウの選択

1. ドロップダウンリストから操作対象のウィンドウを選択
2. 選択すると自動的にウィンドウ構造が解析されます

### 3. チャットでUI要素を探索

チャット欄に質問を入力：
- 「保存ボタンはどこ？」
- 「ファイルメニューを探して」
- 「検索欄を教えて」

AIが該当要素を見つけると、赤い枠でハイライト表示されます。

### キーボードショートカット

- **Shift + Enter**: メッセージ送信
- **Enter**: 改行
- **ESC**: ハイライト表示を消去

## プロジェクト構成

```
GuiBuddy/
├── GuiBuddy.App/              # WPFアプリケーション層
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Views/                # XAML Views
│   └── Services/             # アプリケーションサービス
├── GuiBuddy.Core/             # コアドメイン層
│   ├── Models/               # ドメインモデル
│   ├── Services/             # インターフェース定義
│   └── Repositories/         # リポジトリインターフェース
├── GuiBuddy.Infrastructure/   # インフラストラクチャ層
│   ├── AI/                   # AI統合（Gemini SDKクライアント）
│   ├── Services/             # 設定サービス等
│   └── Automation/          # UI Automation実装
└── GuiBuddy.Services/         # ビジネスロジック層
    ├── WindowService         # ウィンドウ管理
    └── UIMapService          # UI構造マッピング
```

## 技術スタック

- **UI Framework**: WPF (.NET 10.0)
- **MVVM**: ReactiveProperty
- **UI Automation**: UI Automation API
- **AI SDK**: Google Gen AI .NET SDK (Gemini 2.5 Flash)
- **DI Container**: Microsoft.Extensions.DependencyInjection

## バージョン履歴

### v0.1.0 (2025-12-13) - プロトタイプ初版
- ✅ AI駆動チャット機能
- ✅ UI要素のビジュアルハイライト
- ✅ Gemini API統合
- ✅ セキュアな設定管理
- ✅ リアルタイムUI情報更新

## トラブルシューティング

### APIキーが設定されていませんエラー
→ 設定画面（⚙）から有効なGemini APIキーを設定してください

### UI要素がハイライトされない
→ ターゲットウィンドウが最小化されていないか確認してください

### オーバーレイが表示されない
→ マルチディスプレイ環境の場合、ターゲットウィンドウと同じモニター上で確認してください

