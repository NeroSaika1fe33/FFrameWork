SonoriX v0.1.0 Windows デモ版

【起動方法】
GitRelease.zip をすべて展開してください。
GitRelease フォルダー内の My project (1).exe を実行してください。
Data フォルダー、UnityPlayer.dll、その他の同梱ファイルは一緒に保管してください。
プレイに Unity Editor や FMOD Studio のインストールは必要ありません。

【基本操作】
A / S / K / L：固定レーンのリズム入力
Space またはマウスボタン：フリーレーンの入力
マウスの左右移動：判定枠の位置を操作
マウスの上下移動：幅変更に関するノーツの方向入力
詳しいルールはゲーム内の操作ガイドをご覧ください。

【主なコード利用元】
OpenSekai の LiveLogic と Converter を主な基盤として利用・改修しています。
LiveLogic（演奏進行・入力判定の基盤）：
https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/Core/Live/LiveLogic.cs
Converter（SUS 譜面解析）：
https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/SUS/Converter.cs

表示・入力などの周辺実装を参考にし、一部の表示・プール・補助コードも改修して利用しています。
開発には AI 支援を利用しています。
著作権表示と MIT ライセンス全文は THIRD_PARTY_NOTICES.md をご確認ください。

FMOD by Firelight Technologies Pty Ltd.
FMOD の利用条件：FMOD-LICENSE.txt
クレジット：CREDITS.html
