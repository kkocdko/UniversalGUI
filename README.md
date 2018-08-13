### Introduction

![001](https://s1.ax1x.com/2018/07/26/Pt7lQO.png)

假设你有一个多核心的CPU（现在谁还用单核！），同时，你有一批图片需要转换为WebP格式，但是WebP的编码器`cwebp`是一个命令行程序，一次只能处理一张图片，并且它**不支持多核**（虽然有`-mt`开关，但是并不怎么管用），怎么办？

如果使用`XnConvert`之类的带GUI的集成转换器，可以解决手动输命令的问题。再多开几个，也**似乎**能解决CPU利用的问题，不过——

若是`XnConvert`的其中一个实例已完成了分配的任务，而其他的实例还没完成，那么就不能最大化地利用CPU时间了！

再这么下去，最后也许会出现“仅一个实例未完成分配的任务，其他实例都完成了”这种蛋疼的局面，于是又回到了“一核有难x核围观”的状态。

所以——

来试试我的**Universal GUI**！

* 也许你已经猜出来了，我写这程序的初衷是为了解决**多核利用**问题，不过我不满足于此，就把它做成了一个还算友好的，通用的`GUI外壳`。

* 顾名思义，这是**通用**的GUI外壳，不仅可用于举例的LibWebP，也可用于Lame、NeroAAC、Guetzli等命令行程序。当然，也不仅用于文件转换。

### How to Use?

首先，我们需要看看源程序的帮助或者`Readme`，了解一下它的参数格式。下边是一个示例（LibWebP-cwebp）：

```
Usage: cwebp [-preset <...>] [options] in_file [-o out_file]
```

把参数中的各个部分抽象化，如下：

* `[-preset <...>] [options]`是可供调整的选项，用`{UserParameters}`标记表示。

* `in_file`是输入文件名，用`{InputFile}`标记表示。

* `-o`是输出文件名的开始标志，在`Argument templet`中直接填入。

* `out_file`是输出文件名，用`{OutputFile}`标记表示。

综上，我们在`Universal GUI`的`Argument templet`中填入：`{UserParameters} {InputFile} -o {OutputFile}`。请留意在标记之间按需添加空格。

接着在`Universal GUI`的`User arguments`中填入所需的参数，这些文本将会替换`{UserParameters}`标记。当然，你也可以直接在`Argument templet`中填入所需参数，不使用`{UserParameters}`标记（这可以适配一些对于输入文件有一些选项，对输出文件又有一些选项，并且这些选项在参数中的位置不连续的程序）。

不过，`这程序的初衷`难道被我忘掉了？当然不。我们可以在`Thread number`这个Combo Box中选择线程数，这将同时运行多个源程序，并进行**异步调度**（多高大上），以充分利用CPU时间。

其他的就没啥可说了。添加文件到列表中，给输出文件指定新的拓展名以及添加后缀……凡所应有，无所不有。Enjoy it!

### Contrast

我使用`LibWebP-cwebp`将36张图片转换为WebP格式，使用`-m 6`以取得更好的压缩效果。

* `UniversalGUI`版本：`0.7.1.1`

* `LibWebP`版本：`1.0.0`

* 测试平台：`i3-2310m 2核心 4逻辑处理器（超线程）`、`2G DDR3 1333 内存`。

|------------------|所用时间|平均CPU利用率|
|------------------|--------|-------------|
|未使用多线程      | 1min29s|        26.1%|
|UniversalGUI-4线程|     39s|        98.4%|

### Matters Need Attention

* 可在程序所在目录下新建一个名为`Portable`的文件（注意大小写以及无拓展名），让程序把ini配置文件放在所在目录，达到便携化效果。

* 在**所有CPU都已占满**时，`Process.Start();`需要很长时间，因此可能难以达到所指定的线程数。

### Todo

* 添加“强制停止”功能。

### Bugs

* Combo Box只有“收起”时才能看到Pressed效果。