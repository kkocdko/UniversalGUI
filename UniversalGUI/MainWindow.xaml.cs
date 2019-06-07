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
using System.Windows.Media;

namespace UniversalGUI
{
    // About UI
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = uiData = new MainWindowData();
            DefaultTitle = this.Title;
            IniConfigManager = new IniManager(GetIniConfigFile());
            ImputIniConfig(IniConfigManager);
            SetLanguage();
        }

        private readonly string DefaultTitle;

        public MainWindowData uiData;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartMonitorAsync();
        }

        private void MainWindow_WindowClosing(object sender, CancelEventArgs e)
        {
            StartTaskButton.Focus(); //Ensure that the contents of the window being modified are saved
            SaveIniConfig(IniConfigManager);
            StopTask();
        }

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
            if (!uiData.ConfigVariable)
            {
                StopTask();
                return;
            }
            StartTaskButton.Content = QueryLangDict("Button_StartTask_Stop");
            uiData.ConfigVariable = false;
            SetProgress(0);
            SumFilesConfig();
            bool settingLegal = CheckConfig();
            await Task.Run(StartTask);
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
            uiData.ConfigVariable = true;
            SetProgress();
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
                TaskProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
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
            await Task.Run(async () =>
            {
                var cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuPerformanceCounter.NextValue();
                double cpuUseRatio;
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                double usedRam;
                string ramUnit = "KB";
                double ramUseRatio;
                while (true)
                {
                    cpuUseRatio = Math.Round(cpuPerformanceCounter.NextValue());
                    usedRam = (double)computerInfo.TotalPhysicalMemory - computerInfo.AvailablePhysicalMemory;
                    ramUseRatio = Math.Round(usedRam / computerInfo.TotalPhysicalMemory * 100);
                    if (usedRam >= 1099511627776) //1TB
                    {
                        ramUnit = "TB";
                        usedRam /= 1099511627776; //1024^4
                        usedRam = Math.Round(usedRam, 3);
                    }
                    else if (usedRam >= 107374182400) //100GB
                    {
                        ramUnit = "GB";
                        usedRam /= 1073741824; //^3
                        usedRam = Math.Round(usedRam, 1);
                    }
                    else if (usedRam >= 10737418240) //10GB
                    {
                        ramUnit = "GB";
                        usedRam /= 1073741824; //^3
                        usedRam = Math.Round(usedRam, 2);
                    }
                    else if (usedRam >= 1073741824) //1GB
                    {
                        ramUnit = "GB";
                        usedRam /= 1073741824; //^3
                        usedRam = Math.Round(usedRam, 3);
                    }
                    else if (usedRam >= 104857600) //100MB
                    {
                        ramUnit = "MB";
                        usedRam /= 1048576;//^2
                        usedRam = Math.Round(usedRam, 1);
                    }
                    uiData.CpuUsage = cpuUseRatio + "%";
                    uiData.RamUsage = usedRam + ramUnit + " (" + ramUseRatio + "%)";
                    await Task.Delay(1000);
                }
            });
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
            else if (uiData.AppPath == "")
            {
                MessageBox.Show(
                    QueryLangDict("Message_CommandAppUnspecified"),
                    errorTitle
                );
                return false;
            }
            else if (uiData.OutputFloder == "" && uiData.OutputExtension == "" && uiData.OutputSuffix == "")
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
            else if (CustomThreadNumberItem.IsSelected == true)
            {
                if (Convert.ToInt32(CustomThreadNumberItem.Tag) == 0 || CustomThreadNumberTextBox.Text == "")
                {
                    MessageBox.Show(
                        QueryLangDict("Message_ThreadNumberIsIllegal"),
                        errorTitle
                    );
                    return false;
                }
            }
            else if (AppPath.Text.IndexOf(' ') != -1 && uiData.SimulateCmd == 2)
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
            sender.Focus();
        }

        private void InsertToArgsTemplet(string insertContent)
        {
            string text = uiData.ArgsTemplet;
            int selectionStart = ArgsTemplet.SelectionStart;
            text = text.Insert(selectionStart, insertContent);
            ArgsTemplet.Text = text;
            ArgsTemplet.SelectionStart = selectionStart + insertContent.Length;
            ArgsTemplet.Focus();
        }

        private void InsertArgsTempletMark(object senderObj, RoutedEventArgs e)
        {
            var sender = (MenuItem)senderObj;
            InsertToArgsTemplet(sender.Header.ToString());
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
                Convert.ToUInt32(CustomThreadNumberTextBox.Text);
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
                iniConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\UniversalGUI";
            }
            string IniConfigFile = Path.Combine(iniConfigFilePath, IniConfigFileName);
            return IniConfigFile;
        }

        private void ImputIniConfig(IniManager ini)
        {
            if (File.Exists(ini.IniFilePath) && File.ReadAllBytes(ini.IniFilePath).Length != 0)
            {
                if (ini.Read("Versions", "ConfigFile") == IniConfigFileVersion)
                {
                    try
                    {
                        string windowWidth = ini.Read("Window", "Width");
                        this.Width = Convert.ToDouble(windowWidth);
                        string windowHeight = ini.Read("Window", "Height");
                        this.Height = Convert.ToDouble(windowHeight);

                        uiData.AppPath = ini.Read("Command", "AppPath");
                        uiData.ArgsTemplet = ini.Read("Command", "ArgsTemplet");
                        uiData.UserArgs = ini.Read("Command", "UserArgs");
                        uiData.OutputExtension = ini.Read("Output", "Extension");
                        uiData.OutputSuffix = ini.Read("Output", "Suffix");
                        uiData.OutputFloder = ini.Read("Output", "Floder");
                        uiData.Priority = Convert.ToUInt16(ini.Read("Process", "Priority"));
                        ushort threadCount = Convert.ToUInt16(ini.Read("Process", "ThreadCount"));
                        if (threadCount > 8)
                        {
                            //Bug !!!
                            CustomThreadNumberTextBox.Text = threadCount.ToString();
                            //CustomThreadNumberItem.Tag = threadCount;
                            //CustomThreadNumberItem.IsSelected = true;
                        }
                        uiData.ThreadCount = threadCount;
                        uiData.WindowStyle = Convert.ToUInt16(ini.Read("Process", "WindowStyle"));
                        uiData.SimulateCmd = Convert.ToUInt16(ini.Read("Process", "SimulateCmd"));

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
                else
                {
                    MessageBox.Show(
                        QueryLangDict("Message_UseBuildInConfigfile"),
                        QueryLangDict("Message_Title_Hint")
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

            if (ini.Read("Versions", "ConfigFile") == IniConfigFileVersion || File.ReadAllBytes(ini.IniFilePath).Length == 0)
            {
                ini.Write("Versions", "ConfigFile", IniConfigFileVersion);
                ini.Write("Window", "Width", this.Width);
                ini.Write("Window", "Height", this.Height);
                ini.Write("Command", "AppPath", uiData.AppPath);
                ini.Write("Command", "ArgsTemplet", uiData.ArgsTemplet);
                ini.Write("Command", "UserArgs", uiData.UserArgs);
                ini.Write("Output", "Extension", uiData.OutputExtension);
                ini.Write("Output", "Suffix", uiData.OutputSuffix);
                ini.Write("Output", "Floder", uiData.OutputFloder);
                ini.Write("Process", "Priority", uiData.Priority);
                ini.Write("Process", "ThreadCount", uiData.ThreadCount);
                ini.Write("Process", "WindowStyle", uiData.WindowStyle);
                ini.Write("Process", "SimulateCmd", uiData.SimulateCmd);
            }
            else
            {
                var result = MessageBox.Show(
                    QueryLangDict("Message_CreatNewConfigfile"),
                    QueryLangDict("Message_Title_Hint"),
                    MessageBoxButton.YesNo
                );
                if (result == MessageBoxResult.Yes)
                {
                    ini.CreatFile();
                    SaveIniConfig(ini);
                }
            }

        }
    }

    public class MainWindowData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyValueChanged(string argName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(argName));
            }
        }

        private string _appPath;
        public string AppPath
        {
            get => _appPath;
            set { _appPath = value; NotifyValueChanged("AppPath"); }
        }

        private string _argsTemplet;
        public string ArgsTemplet
        {
            get => _argsTemplet;
            set { _argsTemplet = value; NotifyValueChanged("ArgsTemplet"); }
        }

        private string _userArgs;
        public string UserArgs
        {
            get => _userArgs;
            set { _userArgs = value; NotifyValueChanged("UserArgs"); }
        }

        private string _outputSuffix = "_Output";
        public string OutputSuffix
        {
            get => _outputSuffix;
            set { _outputSuffix = value; NotifyValueChanged("OutputSuffix"); }
        }

        private string _outputExtension;
        public string OutputExtension
        {
            get => _outputExtension;
            set { _outputExtension = value; NotifyValueChanged("OutputExtension"); }
        }

        private string _outputFloder;
        public string OutputFloder
        {
            get => _outputFloder;
            set { _outputFloder = value; NotifyValueChanged("OutputFloder"); }
        }

        private ushort _priority;
        public ushort Priority
        {
            get => _priority;
            set { _priority = value; NotifyValueChanged("Priority"); }
        }

        private ushort _threadCount;
        public ushort ThreadCount
        {
            get => _threadCount;
            set { _threadCount = value; NotifyValueChanged("ThreadCount"); }
        }

        private ushort _windowStyle;
        public ushort WindowStyle
        {
            get => _windowStyle;
            set { _windowStyle = value; NotifyValueChanged("WindowStyle"); }
        }

        private ushort _simulateCmd;
        public ushort SimulateCmd
        {
            get => _simulateCmd;
            set { _simulateCmd = value; NotifyValueChanged("SimulateCmd"); }
        }

        private bool _configVariable = true;
        public bool ConfigVariable
        {
            get => _configVariable;
            set { _configVariable = value; NotifyValueChanged("ConfigVariable"); }
        }

        private string _cpuUsage = "--%";
        public string CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; NotifyValueChanged("CpuUsage"); }
        }

        private string _ramUsage = "----GB (--%)";
        public string RamUsage
        {
            get => _ramUsage;
            set { _ramUsage = value; NotifyValueChanged("RamUsage"); }
        }
    }
}
