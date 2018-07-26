using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace UniversalGUI
{
    /// <summary>
    /// MainWindow.xaml's Interactive logic
    /// </summary>

    //Core function
    public partial class MainWindow : Window
    {
        private string DefaultTitle;

        Config config = new Config();

        public MainWindow()
        {
            InitializeComponent();
            
            DefaultTitle = Title;
            StartMonitorAsync();
        }

        public void Tetete() //Some methods for text
        {

            //MessageBox.Show(myw.ParametersTemplet.Text);

            //var jfiodsjf = Dispatcher.Invoke(() => ParametersTemplet.Text);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                //MessageBox.Show();
                StartTask(null, null);
            }));



            //var asApplicationPath = this.ApplicationPath.Text;

            //MessageBox.Show(SummarizeParameters(@"C: \Users\kkocdko\Desktop\传文件提示.txt"));
        }

        public async void StartTask()
        {
            //Change UI
            MainGrid.IsEnabled = false;
            StartTaskButton.IsEnabled = false;
            StartTaskButton.Content = "Running";
            TaskProgressBar.Visibility = Visibility.Visible;

            //Collect config on UI
            SumConfig();
            bool settingRight = CheckSetting();
            int filesNum = config.FilesList.Count;

            //Run on backgrount thread,avoid UI have been lock
            await Task.Run(() =>
            {
                if (settingRight == true)
                {
                    SetProgress(0);
                    Task[] tasks = new Task[config.ThreadNumber];
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = NewThreadAsync();
                    }
                    foreach(var task in tasks)
                    {
                        task.Wait();
                    }
                }
            });

            //Change UI
            MainGrid.IsEnabled = true;
            if (settingRight == true)
            {
                StartTaskButton.Content = "Finished";
                SetProgress(1);
            }
            else
            {
                StartTaskButton.Content = "Error";
                SetProgress(-1);
            }
            await Task.Delay(3000); //Show result to user
            StartTaskButton.IsEnabled = true;
            StartTaskButton.Content = "Start";
            TaskProgressBar.Visibility = Visibility.Hidden;
            SetProgress(); //Clear progress
        }

        private async Task NewThreadAsync()
        {
            while (config.FilesList.Count > 0)
            {
                string args = SumArgs(
                    config.ArgsTemplet,
                    config.FilesList.First.Value, //传入链表中第一个文件
                    config.UserArgs,
                    config.OutputSuffix,
                    config.OutputExtension,
                    config.OutputFloder);
                config.FilesList.RemoveFirst(); //弹出链表中第一个文件
                await Task.Run(() =>
                {
                    NewProcess(config.AppPath, args);
                    SetProgress(((double)config.FilesSum - config.FilesList.Count) / config.FilesSum);
                });
            }
        }

        private void SumConfig()
        {
            config = new Config(); //直接new一个，省的整天重置这重置那的
            Dispatcher.Invoke(() =>
            {
                foreach (var item in FilesList.Items)
                {
                    config.FilesList.AddLast(Convert.ToString(item)); //把文件压入链表底部
                }
                config.FilesSum = config.FilesList.Count;
                config.AppPath = AppPath.Text;
                config.ArgsTemplet = ArgsTemplet.Text;
                config.UserArgs = UserArgs.Text;
                config.OutputSuffix = OutputSuffix.Text;
                config.OutputExtension = OutputExtension.Text;
                config.OutputFloder = OutputFloder.Text;
                config.Priority = Convert.ToInt32(Priority.SelectedValue);
                config.ThreadNumber = Convert.ToInt32(ThreadNumber.SelectedValue);
                config.WindowStyle = Convert.ToInt32(CUIWindowStyle.SelectedValue);
            });
        }

        private string SumArgs(string argsTemplet, string inputFile, string userArgs, string outputSuffix, string outputExtension, string outputFloder)
        {
            //去前后引号
            inputFile = new Regex("[(^\")(\"$)]").Replace(inputFile, "");
            argsTemplet = new Regex("[(^\")(\"$)]").Replace(argsTemplet, "");
            userArgs = new Regex("[(^\")(\"$)]").Replace(userArgs, "");
            outputSuffix = new Regex("[(^\")(\"$)]").Replace(outputSuffix, "");
            outputExtension = new Regex("[(^\")(\"$)]").Replace(outputExtension, "");
            outputFloder = new Regex("[(^\")(\"$)]").Replace(outputFloder, "");

            string arguments = argsTemplet;

            //替换 {UserParameters}
            {
                //替换模板中的标记
                arguments = new Regex(@"\{UserParameters\}").Replace(arguments, userArgs);
            }

            //替换 {InputFile}
            {
                //加前后引号
                string inputFile2 = "\"" + inputFile + "\"";
                //替换模板中的标记
                arguments = new Regex(@"\{InputFile\}").Replace(arguments, inputFile2);
            }

            //替换 {OutputFile}
            {
                string outputFile;
                //获得主文件名
                string mainName = new Regex(@"\..[^.]+?$").Replace(inputFile, "");

                //后缀
                if (outputSuffix != "")
                {
                    mainName = mainName + outputSuffix;
                }

                //拓展名
                string extension;
                if (outputExtension != "")
                {
                    //新拓展名
                    extension = outputExtension;
                }
                else
                {
                    //原拓展名
                    var sourceExtension = new Regex(@"\..[^.]+?$").Match(inputFile);
                    extension = Convert.ToString(sourceExtension);
                }
                //去除拓展名前的点
                extension = new Regex(@"\.").Replace(extension, "");
                //组合
                outputFile = mainName + "." + extension;

                //输出文件夹
                if (outputFloder != "")
                {
                    //去路径后正反斜杠
                    outputFloder = new Regex(@"[\\/]$").Replace(outputFloder, "");
                    //加路径后反斜杠
                    outputFloder = outputFloder + "\\";
                    //替换输出路径
                    outputFile = new Regex(@"^.+\\").Replace(outputFile, outputFloder);
                }

                //加前后引号
                outputFile = "\"" + outputFile + "\"";
                //替换模板中的标记
                arguments = new Regex(@"\{OutputFile\}").Replace(arguments, outputFile);
            }

            return arguments;
        }
        
        private void NewProcess(string appPath, string args)
        {
            var process = new Process();
            process.StartInfo.FileName = appPath;
            process.StartInfo.Arguments = args;
            switch (config.WindowStyle)
            {
                case 0:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    break;
                case 1:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    break;
                case 2:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    break;
                case 3:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    break;
                default:
                    break;
            }
            process.Start();
            /*
            switch (config.Priority)
            {
                case 1:
                    process.PriorityClass = ProcessPriorityClass.Idle;
                    break;
                case 2:
                    process.PriorityClass = ProcessPriorityClass.BelowNormal;
                    break;
                case 3:
                    process.PriorityClass = ProcessPriorityClass.Normal;
                    break;
                case 4:
                    process.PriorityClass = ProcessPriorityClass.AboveNormal;
                    break;
                case 5:
                    process.PriorityClass = ProcessPriorityClass.High;
                    break;
                case 6:
                    process.PriorityClass = ProcessPriorityClass.RealTime;
                    break;
            }
            */
            process.WaitForExit();
        }
    }

    //About UI
    public partial class MainWindow : Window
    {
        private void StartTask(object sender, RoutedEventArgs e) => StartTask();

        private void SetProgress(double multiple = -2)
        {
            Dispatcher.Invoke(() =>
            {
                //重置
                if (multiple == -2)
                {
                    SetTitleSuffix();
                    TaskProgressBar.Value = 0;
                    TaskProgressBar.Foreground = new SolidColorBrush(Colors.DimGray);
                    //TaskProgressBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF06B025"));
                }

                //错误警告
                if (multiple == -1)
                {
                    SetTitleSuffix("Error");
                    TaskProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                    TaskProgressBar.Value = 100;
                }

                //修改
                if (multiple >= 0 && multiple <= 1)
                {
                    double percent = Math.Round(multiple * 100);
                    SetTitleSuffix(percent + "%");
                    TaskProgressBar.Value = percent;
                }
            });
        }

        private void SetTitleSuffix(string suffix = "")
        {
            Dispatcher.Invoke(() =>
            {
                if (suffix != "")
                {
                    Title = DefaultTitle + " [" + suffix + "]";
                }
                else
                {
                    Title = DefaultTitle;
                }
            });
        }

        private async void StartMonitorAsync()
        {
            await Task.Run(() =>
            {
                var cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuPerformanceCounter.NextValue(); //调用一下这个方法，避免输出一个“0%”
                double cpuUseRatio;
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                double usedMem;
                double memUseRatio;
                string memUnit = "KB";
                while (true)
                {
                    cpuUseRatio = Math.Round(cpuPerformanceCounter.NextValue());
                    usedMem = (double)computerInfo.TotalPhysicalMemory - computerInfo.AvailablePhysicalMemory;
                    memUseRatio = Math.Round((double)usedMem / computerInfo.TotalPhysicalMemory * 100);

                    if (usedMem > 1073634449817.6) //大于999.9GB
                    {
                        memUnit = "TB";
                        usedMem = usedMem / 1024 / 1024 / 1024 / 1024;
                        usedMem = Math.Round(usedMem, 3);
                    }
                    else if (usedMem > 107363444981.76) //大于99.99GB
                    {
                        memUnit = "GB";
                        usedMem = usedMem / 1024 / 1024 / 1024;
                        usedMem = Math.Round(usedMem, 1);
                    }
                    else if (usedMem > 10736344498.176) //大于9.999GB
                    {
                        memUnit = "GB";
                        usedMem = usedMem / 1024 / 1024 / 1024;
                        usedMem = Math.Round(usedMem, 2);
                    }
                    else if (usedMem > 1048471142.4) //大于999.9MB
                    {
                        memUnit = "GB";
                        usedMem = usedMem / 1024 / 1024 / 1024;
                        usedMem = Math.Round(usedMem, 3);
                    }
                    else if (usedMem > 104847114.24) //大于99.99MB
                    {
                        memUnit = "MB";
                        usedMem = usedMem / 1024 / 1024;
                        usedMem = Math.Round(usedMem, 1);
                    }
                    
                    Dispatcher.Invoke(() =>
                    {
                        MonitorForCPU.Text = cpuUseRatio + "%";
                        MonitorForMem.Text = usedMem + memUnit + " (" + memUseRatio + "%)";
                    });

                    Thread.Sleep(1000);
                }
            });
        }

        private bool CheckSetting()
        {
            if (FilesList.Items.Count == 0)
            {
                MessageBox.Show(@"Please add file into fileslist.", "Error");
                return false;
            }
            if (AppPath.Text == "")
            {
                MessageBox.Show(@"Please input command application's path.", "Error");
                return false;
            }
            if (ArgsTemplet.Text == "")
            {
                MessageBox.Show(@"Please input parameters' templet.", "Error");
                return false;
            }
            if (OutputFloder.Text == "" && OutputExtension.Text == "" && OutputSuffix.Text == "")
            {
                MessageBox.Show(@"If you want to output into source floder,in output extension and suffix, you need to fill in at least one.", "Error");
                return false;
            }
            return true;
        }

        private void AddFilesListItems(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                DereferenceLinks = true,
                Multiselect = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    FilesList.Items.Add(file);
                }
            }
        }

        private void RemoveFilesListItems(object sender, RoutedEventArgs e)
        {
            if (FilesList.SelectedItems.Count == FilesList.Items.Count)
            {
                FilesList.Items.Clear();
            }
            else
            {
                var selectedItems = FilesList.SelectedItems;
                var itemsCount = FilesList.SelectedItems.Count;
                for (int i = itemsCount - 1; i >= 0; i--)
                {
                    FilesList.Items.Remove(selectedItems[i]);
                }
            }
        }

        private void EmptyFilesList(object sender, RoutedEventArgs e)
        {
            FilesList.Items.Clear();
        }

        private void FilesList_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            string[] dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in dropFiles)
            {
                FilesList.Items.Add(file);
            }
        }

        private void ApplicationPath_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void ApplicationPath_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            AppPath.Text = dropFiles[0];
        }

        private void SwitchApplicationPath(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Executable program (*.exe)|*.exe|Dynamic link library (*.dll)|*.dll",
                DereferenceLinks = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                AppPath.Text = openFileDialog.FileName;
            }
        }

        private void SwitchOutputFloder(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFloder.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }

    //Save config
    public class Config
    {
        public LinkedList<string> FilesList = new LinkedList<string>();
        public int FilesSum;

        public string AppPath;
        public string ArgsTemplet;
        public string UserArgs;
        public string OutputSuffix;
        public string OutputExtension;
        public string OutputFloder;

        public int Priority;
        public int ThreadNumber;
        public int WindowStyle;
    }
}

