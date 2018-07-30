using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    //Core
    public partial class MainWindow : Window
    {
        private Config config = new Config();

        public async void StartTaskAsync()
        {
            //Change UI
            this.IsEnabled = false;
            StartTaskButton.IsEnabled = false;
            StartTaskButton.Content = "Running";
            SetProgress(0);

            //Collect config on UI
            SumConfig();
            bool settingLegal = CheckConfig();

            //Run on background thread
            await Task.Run(() =>
            {
                if (settingLegal == true)
                {
                    Task[] tasks = new Task[config.ThreadNumber];
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = NewThreadAsync();
                    }
                    foreach (var task in tasks)
                    {
                        task.Wait();
                    }
                }
            });

            //Change UI
            this.IsEnabled = true;
            if (settingLegal == true)
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
            SetProgress(); //擦屁股
        }

        public async Task NewThreadAsync()
        {
            while (config.FilesList.Count > 0)
            {
                string sumAppArgs = SumAppArgs(
                    argsTemplet: config.ArgsTemplet,
                    inputFile: config.FilesList.First.Value, //链表中第一个文件
                    userArgs: config.UserArgs,
                    outputSuffix: config.OutputSuffix,
                    outputExtension: config.OutputExtension,
                    outputFloder: config.OutputFloder);
                config.FilesList.RemoveFirst(); //弹出链表中第一个文件
                await Task.Run(() =>
                {
                    NewProcess(
                        appPath: config.AppPath,
                        appArgs: sumAppArgs,
                        windowStyle: config.WindowStyle,
                        priority: config.Priority);
                });
                config.CompletedFileNum++;
                Dispatcher.Invoke(() =>
                {
                    SetProgress((double)config.CompletedFileNum / config.FilesSum);
                });
            }
        }

        private string SumAppArgs(string argsTemplet, string inputFile, string userArgs, string outputSuffix, string outputExtension, string outputFloder)
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

        private void NewProcess(string appPath, string appArgs, uint windowStyle, uint priority)
        {
            var process = new Process();
            process.StartInfo.FileName = appPath;
            process.StartInfo.Arguments = appArgs;
            switch (windowStyle)
            {
                case 1:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    break;
                case 2:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    break;
                case 3:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    break;
                case 4:
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    break;
                default:
                    break;
            }
            process.Start();
            switch (priority)
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
            process.WaitForExit();
        }

        private void SumConfig()
        {
            config = new Config(); //直接new一个，省的整天重置这重置那的
            Dispatcher.Invoke(() =>
            {
                foreach (var item in FilesList.Items)
                {
                    config.FilesList.AddLast(Convert.ToString(item)); //把文件添加到链表底部
                }
                config.FilesSum = FilesList.Items.Count;
                config.AppPath = AppPath.Text;
                config.ArgsTemplet = ArgsTemplet.Text;
                config.UserArgs = UserArgs.Text;
                config.OutputSuffix = OutputSuffix.Text;
                config.OutputExtension = OutputExtension.Text;
                config.OutputFloder = OutputFloder.Text;
                config.Priority = Convert.ToUInt32(Priority.SelectedValue);
                config.ThreadNumber = Convert.ToUInt32(ThreadNumber.SelectedValue);
                config.WindowStyle = Convert.ToUInt32(CUIWindowStyle.SelectedValue);
            });
        }
    }

    //Start and Exit
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DefaultTitle = this.Title;

            IniConfigManager = new IniManager(GetIniConfigFile());
            ImputIniConfig(IniConfigManager);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartMonitorAsync();
        }

        private void MainWindow_WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveIniConfig(IniConfigManager);
        }
    }

    //About UI
    public partial class MainWindow : Window
    {
        private string DefaultTitle;

        private void StartTaskAsync(object sender, RoutedEventArgs e) => StartTaskAsync();

        private void SetProgress(double multiple = -2)
        {
            if (multiple == -2) //重置
            {
                SetTitleSuffix();
                TaskProgressBar.Value = 0;
                TaskProgressBar.Foreground = new SolidColorBrush(Colors.DimGray);
                TaskProgressBar.Visibility = Visibility.Hidden;
            }
            else if (multiple == -1) //错误警告
            {
                SetTitleSuffix("Error");
                TaskProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                TaskProgressBar.Value = 100;
                TaskProgressBar.Visibility = Visibility.Visible;
            }
            else if (multiple >= 0 && multiple <= 1) //修改
            {
                double percent = Math.Round(multiple * 100);
                SetTitleSuffix(percent + "%");
                TaskProgressBar.Value = percent;
                TaskProgressBar.Visibility = Visibility.Visible;
            }
        }

        private void SetTitleSuffix(string suffix = "")
        {
            if (suffix == "")
            {
                Title = DefaultTitle;
            }
            else
            {
                Title = DefaultTitle + " [" + suffix + "]";
            }
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

        private bool CheckConfig()
        {
            if (FilesList.Items.Count == 0)
            {
                MessageBox.Show("Please add file into fileslist.", "Error");
                return false;
            }
            else if (AppPath.Text == "")
            {
                MessageBox.Show("Please input command application's path.", "Error");
                return false;
            }
            else if (ArgsTemplet.Text == "")
            {
                MessageBox.Show("Please input arguments' templet.", "Error");
                return false;
            }
            else if (OutputFloder.Text == "" && OutputExtension.Text == "" && OutputSuffix.Text == "")
            {
                MessageBox.Show("If you want to output into source floder,in output extension and suffix, you need to fill in at least one.", "Error");
                return false;
            }
            else if (CustomThreadNumberItem.IsSelected == true)
            {
                if (Convert.ToInt32(CustomThreadNumberItem.Tag) == 0 || CustomThreadNumberTextBox.Text == "")
                {
                    MessageBox.Show("Thread number is illegal.", "Error");
                    return false;
                }
            }
            return true;
        }

        private void AddFilesListItems(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
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

        private void SwitchApplicationPath(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Executable program (*.exe)|*.exe|Dynamic link library (*.dll)|*.dll",
                DereferenceLinks = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                AppPath.Text = openFileDialog.FileName;
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

        private void SwitchOutputFloder(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFloder.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void OutputFloder_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void OutputFloder_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] dropFloders = (string[])e.Data.GetData(DataFormats.FileDrop);
            OutputFloder.Text = dropFloders[0];
        }

        private void OutputExtension_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OutputExtension.Focus();
            e.Handled = true;
        }

        private void OutputExtension_GotFocus(object sender, RoutedEventArgs e)
        {
            OutputExtension.SelectAll();
        }

        private void OutputSuffix_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OutputSuffix.Focus();
            e.Handled = true;
        }

        private void OutputSuffix_GotFocus(object sender, RoutedEventArgs e)
        {
            OutputSuffix.SelectAll();
        }

        private void CustomThreadNumberTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            uint threadNumber = 0;
            try
            {
                threadNumber = Convert.ToUInt32(CustomThreadNumberTextBox.Text);
            }
            catch (FormatException)
            {
                CustomThreadNumberTextBox.Clear();
            }
            catch (OverflowException)
            {
                CustomThreadNumberTextBox.Clear();
            }
            finally
            {
                CustomThreadNumber(threadNumber);
                CustomThreadNumberTextBox.Focus();
            }
        }

        private void CustomThreadNumber(uint threadNumber, bool updateTextBox = false)
        {
            CustomThreadNumberItem.Tag = threadNumber;
            if (threadNumber > 8)
            {
                ThreadNumber.SelectedValue = threadNumber;
            }
            if (updateTextBox == true)
            {
                CustomThreadNumberTextBox.Text = threadNumber.ToString();
            }
        }
    }

    //About Ini Config
    public partial class MainWindow : Window
    {
        private string IniConfigFileName = "Config.ini";

        private string IniConfigFileVersion = "0.7.7.2";

        private IniManager IniConfigManager;

        private string GetIniConfigFile()
        {
            string iniConfigFilePath;
            if (File.Exists(Environment.CurrentDirectory + "\\Portable") == true)
            {
                iniConfigFilePath = Environment.CurrentDirectory;
            }
            else
            {
                iniConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\UniversalGUI";
            }
            string IniConfigFile = Path.Combine(iniConfigFilePath, IniConfigFileName);
            return IniConfigFile;
        }

        private void ImputIniConfig(IniManager ini)
        {
            if (File.Exists(ini.IniFile) && File.ReadAllBytes(ini.IniFile).Length != 0)
            {
                if (ini.Read("Versions", "ConfigFile") == IniConfigFileVersion)
                {
                    try
                    {
                        string windowWidth = ini.Read("Window", "Width");
                        this.Width = Convert.ToDouble(windowWidth);
                        string windowHeight = ini.Read("Window", "Height");
                        this.Height = Convert.ToDouble(windowHeight);
                        AppPath.Text = ini.Read("Command", "AppPath");
                        ArgsTemplet.Text = ini.Read("Command", "ArgsTemplet");
                        UserArgs.Text = ini.Read("Command", "UserArgs");
                        OutputExtension.Text = ini.Read("Output", "Extension");
                        OutputSuffix.Text = ini.Read("Output", "Suffix");
                        OutputFloder.Text = ini.Read("Output", "Floder");
                        Priority.SelectedValue = ini.Read("Process", "Priority");
                        uint threadNumber = Convert.ToUInt32(ini.Read("Process", "ThreadNumber"));
                        if (threadNumber > 8)
                        {
                            CustomThreadNumber(threadNumber, updateTextBox: true);
                        }
                        ThreadNumber.SelectedValue = threadNumber;
                        CUIWindowStyle.SelectedValue = ini.Read("Process", "WindowStyle");
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("There is a mistake in the configfile's format:" + "\n\n" + e.TargetSite + "\n\n" + e.Message, "Error");
                    }
                }
                else
                {
                    MessageBox.Show("Existing configFile's version is not supported.So you will use built-in config", "Hint");
                }
            }
        }

        private void SaveIniConfig(IniManager ini)
        {
            if (File.Exists(ini.IniFile))
            {
                if (ini.Read("Versions", "ConfigFile") == IniConfigFileVersion || File.ReadAllBytes(ini.IniFile).Length == 0)
                {
                    ini.Write("Versions", "ConfigFile", IniConfigFileVersion);
                    ini.Write("Window", "Width", this.Width);
                    ini.Write("Window", "Height", this.Height);
                    ini.Write("Command", "AppPath", AppPath.Text);
                    ini.Write("Command", "ArgsTemplet", ArgsTemplet.Text);
                    ini.Write("Command", "UserArgs", UserArgs.Text);
                    ini.Write("Output", "Extension", OutputExtension.Text);
                    ini.Write("Output", "Suffix", OutputSuffix.Text);
                    ini.Write("Output", "Floder", OutputFloder.Text);
                    ini.Write("Process", "Priority", Priority.SelectedValue);
                    ini.Write("Process", "ThreadNumber", ThreadNumber.SelectedValue);
                    ini.Write("Process", "WindowStyle", CUIWindowStyle.SelectedValue);
                }
                else
                {
                    var result = MessageBox.Show("Existing configFile's version is not supported.Want you delete it and creat a new one?", "Hint", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        ini.CreatFile();
                        SaveIniConfig(ini);
                    }
                }
            }
            else
            {
                ini.CreatFile();
                SaveIniConfig(ini);
            }
        }
    }

    public class Config
    {
        public LinkedList<string> FilesList = new LinkedList<string>();
        public int FilesSum = 0;
        public int CompletedFileNum = 0;

        public string AppPath;
        public string ArgsTemplet;
        public string UserArgs;
        public string OutputSuffix;
        public string OutputExtension;
        public string OutputFloder;

        public uint Priority;
        public uint ThreadNumber;
        public uint WindowStyle;
    }
}
