### Introduction

![001](https://s1.ax1x.com/2018/07/26/Pt7lQO.png)

#### What does it do?

* 改善某些命令行程序（图片/音视频编解码器等）在处理较多文件时的**多核利用**问题。

* 自动生成参数，省去手动输入文件名的麻烦，达到类似`XnConvert`的带GUI集成转换器的效果。

* 还有很多用处。顾名思义，这是**通用**的GUI外壳，不仅可用于下边举例的LibWebP，也可用于Lame、Guetzli、FFmpeg等。

#### How to Use?

先看源程序的帮助或者`Readme`，了解一下它的参数格式。下边是一个示例（LibWebP-cwebp）：

```
Usage: cwebp [-preset <...>] [options] in_file [-o out_file]
```

把参数分成几个部分，如下：

* `[-preset <...>] [options]`是可供调整的选项，用`{UserParameters}`标记表示。

* `in_file`是输入文件名，用`{InputFile}`标记表示。

* `-o`是输出文件名的开始标志，在`Argument templet`中直接填入。

* `out_file`是输出文件名，用`{OutputFile}`标记表示。

综上，在`Universal GUI`的`Argument templet`中填入：`{UserParameters} {InputFile} -o {OutputFile}`。请留意在标记之间按需添加空格。

接着在`Universal GUI`的`User arguments`中填入所需的参数，这些文本将替换`{UserParameters}`标记。当然，你也可以直接在`Argument templet`中填入所需参数，不使用`{UserParameters}`标记（这可以适配一些对于输入文件有一些选项，对输出文件又有一些选项，并且这些选项在参数中的位置不连续的程序，例如FFmpeg）。

不过，`这程序的初衷`难道被我忘掉了？当然不。我们可以在`Thread number`这个Combo Box中选择线程数，这将同时运行多个源程序，并进行**异步调度**（多高大上），以充分利用CPU时间。

其他的就没啥可说了。添加文件到列表中，给输出文件指定新的拓展名以及添加后缀……凡所应有，无所不有。Enjoy it!

### Contrast

我使用`LibWebP-cwebp`将36张图片转换为WebP格式，使用`-m 6`参数以取得更好的编码效果。

* `UniversalGUI`版本：`0.7.1.1`

* `LibWebP`版本：`1.0.0`

* 测试平台：`i3-2310m 2C4T`、`2G DDR3 1333 内存`。

|------------------|所用时间|平均CPU利用率|
|------------------|--------|-------------|
|未使用多线程      | 1min29s|        26.1%|
|UniversalGUI-4线程|     39s|        98.4%|

### Matters Need Attention

* 可在程序所在目录下新建一个名为`Portable`的文件（注意大小写以及无拓展名），让程序把ini配置文件放在所在目录，达到便携化效果。

* 在**所有CPU都已占满**时，`Process.Start();`需要很长时间，因此可能难以达到所指定的线程数。

### Bugs

* Combo Box只有“收起”时才能看到Pressed效果。