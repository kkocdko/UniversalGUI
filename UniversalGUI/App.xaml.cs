using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace UniversalGUI
{
    public partial class App : Application
    {
    }

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

                await Task.Run(process.WaitForExit);

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
