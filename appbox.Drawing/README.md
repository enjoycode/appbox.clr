本工程使用SkiaSharp模拟System.Drawing的API

# 编译Skia支持Pdf嵌入字体子集
```bash
git clone https://github.com/mono/SkiaSharp.git
cd SkiaSharp
git submodule update --init --recursive
```
> 另修改tools/packages.config <package id="Cake" version="0.37.0" />

## Linux
修改编译参数 native/linux/build.cake
```bash
skia_use_icu=true skia_use_system_icu=false skia_use_sfntly=true
```

```bash
sudo apt install libfontconfig1-dev
export CC=clang
export CXX=clang++
./bootstrapper.sh -t externals-linux
```

## MacOS
修改编译参数 native/macos/build.cake
```bash
skia_use_icu=true skia_use_system_icu=false skia_use_sfntly=true
```

```bash
./bootstrapper.sh -t externals-osx
```

## Windows
TODO

> If you are updating the source using a previous checkout, make sure to run the `clean` target before building.


# About text shaping
目前临时用TextLayout实现布局，待SKParagraph移植至SkiaSharp重新实现或参考其实现
https://github.com/mono/SkiaSharp.Extended/issues/28 Porting SkTextBox to support draw text in wrap
https://github.com/flutter/flutter/issues/39420 Text shaping backend switch: SkShaper and SkParagraph
https://github.com/ryancheung/SkiaTextRenderer/tree/master/SkiaTextRenderer