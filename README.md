# SonoriX / FFrameWork

Unity で制作したリズムゲーム
制作期間：五ヶ月（2026年5月～2026年10月）
制作人数：一人
音楽と譜面：譜面一部友達に頼んで、音楽は先生と友達から貰った学生作品

## ダウンロードと起動

[Windows 版 v0.1.0 をダウンロード](https://github.com/NeroSaika1fe33/FFrameWork/releases/download/v0.1.0/GitRelease.zip)

GitRelease.zip をすべて展開し、GitRelease フォルダー内の My project (1).exe を実行してください。Data フォルダー、UnityPlayer.dll、その他の同梱ファイルは、実行ファイルと一緒に保管してください。プレイに Unity Editor や FMOD Studio のインストールは必要ありません。

## 基本操作

| 入力 | 操作 |
| --- | --- |
| A / S / K / L | 固定レーンのリズム入力 |
| Space またはマウスボタン | フリーレーンの入力 |
| マウスの左右移動 | 判定枠の位置を操作 |
| マウスの上下移動 | 幅変更に関するノーツの方向入力 |

本編を十分にお楽しみいただくため、専用コントローラーのご使用を推奨します。
詳しいルールはゲーム内の操作ガイドをご覧ください。

## OpenSekai の利用と参照

本作品は [OpenSekai](https://github.com/cubeww/OpenSekai) のコードを基盤として、独自の操作方式やゲームルールに合わせた追加・改修を行っています。主なコード利用元は、次の二つのクラスです。

- **[LiveLogic.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/Core/Live/LiveLogic.cs)**：演奏進行、入力と判定候補の対応付け、ノーツ処理の基盤として利用・改修しています。
- **[Converter.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/SUS/Converter.cs)**：SUS 譜面解析の基盤として利用し、本作品向けのノーツや記述方法に合わせて拡張しています。

表示・入力などの周辺クラスは、設計や処理の参考にしています。

開発には AI 支援を利用しており、UI 実装や不具合の調査・修正にも活用しています。著作権表示と MIT ライセンスの全文は [第三者コードとライセンス](THIRD_PARTY_NOTICES.md) に記載しています。

## ソースコードと開発環境

Windows 向けのビルド済みデモと、Unity プロジェクトのソースコードを公開しています。実行版は Releases、実装は Assets/Scripts/FFrameWork をご覧ください。

- Unity：6000.4.5f1
- FMOD for Unity：2.03.14
- メインシーン：Assets/Scenes/xxAxxLive.unity

### プロジェクトを開く

1. Git LFS をインストールし、このリポジトリを clone して git lfs pull を実行します。
2. Unity Hub で、Assets・Packages・ProjectSettings があるリポジトリ直下を開きます。
3. FMOD for Unity 2.03.14 を Assets/Plugins/FMOD に導入します。FMOD プラグイン本体は、このソースコードには含めていません。
4. Unity の読み込みとコンパイルが完了したら、Assets/Scenes/xxAxxLive.unity を開きます。

.sln と .csproj は Unity が生成するため、リポジトリには含めていません。Unity から C# スクリプトを Rider で開くと、IDE 用のプロジェクトを生成できます。

### 主なディレクトリ

- Assets/Scripts/FFrameWork：譜面解析、演奏進行、入力、判定、音響と表示の実装。
- Assets/Charts：譜面テキスト。
- Assets/StreamingAssets：実行時に使用する譜面と FMOD Bank。
- Assets/Shader：シェーダー。
- Packages / ProjectSettings：Unity の依存関係と設定。
- [作品の工夫した点](Docs/Interview/06-作品の工夫した点.md)：五つの工夫と、SonoriX_Rebuild で進めている自作譜面解析器の開発について。

Library・Temp・IDE のキャッシュ、作業用の出力フォルダー、プレイ記録は含めていません。FMOD Studio の編集用音声プロジェクトは、このリポジトリには含まれていません。

音楽、動画、画像、フォントなどは、それぞれの権利と利用条件に従います。コードの MIT ライセンスは、第三者の素材を一括して許諾するものではありません。

### 他の資料

プレイ動画：https://drive.google.com/drive/folders/1-A5zfzy5HMVTP8RKAiCYtJ0z7NjE70-V?usp=drive_link  
専用コントローラーと企画の資料：https://drive.google.com/drive/folders/1A1tMknwVeTgXUlJyg70lijrpvbUSBoBz?usp=drive_link
