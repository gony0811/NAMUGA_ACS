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

        public static List<string> PerformCommand(string[] commandAttributes, int max)
        {
            List<string> lines = new List<string>();
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

                //sb.Append(commandAttributes[0] + " ");

                procStartInfo = new ProcessStartInfo();
                procStartInfo.FileName = @"cmd ";
                procStartInfo.CreateNoWindow = false;
                procStartInfo.UseShellExecute = false;
                procStartInfo.RedirectStandardInput = true;
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;

                process = new Process();
                process.StartInfo = procStartInfo;            
                process.Start();

                process.StandardInput.Write(sb.ToString() + Environment.NewLine);
                process.StandardInput.Close();
                //string line = process.StandardOutput.ReadLine();

                //while (!(string.IsNullOrEmpty(line)) && (lines.Count < max))
                //{
                //    line = line.ToLower().Trim();
                //    lines.Add(line);
                //    line = process.StandardOutput.ReadLine();
                //}                 

                if(process.ExitCode != 0)
                {
                    throw new PerformCommandException(
                        "Command line returned OS error code '" + sb.ToString() + process.ExitCode, commandAttributes);
                }
            }
            catch (Exception e)
            {
                throw new PerformCommandException(
                    "Command line throw and Unknown Exception '" + e.Message + "' for command " + sb.ToString(), commandAttributes);
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
