### Introduction

![001](https://kkocdko.github.io/src/img/20180726-010059-001.webp)

* 此为`1.0.1.1`版本的截图。

#### What does it do?

* 自动生成参数，省去手动输入文件名的麻烦，达到类似`XnConvert`的带GUI集成转换器的效果。

* 改善某些命令行程序（图片/音视频编解码器等）在处理较多文件时的**多核利用**问题。

#### How to Use?

阅读源程序的`README`，了解参数格式。下面是一个示例（LibWebP-cwebp）：

```
Usage: cwebp [-preset <...>] [options] in_file [-o out_file]
```

把参数分成几个部分：

* `[-preset <...>] [options]`是可供调整的选项，用`{UserArg}`标记表示。

* `in_file`是输入文件名，用`{InputFile}`标记表示。

* `-o`是输出文件名的开始标志，在`Argument templet`中直接填入。

* `out_file`是输出文件名，用`{OutputFile}`标记表示。

综上，在`Argument templet`中填入：`{UserArg} {InputFile} -o {OutputFile}`。请留意在标记之间按需添加空格。

接着在`User arguments`中填入所需参数，这将替换`{UserArg}`标记。当然，你也可以直接在`Argument templet`中填入参数，不使用`{UserArg}`标记。

可以在`Thread count`这个Combo Box中选择线程数。这将同时运行多个源程序，充分利用CPU算力。

其他功能按需食用。

### Contrast

我使用`LibWebP-cwebp`将36张PNG图片转换为WebP格式，参数`-m 6`。

* `UniversalGUI`：`0.7.1.1`

* `LibWebP`：`1.0.0`

* `i3-2310m 2C4T`、`2G DDR3 1333`。

|------------------|所用时间|平均CPU利用率|
|------------------|--------|-------------|
|未使用多线程      | 1min29s|        26.1%|
|UniversalGUI-4线程|     39s|        98.4%|

### Matters Need Attention

* 可在程序所在目录下新建名为`Portable`的文件（无拓展名），让程序把配置文件放在所在目录，达到便携化效果。

* 在**所有CPU都已占满**时，`Process.Start();`需要很长时间，因此可能难以达到所指定的线程数。

* 某些命令行操作不能通过给程序加参实现（例如用ffmpeg搭桥到qaac进行音频压制），这时请开启`Simulate cmd`选项，以模拟在cmd中输入命令。
