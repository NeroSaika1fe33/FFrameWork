# Third Party Notices

## OpenSekai

本项目使用并修改了 OpenSekai 的谱面解析、基础音符与判定、对象池、长条绘制及部分显示和工具代码。相关实现主要位于 `Assets/Scripts/FFrameWork`。当前目录名和命名空间不表示这些基础实现均为本项目原创。

- 上游项目：https://github.com/cubeww/OpenSekai
- 许可证来源：https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/LICENSE
- 上述提交用于核对许可证，不代表已经确定了最初引入代码的提交版本。
- 本项目开发与改造中使用了 AI，包括部分 UI 实现、问题排查和代码修改。

以下为上游 MIT 许可证全文，适用于其相应代码：

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

本项目依赖 FMOD for Unity 2.03.14。FMOD 软件与运行库遵循其自己的许可，不包含在 OpenSekai 的 MIT 授权内。源码仓库排除 `Assets/Plugins/FMOD`，开发者需单独导入插件。

## 音乐与其他资源

音乐、视频、封面、字体、贴图、Prefab 和 Shader 参考资源按各自的来源和许可处理。OpenSekai 的 MIT 声明不自动授权原作或其他第三方的资源。本文件不对全部项目文件作统一的 MIT 授权。