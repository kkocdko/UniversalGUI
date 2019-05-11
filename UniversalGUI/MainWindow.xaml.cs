using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace UniversalGUI
{
    /// <summary>
    /// MainWindow.xaml's Interactive logic
    /// </summary>

    //Core
    public partial class MainWindow : Window
    {
        private Config config;

        private int[] processIds;

        public void StartTask()
        {
            Task[] tasks = new Task[config.ThreadNumber];
            processIds = new int[config.ThreadNumber];
            for (int i = 0, l = tasks.Length; i < l; i++)
            {
                tasks[i] = NewThreadAsync(i);
            }
            Task.WaitAll(tasks);
        }

        public void StopTask()
        {
            if (config == null)
            {
                return;
            }
            config.FilesList = new LinkedList<string>();
            config.FilesListEnumerator = config.FilesList.GetEnumerator();
            for (int i = 0, l = processIds.Length; i < l; i++)
            {
                try
                {
                    if (processIds[i] != 0)
                    {
                        Process.GetProcessById(processIds[i]).Kill();
                    }
                    processIds[i] = 0;
                }
                catch (System.ArgumentException e)
                {
                    Debug.WriteLine("Maybe the process [" + processIds[i] + "] isn't running. Exception message: " + e.Message);
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    MessageBox.Show("Can't kill the process [" + processIds[i] + "] . You can try again. Exception message: " + e.Message);
                }
            }
        }

        public async Task NewThreadAsync(int processIdIndex)
        {
            while (config.FilesListEnumerator.MoveNext()) // Side effect
            {
                string currentFileName = (string)config.FilesListEnumerator.Current;

                string appArgs = SumAppArgs(
                    argsTemplet: config.ArgsTemplet,
                    inputFileName: currentFileName,
                    userArgs: config.UserArgs,
                    outputSuffix: config.OutputSuffix,
                    outputExtension: config.OutputExtension,
                    outputFloder: config.OutputFloder);

                Process process = NewProcess(
                    appPath: config.AppPath,
                    appArgs: appArgs,
                    windowStyle: config.WindowStyle,
                    priority: config.Priority,
                    simulateCmd: config.SimulateCmd);

                processIds[processIdIndex] = process.Id;

                await Task.Run(() => process.WaitForExit());

                config.CompletedFileNumber++;

                Dispatcher.Invoke(() => SetProgress((double)config.CompletedFileNumber / config.FilesNumber));
            }
        }

        private void RemoveQuotationMasks(ref string sourceString)
        {
            sourceString = new Regex("(^\")|(\"$)").Replace(sourceString, "");
        }

        private void AddQuotationMasks(ref string sourceString)
        {
            sourceString = "\"" + sourceString + "\"";
        }

        private string SumAppArgs(string argsTemplet, string inputFileName, string userArgs, string outputSuffix, string outputExtension, string outputFloder)
        {
            // Remove quotation mask
            RemoveQuotationMasks(ref inputFileName);
            RemoveQuotationMasks(ref argsTemplet);
            RemoveQuotationMasks(ref outputSuffix);
            RemoveQuotationMasks(ref outputExtension);
            RemoveQuotationMasks(ref outputFloder);

            string args = argsTemplet;

            //替换 {UserParameters}
            {
                //替换模板中的标记
                args = new Regex(@"\{UserParameters\}").Replace(args, userArgs);
            }

            //替换 {InputFile}
            {
                //加前后引号
                string inputFile2 = "\"" + inputFileName + "\"";
                //替换模板中的标记
                args = new Regex(@"\{InputFile\}").Replace(args, inputFile2);
            }

            //替换 {OutputFile}
            {
                string outputFile;
                //获得主文件名
                string mainName = new Regex(@"\..[^.]+?$").Replace(inputFileName, "");

                //后缀
                if (outputSuffix != "")
                {
                    mainName += outputSuffix;
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
                    var sourceExtension = new Regex(@"\..[^.]+?$").Match(inputFileName);
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
                    outputFloder += "\\";
                    //替换输出路径
                    outputFile = new Regex(@"^.+\\").Replace(outputFile, outputFloder);
                }

                //加前后引号
                AddQuotationMasks(ref outputFile);
                //替换模板中的标记
                args = new Regex(@"\{OutputFile\}").Replace(args, outputFile);
            }

            return args;
        }

        private Process NewProcess(string appPath, string appArgs, uint windowStyle, uint priority, bool simulateCmd)
        {
            var process = new Process();
            if (simulateCmd == false)
            {
                process.StartInfo.FileName = appPath;
                process.StartInfo.Arguments = appArgs;
            }
            else
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + appPath + " " + appArgs; // 这边不能给appPath加引号
            }

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
            }

            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Debug.WriteLine("The process can not be started. Exception message: {0}", e.Message);
                return null;
            }

            process.PriorityBoostEnabled = false;
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

            return process;
        }

        private void SumConfig()
        {
            config = new Config();
            foreach (var item in FilesList.Items)
            {
                config.FilesList.AddLast((string)item);
            }
            config.FilesListEnumerator = config.FilesList.GetEnumerator();
            config.FilesNumber = config.FilesList.Count;
            config.AppPath = AppPath.Text;
            config.ArgsTemplet = ArgsTemplet.Text;
            config.UserArgs = UserArgs.Text;
            config.OutputSuffix = OutputSuffix.Text;
            config.OutputExtension = OutputExtension.Text;
            config.OutputFloder = OutputFloder.Text;
            config.Priority = Convert.ToUInt32(Priority.SelectedValue);
            config.ThreadNumber = Convert.ToUInt32(ThreadNumber.SelectedValue);
            config.WindowStyle = Convert.ToUInt32(CUIWindowStyle.SelectedValue);
            config.SimulateCmd = SimulateCmd.SelectedValue.ToString() == "2";
        }
    }

    // On start or exit
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DefaultTitle = this.Title;
            IniConfigManager = new IniManager(GetIniConfigFile());
            ImputIniConfig(IniConfigManager);
            SetLanguage();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartMonitorAsync();
        }

        private void MainWindow_WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveIniConfig(IniConfigManager);
        }

        private void MainWindow_WindowClosed(object sender, EventArgs e)
        {
            StopTask();
        }
    }

    // About UI
    public partial class MainWindow : Window
    {
        private readonly string DefaultTitle;

        private string QueryLangDict(string key)
        {
            return Application.Current.FindResource(key).ToString();
        }

        private void SetLanguage()
        {
            var dictionaryList = new List<ResourceDictionary>();
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            string culture = Thread.CurrentThread.CurrentCulture.ToString();
            string requestedCulture = string.Format(@"Resources\Language\{0}.xaml", culture);
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            if (resourceDictionary == null)
            {
                requestedCulture = @"Resources\Language\en-US.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            }
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private async void StartTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskProgressBar.Visibility == Visibility.Visible)
            {
                StopTask();
                return;
            }
            else
            {
                // Change UI
                //TaskSettings.IsEnabled = false;
                StartTaskButton.Content = QueryLangDict("Button_StartTask_Stop");
                SetProgress(0);

                // Collect config
                SumConfig();
                bool settingLegal = CheckConfig();

                // Start task
                await Task.Run(() => StartTask());

                // Change UI
                //TaskSettings.IsEnabled = true;
                if (settingLegal == true)
                {
                    StartTaskButton.Content = QueryLangDict("Button_StartTask_Finished");
                    SetProgress(1);
                }
                else
                {
                    StartTaskButton.Content = QueryLangDict("Button_StartTask_Error");
                    SetProgress(-1);
                }
                await Task.Delay(3000); //Show result to user
                StartTaskButton.Content = QueryLangDict("Button_StartTask_Start");
                TaskProgressBar.Visibility = Visibility.Hidden;
                SetProgress(); // Reset progress
            }
        }

        private void SetProgress(double multiple = -2)
        {
            if (multiple >= 0 && multiple <= 1) // Change
            {
                int percent = Convert.ToInt32(Math.Round(multiple * 100));
                SetTitleSuffix(percent + "%");
                TaskProgressBar.Value = percent;
                TaskProgressBar.Visibility = Visibility.Visible;
                TaskbarManager.SetProgressValue(percent, 100);
                TaskbarManager.SetProgressState(TaskbarProgressBarState.Normal);
            }
            else if (multiple == -2) // Reset
            {
                SetTitleSuffix();
                TaskProgressBar.Value = 0;
                TaskProgressBar.Foreground = new SolidColorBrush(Colors.DimGray);
                TaskProgressBar.Visibility = Visibility.Hidden;
                TaskbarManager.SetProgressValue(0, 100);
                TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);
            }
            else if (multiple == -1) // Error warning
            {
                string suffix = QueryLangDict("Window_MainWindow_Title_Suffix_Error");
                SetTitleSuffix(suffix);
                TaskProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                TaskProgressBar.Value = 100;
                TaskProgressBar.Visibility = Visibility.Visible;
                TaskbarManager.SetProgressValue(100, 100);
                TaskbarManager.SetProgressState(TaskbarProgressBarState.Error);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void SetTitleSuffix(string suffix = "")
        {
            Title = suffix == ""
                ? DefaultTitle
                : DefaultTitle + " - " + suffix;
        }

        private async void StartMonitorAsync()
        {
            await Task.Run(() =>
            {
                var cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuPerformanceCounter.NextValue();
                double cpuUseRatio;
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                double usedMem;
                string memUnit = "KB";
                double memUseRatio;
                while (true)
                {
                    cpuUseRatio = Math.Round(cpuPerformanceCounter.NextValue());
                    usedMem = (double)computerInfo.TotalPhysicalMemory - computerInfo.AvailablePhysicalMemory;
                    memUseRatio = Math.Round(usedMem / computerInfo.TotalPhysicalMemory * 100);
                    if (usedMem >= 1099511627776) //1TB
                    {
                        memUnit = "TB";
                        usedMem /= 1099511627776; //1024^4
                        usedMem = Math.Round(usedMem, 3);
                    }
                    else if (usedMem >= 107374182400) //100GB
                    {
                        memUnit = "GB";
                        usedMem /= 1073741824; //^3
                        usedMem = Math.Round(usedMem, 1);
                    }
                    else if (usedMem >= 10737418240) //10GB
                    {
                        memUnit = "GB";
                        usedMem /= 1073741824; //^3
                        usedMem = Math.Round(usedMem, 2);
                    }
                    else if (usedMem >= 1073741824) //1GB
                    {
                        memUnit = "GB";
                        usedMem /= 1073741824; //^3
                        usedMem = Math.Round(usedMem, 3);
                    }
                    else if (usedMem >= 104857600) //100MB
                    {
                        memUnit = "MB";
                        usedMem /= 1048576;//^2
                        usedMem = Math.Round(usedMem, 1);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        MonitorForCPU.Content = cpuUseRatio + "%";
                        MonitorForRAM.Content = usedMem + memUnit + " (" + memUseRatio + "%)";
                    });

                    Thread.Sleep(1000);
                }
            });
        }

        private bool CheckConfig()
        {
            string title = QueryLangDict("Message_Title_Error");
            if (FilesList.Items.Count == 0)
            {
                string content = QueryLangDict("Message_FileslistIsEmpty");
                MessageBox.Show(content, title);
                return false;
            }
            else if (AppPath.Text == "")
            {
                string content = QueryLangDict("Message_CommandAppUnspecified");
                MessageBox.Show(content, title);
                return false;
            }
            else if (OutputFloder.Text == "" && OutputExtension.Text == "" && OutputSuffix.Text == "")
            {
                string content = QueryLangDict("Message_OutputSettingsDangerous");
                var result = MessageBox.Show(content, title, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }
            else if (CustomThreadNumberItem.IsSelected == true)
            {
                if (Convert.ToInt32(CustomThreadNumberItem.Tag) == 0 || CustomThreadNumberTextBox.Text == "")
                {
                    string content = QueryLangDict("Message_ThreadNumberIsIllegal");
                    MessageBox.Show(content, title);
                    return false;
                }
            }
            else if (AppPath.Text.IndexOf(' ') != -1 && Convert.ToInt32(SimulateCmd.SelectedValue) == 2)
            {
                string content = QueryLangDict("Message_SimulateCmdIsIllegal");
                MessageBox.Show(content, title);
                return false;
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
                for (int i = selectedItems.Count - 1; i > -1; i--)
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

        private void SwitchAppPath(object sender, RoutedEventArgs e)
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

        private void DropFileTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void DropFileTextBox_PreviewDrop(object senderObj, DragEventArgs e)
        {
            var sender = (TextBox)senderObj;
            string[] dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            sender.Text = dropFiles[0];
        }

        private void InsertArgsTempletTag(string tagContent)
        {
            string text = ArgsTemplet.Text;
            string insertContent = "{" + tagContent + "}";
            int selectionStart = ArgsTemplet.SelectionStart;
            text = text.Insert(selectionStart, insertContent);
            ArgsTemplet.Text = text;
            ArgsTemplet.SelectionStart = selectionStart + insertContent.Length;
            ArgsTemplet.Focus();
        }

        private void InsertArgsTempletMark_UserParameters(object sender, RoutedEventArgs e)
        {
            InsertArgsTempletTag("UserParameters");
        }

        private void InsertArgsTempletMark_InputFile(object sender, RoutedEventArgs e)
        {
            InsertArgsTempletTag("InputFile");
        }

        private void InsertArgsTempletMark_OutputFile(object sender, RoutedEventArgs e)
        {
            InsertArgsTempletTag("OutputFile");
        }

        private void ShowArgsTempletHelp(object sender, RoutedEventArgs e)
        {
            string title = QueryLangDict("Message_Title_Hint");
            string content = QueryLangDict("Message_ArgsTempletHelp");
            MessageBox.Show(content, title);
        }

        private void SwitchOutputFloder(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFloder.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void AutoSelectTextBox_PreviewMouseDown(object senderObj, MouseButtonEventArgs e)
        {
            var sender = (TextBox)senderObj;
            if (!sender.IsFocused)
            {
                sender.Focus();
                e.Handled = true;
            }
        }

        private void AutoSelectTextBox_GotFocus(object senderObj, RoutedEventArgs e)
        {
            var sender = (TextBox)senderObj;
            sender.SelectAll();
        }

        private void CustomThreadNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CustomThreadNumberItem.IsSelected = true;
        }

        private void CustomThreadNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _ = Convert.ToUInt32(CustomThreadNumberTextBox.Text);
            }
            catch
            {
                CustomThreadNumberTextBox.Text = "";
            }
        }
    }

    // About Ini Config
    public partial class MainWindow : Window
    {
        private const string IniConfigFileName = "Config.ini";

        /// <summary>
        /// This is the config file's version, not app version.
        /// </summary>
        private const string IniConfigFileVersion = "0.7.7.2";

        private readonly IniManager IniConfigManager;

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
                        ThreadNumber.SelectedValue = threadNumber;
                        if (threadNumber > 8)
                        {
                            CustomThreadNumberTextBox.Text = threadNumber.ToString();
                            CustomThreadNumberItem.Tag = threadNumber;
                            CustomThreadNumberItem.IsSelected = true;
                        }

                        CUIWindowStyle.SelectedValue = ini.Read("Process", "WindowStyle");
                        SimulateCmd.SelectedValue = ini.Read("Process", "SimulateCmd");

                        string culture = ini.Read("Language", "Culture");
                        if (culture != "")
                        {
                            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                        }
                    }
                    catch (Exception e)
                    {
                        string title = QueryLangDict("Message_Title_Error");
                        string content = QueryLangDict("Message_ConfigfileFormatMistake");
                        MessageBox.Show(content + "\n\n" + e.TargetSite + "\n\n" + e.Message, title);
                    }
                }
                else
                {
                    string title = QueryLangDict("Message_Title_Hint");
                    string content = QueryLangDict("Message_UseBuildInConfigfile");
                    MessageBox.Show(content, title);
                }
            }
        }

        private void SaveIniConfig(IniManager ini)
        {
            if (!File.Exists(ini.IniFile))
            {
                try
                {
                    ini.CreatFile();
                }
                catch
                {
                    string title = QueryLangDict("Message_Title_Error");
                    string content = QueryLangDict("Message_CanNotWriteConfigfile");
                    MessageBox.Show(content, title);
                    return;
                }
            }

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
                ini.Write("Process", "SimulateCmd", SimulateCmd.SelectedValue);
            }
            else
            {
                string title = QueryLangDict("Message_Title_Hint");
                string content = QueryLangDict("Message_CreatNewConfigfile");
                var result = MessageBox.Show(content, title, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    ini.CreatFile();
                    SaveIniConfig(ini);
                }
            }

        }
    }

    public class Config
    {
        public LinkedList<string> FilesList = new LinkedList<string>();
        public IEnumerator FilesListEnumerator;
        public int FilesNumber;
        public int CompletedFileNumber = 0;

        public string AppPath;
        public string ArgsTemplet;
        public string UserArgs;
        public string OutputSuffix;
        public string OutputExtension;
        public string OutputFloder;

        public uint Priority;
        public uint ThreadNumber;
        public uint WindowStyle;
        public bool SimulateCmd;
    }
}
