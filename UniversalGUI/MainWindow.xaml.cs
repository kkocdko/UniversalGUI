using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UniversalGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = UiData = new MainWindowData();
            UiData.DefaultTitle = this.Title;
            IniConfigManager = new IniManager(GetIniConfigFile());
            ImportIniConfig(IniConfigManager);
            SetLanguage();
        }

        private MainWindowData UiData;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(StartMonitorAsync);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            StopTask();
            StartTaskButton.Focus(); //Ensure that the binding data are saved
            SaveIniConfig(IniConfigManager);
        }

        private string QueryLangDict(string key)
        {
            return Application.Current.FindResource(key).ToString();
        }

        private void SetLanguage()
        {
            var dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
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
            if (UiData.TaskRunning)
            {
                StopTask();
            }
            else if (CheckConfig())
            {
                UiData.TaskRunning = true;
                SetProgress(0);
                StartTaskButton.Content = QueryLangDict("Button_StartTask_Stop");
                taskFiles = new TaskFiles(FilesList.Items);
                await Task.Run(StartTask);
                StartTaskButton.Content = QueryLangDict("Button_StartTask_Finished");
                SetProgress(1);
                await Task.Delay(3000);
                UiData.TaskRunning = false;
                SetProgress();
                StartTaskButton.Content = QueryLangDict("Button_StartTask_Start");
            }
        }

        private void SetProgress(double multiple = -1)
        {
            Dispatcher.Invoke(() =>
            {
                if (multiple >= 0 && multiple <= 1) // Change
                {
                    int percent = Convert.ToInt32(Math.Round(multiple * 100));
                    SetTitleSuffix(percent + "%");
                    TaskProgressBar.Value = percent;
                    TaskbarManager.SetProgressValue(percent, 100);
                    TaskbarManager.SetProgressState(TaskbarProgressBarState.Normal);
                }
                else if (multiple == -1) // Reset
                {
                    SetTitleSuffix();
                    TaskProgressBar.Value = 0;
                    TaskbarManager.SetProgressValue(0, 100);
                    TaskbarManager.SetProgressState(TaskbarProgressBarState.NoProgress);
                }
                else
                {
                    throw new ArgumentException();
                }
            });
        }

        private void SetTitleSuffix(string suffix = "")
        {
            Title = suffix == ""
                ? UiData.DefaultTitle
                : UiData.DefaultTitle + " - " + suffix;
        }

        private async void StartMonitorAsync()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramInUseCounter = new PerformanceCounter("Process", "Working Set", "_Total");
            var ramAvailableCounter = new PerformanceCounter("Memory", "Available Bytes");
            float ramTotal;
            float ramInUse;
            while (true)
            {
                ramInUse = ramInUseCounter.NextValue();
                ramTotal = ramInUse + ramAvailableCounter.NextValue();
                UiData.CpuUsage = Math.Round(cpuCounter.NextValue()) + "%";
                UiData.RamUsage = Math.Round(ramInUse / ramTotal * 100) + "%";
                await Task.Delay(1000);
            }
        }

        private bool CheckConfig()
        {
            string errorTitle = QueryLangDict("Message_Title_Error");
            if (FilesList.Items.Count == 0)
            {
                MessageBox.Show(
                    QueryLangDict("Message_FileslistIsEmpty"),
                    errorTitle
                );
                return false;
            }
            else if (UiData.AppPath == "")
            {
                MessageBox.Show(
                    QueryLangDict("Message_CommandAppUnspecified"),
                    errorTitle
                );
                return false;
            }
            else if (UiData.OutputFloder == "" && UiData.OutputExtension == "" && UiData.OutputSuffix == "")
            {
                var result = MessageBox.Show(
                    QueryLangDict("Message_OutputSettingsDangerous"),
                    errorTitle,
                    MessageBoxButton.YesNo
                );
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }
            else if (UiData.ThreadCount == 0 || (CustomThreadCountItem.IsSelected && CustomThreadCountTextBox.Text == ""))
            {
                MessageBox.Show(
                    QueryLangDict("Message_ThreadNumberIsIllegal"),
                    errorTitle
                );
                return false;
            }
            else if (UiData.SimulateCmd == 2 && UiData.AppPath.IndexOf(' ') != -1)
            {
                MessageBox.Show(
                    QueryLangDict("Message_SimulateCmdIsIllegal"),
                    errorTitle
                );
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
                foreach (string file in openFileDialog.FileNames)
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
            foreach (string file in dropFiles)
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
                UiData.AppPath = openFileDialog.FileName;
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
            sender.Focus();
        }

        private void InsertArgsTempletMark(object senderObj, RoutedEventArgs e)
        {
            var sender = (MenuItem)senderObj;
            string insertContent = sender.Header.ToString();
            int originSelectionStart = ArgsTemplet.SelectionStart;
            ArgsTemplet.Text = ArgsTemplet.Text.Insert(ArgsTemplet.SelectionStart, insertContent);
            ArgsTemplet.SelectionStart = originSelectionStart + insertContent.Length;
            ArgsTemplet.Focus();
        }

        private void ShowArgsTempletHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                QueryLangDict("Message_ArgsTempletHelp"),
                QueryLangDict("Message_Title_Hint")
            );
        }

        private void SwitchOutputFloder(object sender, RoutedEventArgs e)
        {
            var folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UiData.OutputFloder = folderBrowser.SelectedPath;
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

        private void CustomThreadCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CustomThreadCountItem.IsSelected = true;
        }

        private void CustomThreadCountTextBox_TextChanged(object senderObj, TextChangedEventArgs e)
        {
            var sender = (TextBox)senderObj;
            CustomThreadCountItem.Tag = sender.Text;
            try
            {
                Convert.ToUInt16(sender.Text);
            }
            catch
            {
                sender.Text = "";
            }
        }
    }

    public partial class MainWindow : Window
    {
        private const string IniConfigFileName = "Config.ini";

        private const string IniConfigFileVersion = "1.0.1.1"; // Configfile's version, not app version

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
                iniConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\UniversalGUI";
            }
            string IniConfigFile = Path.Combine(iniConfigFilePath, IniConfigFileName);
            return IniConfigFile;
        }

        private void ImportIniConfig(IniManager ini)
        {
            if (!File.Exists(ini.IniFilePath) || File.ReadAllBytes(ini.IniFilePath).Length == 0)
            {
                return;
            }
            else if (ini.Read("Versions", "ConfigFile") != IniConfigFileVersion)
            {
                MessageBox.Show(
                    QueryLangDict("Message_UseBuildInConfigfile"),
                    QueryLangDict("Message_Title_Hint")
                );
                return;
            }
            else
            {
                try
                {
                    this.Width = Convert.ToDouble(ini.Read("Window", "Width"));
                    this.Height = Convert.ToDouble(ini.Read("Window", "Height"));
                    UiData.AppPath = ini.Read("Command", "AppPath");
                    UiData.ArgsTemplet = ini.Read("Command", "ArgsTemplet");
                    UiData.UserArgs = ini.Read("Command", "UserArgs");
                    UiData.OutputExtension = ini.Read("Output", "Extension");
                    UiData.OutputSuffix = ini.Read("Output", "Suffix");
                    UiData.OutputFloder = ini.Read("Output", "Floder");
                    UiData.Priority = Convert.ToInt32(ini.Read("Process", "Priority"));
                    int threadCount = Convert.ToInt32(ini.Read("Process", "ThreadCount"));
                    if (threadCount > 8)
                    {
                        CustomThreadCountTextBox.Text = threadCount.ToString();
                    }
                    UiData.ThreadCount = threadCount;
                    UiData.WindowStyle = Convert.ToInt32(ini.Read("Process", "WindowStyle"));
                    UiData.SimulateCmd = Convert.ToInt32(ini.Read("Process", "SimulateCmd"));

                    string culture = ini.Read("Language", "Culture");
                    if (culture != "")
                    {
                        Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        QueryLangDict("Message_ConfigfileFormatMistake") + "\n\n" + e.TargetSite + "\n\n" + e.Message,
                        QueryLangDict("Message_Title_Error")
                    );
                }
            }
        }

        private void SaveIniConfig(IniManager ini)
        {
            if (!File.Exists(ini.IniFilePath))
            {
                try
                {
                    ini.CreatFile();
                }
                catch
                {
                    MessageBox.Show(
                        QueryLangDict("Message_CanNotWriteConfigfile"),
                        QueryLangDict("Message_Title_Error")
                    );
                    return;
                }
            }
            else if (ini.Read("Versions", "ConfigFile") != IniConfigFileVersion || File.ReadAllBytes(ini.IniFilePath).Length == 0)
            {
                var result = MessageBox.Show(
                    QueryLangDict("Message_CreatNewConfigfile"),
                    QueryLangDict("Message_Title_Hint"),
                    MessageBoxButton.YesNo
                );
                if (result == MessageBoxResult.Yes)
                {
                    ini.CreatFile();
                }
            }
            else
            {
                ini.Write("Versions", "ConfigFile", IniConfigFileVersion);
                ini.Write("Window", "Width", this.Width);
                ini.Write("Window", "Height", this.Height);
                ini.Write("Command", "AppPath", UiData.AppPath);
                ini.Write("Command", "ArgsTemplet", UiData.ArgsTemplet);
                ini.Write("Command", "UserArgs", UiData.UserArgs);
                ini.Write("Output", "Extension", UiData.OutputExtension);
                ini.Write("Output", "Suffix", UiData.OutputSuffix);
                ini.Write("Output", "Floder", UiData.OutputFloder);
                ini.Write("Process", "Priority", UiData.Priority);
                ini.Write("Process", "ThreadCount", UiData.ThreadCount);
                ini.Write("Process", "WindowStyle", UiData.WindowStyle);
                ini.Write("Process", "SimulateCmd", UiData.SimulateCmd);
            }
        }
    }

    public class MainWindowData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string DefaultTitle;

        private string _appPath;
        public string AppPath
        {
            get => _appPath;
            set { _appPath = value; NotifyPropertyChanged("AppPath"); }
        }

        private string _argsTemplet;
        public string ArgsTemplet
        {
            get => _argsTemplet;
            set { _argsTemplet = value; NotifyPropertyChanged("ArgsTemplet"); }
        }

        private string _userArgs;
        public string UserArgs
        {
            get => _userArgs;
            set { _userArgs = value; NotifyPropertyChanged("UserArgs"); }
        }

        private string _outputSuffix = "_Output";
        public string OutputSuffix
        {
            get => _outputSuffix;
            set { _outputSuffix = value; NotifyPropertyChanged("OutputSuffix"); }
        }

        private string _outputExtension;
        public string OutputExtension
        {
            get => _outputExtension;
            set { _outputExtension = value; NotifyPropertyChanged("OutputExtension"); }
        }

        private string _outputFloder;
        public string OutputFloder
        {
            get => _outputFloder;
            set { _outputFloder = value; NotifyPropertyChanged("OutputFloder"); }
        }

        private int _priority = 3;
        public int Priority
        {
            get => _priority;
            set { _priority = value; NotifyPropertyChanged("Priority"); }
        }

        private int _threadCount = 1;
        public int ThreadCount
        {
            get => _threadCount;
            set { _threadCount = value; NotifyPropertyChanged("ThreadCount"); }
        }

        private int _windowStyle = 1;
        public int WindowStyle
        {
            get => _windowStyle;
            set { _windowStyle = value; NotifyPropertyChanged("WindowStyle"); }
        }

        private int _simulateCmd = 1;
        public int SimulateCmd
        {
            get => _simulateCmd;
            set { _simulateCmd = value; NotifyPropertyChanged("SimulateCmd"); }
        }

        private bool _taskRunning = false;
        public bool TaskRunning
        {
            get => _taskRunning;
            set
            {
                _taskRunning = value;
                ConfigVariable = !value;
                NotifyPropertyChanged("TaskRunning");
            }
        }

        public bool ConfigVariable
        {
            get => !TaskRunning;
            set => NotifyPropertyChanged("ConfigVariable"); // Throw set value
        }

        private string _cpuUsage = "--%";
        public string CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; NotifyPropertyChanged("CpuUsage"); }
        }

        private string _ramUsage = "--%";
        public string RamUsage
        {
            get => _ramUsage;
            set { _ramUsage = value; NotifyPropertyChanged("RamUsage"); }
        }
    }
}
