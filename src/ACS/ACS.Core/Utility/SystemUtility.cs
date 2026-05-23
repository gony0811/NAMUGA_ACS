using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Diagnostics;
using System.Xml;

namespace ACS.Utility
{
    public class SystemUtility
    {
        public static long FreeDiskSpaceKb(string path)
        {
            try
            {

            }
            catch(Exception e)
            {

            }

            return 0;
        }

        public static T Execute<T>(Func<T> function, TimeSpan timeout, Func<T> onTimeout)
        {
            Task<T> task = Task.Run(function);
            if (task.Wait(timeout))
            {
                // the function returned in time
                return task.Result;
            }
            else
            {
                // the function takes longer than the timeout
                return onTimeout();
            }
        }

        /// <summary>
        /// 장기 실행 서버 프로세스(.exe)를 독립(detached)으로 기동한다. fire-and-forget —
        /// 종료를 기다리지 않고 ExitCode도 확인하지 않는다. (START 전용)
        /// </summary>
        public static void StartProcess(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("filePath is empty", nameof(filePath));
            }

            ProcessStartInfo procStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,   // 호출자와 분리된 독립 프로세스로 기동
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty
            };

            Process.Start(procStartInfo);
        }

        /// <summary>
        /// cmd.exe를 통해 단명령(taskkill, coredump, getprocessid 등)을 실행하고 종료까지 대기한다.
        /// 표준출력을 캡처해 최대 max줄까지 반환하며, 종료코드가 0이 아니면 예외를 던진다.
        /// 장기 실행 서버 기동에는 <see cref="StartProcess"/>를 사용할 것.
        /// </summary>
        public static List<string> PerformCommand(string[] commandAttributes, int max)
        {
            List<string> lines = new List<string>();

            if (commandAttributes == null || commandAttributes.Length == 0 || string.IsNullOrEmpty(commandAttributes[0]))
            {
                throw new PerformCommandException("command is empty", commandAttributes);
            }

            Process process = null;
            ProcessStartInfo procStartInfo = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                foreach (string attribute in commandAttributes)
                {
                    sb.Append(attribute);
                    sb.Append(" ");
                }

                procStartInfo = new ProcessStartInfo();
                procStartInfo.FileName = "cmd.exe";
                procStartInfo.CreateNoWindow = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.RedirectStandardInput = true;
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;

                process = new Process();
                process.StartInfo = procStartInfo;
                process.Start();

                process.StandardInput.WriteLine(sb.ToString());
                process.StandardInput.Close();

                // WaitForExit 전에 출력을 먼저 읽어 파이프 버퍼 데드락을 방지.
                string stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (string raw in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (max >= 0 && lines.Count >= max)
                    {
                        break;
                    }
                    lines.Add(raw.Trim().ToLower());
                }

                if (process.ExitCode != 0)
                {
                    throw new PerformCommandException(
                        "Command line returned OS error code '" + sb.ToString() + "' " + process.ExitCode,
                        process.ExitCode, commandAttributes);
                }
            }
            catch (PerformCommandException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PerformCommandException(
                    "Command line threw an Unknown Exception '" + e.Message + "' for command " + sb.ToString(), commandAttributes);
            }
            finally
            {
                if(process != null)
                    process.Close();
            }

            return lines;
        }

        public static string GetProcessId(string applicationName)
        {
            string processId = "";

            Process[] processes = Process.GetProcesses();

            foreach(Process process in processes)
            {
                if(applicationName.Equals(process.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    processId = process.Id.ToString();
                    break;
                }
            }

            return processId;
        }

        public static bool KillProcess(string applicationName)
        {
            Process[] processes = Process.GetProcesses();

            foreach(Process process in processes)
            {
                if (applicationName.Equals(process.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    int processId = process.Id;

                    PerformCommand(new string[] { "taskKill", "-F", "-PID", processId.ToString() }, -1);
                    return true;
                }
            }

            // logger.warn("there is no application{" + applicationName + "}, can not kill process");
            return false;
        }


        public static string ToString(XmlDocument document)
        {
            if (document != null)
            {
                return document.InnerText;
            }
            return null;
        }

        public static string GetFullPathName(string sitePath, string relativePath)
        {
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string startUpPath = System.IO.Path.GetDirectoryName(exe);

            //string path = startUpPath + @"/" + sitePath + @"/" + relativePath;
            string path = startUpPath + @"/" + relativePath;
            path = path.Replace("@{site}", sitePath);
            return path;
        }

        public static string GetFullPathName(string relativePath)
        {
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string startUpPath = System.IO.Path.GetDirectoryName(exe);
            string path = startUpPath + @"/" + relativePath;

            return path;
        }


    }

    public class IdGeneratorUtils
    {
        public static string RandomTransactionId()
        {
            Random random = new Random();

            return DateTime.Now.Millisecond.ToString() + random.Next(1, 5);
        }
    }
}
