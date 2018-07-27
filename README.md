### Introduction

![001](https://s1.ax1x.com/2018/07/26/Pt7lQO.png)

　　假设你有一个多核心的CPU（现在谁还用单核！），同时，你有一批`图片（或者是别的什么东西）`需要`转换为xx格式`，但是这个转换器是一个命令行程序，一次只能处理一张图片，并且它**不支持多核**，怎么办？

　　也许你会说：“我多开几个，让它占满CPU时间，不就得了？”

　　但是，如果这需要转换的文件非常小，很快就能转换完成，并且这一批文件数量非常多，那么你岂不是要手忙脚乱地输一大堆命令？

　　你也许会使用`XnConvert`之类的带GUI的集成转换器，解决了手动输命令的问题。再多开几个，也似乎能解决CPU利用的问题，但是——

　　若是`XnConvert`的其中一个实例已经转换完了所有你分配给这个实例的文件，然而其他的实例还没完成任务，那么就不能最大化地利用CPU时间了！

　　再这么下去，最后也许会出现“只有一个实例在运行，其他实例都完成了分配的任务”这种蛋疼的局面，于是又回到了“一核有难x核围观”的状态。

　　所以——

　　来试试我的**Universal GUI**！

* 这个程序的初衷是为了解决多核利用的问题，不过嘛。。。我想了想，就把它做成了一个还算友好的，通用的`GUI外壳`。

* 顾名思义，这是**通用**的GUI外壳，当然不止适配下边举例的LibWebP，也可用于Lame、NeroAAC、LibPNG等命令行程序。其实，也不仅用于文件转换。

### How to Use It?

　　首先，我们需要看看源程序的帮助或者“Readme”，了解一下它的参数格式。下边是一个示例（LibWebP-cwebp）：
```
Usage: cwebp [-preset <...>] [options] in_file [-o out_file]
```

　　我们发现了什么？

* 参数大致分为三个部分：`选项（可选，不指定则使用默认选项）`、`输入文件（必填）`、`输出文件（可选，不指定则输出为同文件名）`。

　　因此，我们用三个标记来表示这三个部分。分别是：`{UserParameters}`、`{InputFile}`、`{OutputFile}`。

　　所以，我们在`Universal GUI`的`参数模板`中填入：`{UserParameters} {InputFile} -o {OutputFile}`。请留意在标记之间按需添加空格。

　　接着，我们在`Universal GUI`的`用户参数`中填入所需的参数，这些文本将会替换`{UserParameters}`标记。当然，你也可以直接在`参数模板`中直接填入所需参数，不使用`{UserParameters}`标记（这可以适配一些对于输入文件有一些选项，对输出文件又有一些选项，并且这些选项在参数中不连续的程序）。

　　不过，`这个程序的初衷`难道被我忘掉了？当然不。我们可以在`Thread number`这个Combo Box中选择线程数，这将同时运行多个源程序，并进行**异步调度**（多高大上），以弥补源程序不支持多核心带来的不便。

　　其他的就没啥可说了。添加文件到列表中，给输出文件指定新的拓展名以及添加后缀……凡所应有，无所不有。Enjoy it!

### Contrast

我使用`LibWebP-cwebp`将36张图片转换为WebP格式，使用`-m 6`以取得更好的压缩效果。

* 测试版本：`0.7.1.1`

* 测试平台：`i3-2310m 2核心 4逻辑处理器（超线程）`、`2G DDR3 1333 内存`。

|-------------|未使用多线程|UniversalGUI-4线程|
|-------------|------------|------------------|
|所用时间     |     1min29s|               39s|
|平均CPU利用率|       26.1%|             98.4%|

### Todo

* 添加语言切换功能，毕竟不是人人都能看懂英文。

* 添加暂停功能，用挂起进程实现（可能有点困难，容易出现进程死锁）（已放弃）。

### Bugs

* Combo Box只有“收起”时才能看到Pressed效果。