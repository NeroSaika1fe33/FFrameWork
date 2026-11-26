# 第三者コードとライセンス

## OpenSekai

本作品では、[OpenSekai](https://github.com/cubeww/OpenSekai) の次の二つのクラスを主な基盤として利用・改修しています。

| 主な利用元 | 利用内容 |
| --- | --- |
| [LiveLogic.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/Core/Live/LiveLogic.cs) | 演奏進行、入力・判定候補の処理、ノーツ実行の基盤 |
| [Converter.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/SUS/Converter.cs) | SUS 譜面解析、音符情報の変換と長条ノーツの構築 |

表示・入力などの周辺実装は設計上の参考にしています。一部のノーツ、表示、オブジェクトプール、描画および補助処理には、上流のコードを改修して利用した部分も含まれます。関連コードは主に Assets/Scripts/FFrameWork にあります。

上記リンクは確認時点のコミットを固定したものです。最初にコードを取り込んだ版を特定するものではありません。開発・改修には AI 支援も利用しています。

OpenSekai の著作権表示と MIT ライセンスの原文を以下に保持します。ライセンス原文は翻訳せず、そのまま掲載しています。

```text
MIT License

Copyright (c) 2026 OpenSekai contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## FMOD

FMOD by Firelight Technologies Pty Ltd.

本作品は FMOD for Unity 2.03.14 を使用しています。FMOD のソフトウェアとランタイムには独自のライセンスが適用されます。OpenSekai の MIT ライセンスには含まれません。配布パッケージ内の FMOD-LICENSE.txt もご確認ください。

## フォントとその他の素材

フォントの利用条件は FONT-LICENSE-Zebulon.txt および FONT-LICENSE-UnityDances.txt をご確認ください。これらのライセンス文書は原文を保持しています。

音楽、動画、ジャケット、画像、Prefab、シェーダー参考素材などは、それぞれの出典と利用条件に従います。OpenSekai の MIT ライセンスが、原作や第三者の素材を自動的に許諾するものではありません。本書は、すべてのプロジェクトファイルに一律の MIT ライセンスを付与するものではありません。
