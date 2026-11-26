# SonoriX / FFrameWork

Unity で制作したリズムゲームです。固定キーのリズム入力に、位置と幅を変える判定枠、FMOD による音響効果、シェーダーによる映像演出を組み合わせています。

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

詳しいルールはゲーム内の操作ガイドをご覧ください。

## OpenSekai の利用と参照

本作品は [OpenSekai](https://github.com/cubeww/OpenSekai) のコードを基盤として、独自の操作方式やゲームルールに合わせた追加・改修を行っています。主なコード利用元は、次の二つのクラスです。

- **[LiveLogic.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/Core/Live/LiveLogic.cs)**：演奏進行、入力と判定候補の対応付け、ノーツ処理の基盤として利用・改修しています。
- **[Converter.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/SUS/Converter.cs)**：SUS 譜面解析の基盤として利用し、本作品向けのノーツや記述方法に合わせて拡張しています。

表示・入力などの周辺クラスは、設計や処理の参考にしています。また、一部の表示、ノーツ、オブジェクトプール、補助処理には、上流コードを改修して利用した実装も含まれます。FFrameWork という名称は、これらの基盤をすべて独力で新規実装したことを示すものではありません。

開発には AI 支援を利用しており、UI 実装や不具合の調査・修正にも活用しています。著作権表示と MIT ライセンスの全文は [第三者コードとライセンス](THIRD_PARTY_NOTICES.md) に記載しています。

## ソースコードと開発環境

現在は Windows 向けのビルド済みデモを公開しています。ソースコードは整理中で、まだこのリポジトリへアップロードしていません。

- Unity：6000.4.5f1
- FMOD for Unity：2.03.14
- メインシーン：Assets/Scenes/xxAxxLive.unity
- ソース公開時の音声・動画・Bank ファイルは Git LFS で管理する予定です。

FMOD by Firelight Technologies Pty Ltd.

音楽、動画、画像、フォントなどは、それぞれの権利と利用条件に従います。コードの MIT ライセンスは、第三者の素材を一括して許諾するものではありません。
