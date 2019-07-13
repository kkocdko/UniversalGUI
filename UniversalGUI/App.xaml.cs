using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UniversalGUI
{
    public partial class App : Application { }

    public partial class MainWindow : Window
    {
        private TaskFiles taskFiles = new TaskFiles();

        private int[] processIds = { };

        public void StartTask()
        {
            int threadCount = UiData.ThreadCount;
            processIds = new int[threadCount];
            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = CreateThreadAsync(i);
            }
            Task.WaitAll(tasks);
        }

        public void StopTask()
        {
            taskFiles = new TaskFiles();
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
                catch (ArgumentException e)
                {
                    Debug.WriteLine("Maybe the process [" + processIds[i] + "] isn't running. Exception message: " + e.Message);
                }
                catch (Win32Exception e)
                {
                    MessageBox.Show(
                        "Can't kill the process [" + processIds[i] + "] . You can try again. Exception message: " + e.Message,
                        QueryLangDict("Message_Title_Error")
                    );
                }
            }
        }

        public async Task CreateThreadAsync(int threadIndex)
        {
            while (true)
            {
                string currentFile = taskFiles.Current;
                if (currentFile == null) break;
                string appArgs = SumAppArgs(
                    argsTemplet: UiData.ArgsTemplet,
                    inputFile: currentFile,
                    userArgs: UiData.UserArgs,
                    outputSuffix: UiData.OutputSuffix,
                    outputExtension: UiData.OutputExtension,
                    outputFloder: UiData.OutputFloder
                );
                Process process = CreateProcess(
                    appPath: UiData.AppPath,
                    appArgs: appArgs,
                    windowStyle: UiData.WindowStyle,
                    priority: UiData.Priority,
                    simulateCmd: UiData.SimulateCmd
                );
                if (process == null) break;
                processIds[threadIndex] = process.Id;
                await Task.Run(process.WaitForExit);
                SetProgress(taskFiles.CompletionRate);
            }
        }

        private string SumAppArgs(string argsTemplet, string inputFile, string userArgs, string outputSuffix, string outputExtension, string outputFloder)
        {
            var regexQuotations = new Regex("(^\")|(\"$)");
            var removeQuotations = new Func<string, string>(str => regexQuotations.Replace(str, ""));
            var addQuotations = new Func<string, string>(str => "\"" + str + "\"");

            inputFile = removeQuotations(inputFile);
            argsTemplet = removeQuotations(argsTemplet);
            outputSuffix = removeQuotations(outputSuffix);
            outputExtension = removeQuotations(outputExtension);
            outputFloder = removeQuotations(outputFloder);

            string args = argsTemplet;

            { //{UserArgs}
                args = new Regex("{UserArgs}").Replace(args, userArgs);
            }

            { //{InputFile}
                args = new Regex("{InputFile}").Replace(args, addQuotations(inputFile));
            }

            { //{OutputFile}
                string outputFile;
                string mainName = new Regex(@"^.+(?=\..+?$)").Match(inputFile).ToString();
                mainName += outputSuffix;
                string extension = outputExtension == ""
                    ? new Regex(@"[^\.]+$").Match(inputFile).ToString() //Source extension
                    : new Regex(@"\.").Replace(outputExtension, ""); //Remove dot before the extension
                outputFile = mainName + "." + extension;
                if (outputFloder != "")
                {
                    //Remove "/" or "\"
                    outputFloder = new Regex(@"[\\/]$").Replace(outputFloder, "");
                    //Add "\"
                    outputFloder += @"\";
                    //Replace the output path
                    outputFile = new Regex(@"^.+\\").Replace(outputFile, outputFloder);
                }
                outputFile = addQuotations(outputFile);
                args = new Regex("{OutputFile}").Replace(args, outputFile);
            }
            Debug.WriteLine(args);
            return args;
        }

        private Process CreateProcess(string appPath, string appArgs, int windowStyle, int priority, int simulateCmd)
        {
            var process = new Process();
            switch (simulateCmd)
            {
                case 1:
                    process.StartInfo.FileName = appPath;
                    process.StartInfo.Arguments = appArgs;
                    break;
                case 2:
                case 3:
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c " + appPath + " " + appArgs; // Shouldn't add quotations
                    break;
                    //case 3:
                    //    process.StartInfo.FileName = "cmd.exe";
                    //    process.StartInfo.Arguments = "/c " + appPath + " " + appArgs;
                    //    // 获取输出流，写入到日志中
                    //    break;
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
            catch (Win32Exception e)
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
    }

    public class TaskFiles
    {
        public TaskFiles(ItemCollection itemCollection)
        {
            foreach (object item in itemCollection)
            {
                List.AddLast((string)item);
            }
            Enumerator = List.GetEnumerator();
            Count = List.Count;
        }

        public TaskFiles()
        {
            Enumerator = List.GetEnumerator();
            Count = List.Count;
        }

        private LinkedList<string> List = new LinkedList<string>();

        private IEnumerator<string> Enumerator;

        private int Count;

        private int CompletedCount = 0;

        public string Current
        {
            get
            {
                if (Enumerator.MoveNext()) // Side effect!
                {
                    CompletedCount++;
                    return Enumerator.Current;
                }
                else
                {
                    return null;
                }
            }
        }

        public double CompletionRate
        {
            get => Count > 0 ? (double)CompletedCount / Count : 0;
        }
    }
}
