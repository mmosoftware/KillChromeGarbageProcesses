using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Timers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Management;


namespace KillChromeGarbageProcesses
{
    class ChromeProcessManager
    {
        private Timer _timer;

        public ChromeProcessManager()
        {
            _timer = new Timer(2000); // 2000 milliseconds = 2 seconds
            _timer.Elapsed += KillChromeGarbageProcesses;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void KillChromeGarbageProcesses(Object source, ElapsedEventArgs e)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} Start killing Chrome garbage processes");

                var chromeProcesses = Process.GetProcessesByName("chrome");

                if (chromeProcesses.Length == 0)
                {
                    Console.WriteLine("No Chrome processes found");
                    return;
                }

                List<ChromeProcessInfo> processInfoList = new List<ChromeProcessInfo>();

                foreach (var proc in chromeProcesses)
                {
                    var cmdline = GetCommandLine(proc);
                    var processInfo = ExtractProcessData(proc, cmdline);
                    processInfoList.Add(processInfo);
                }

                var groupedByUserDir = processInfoList.GroupBy(p => p.UserDataDir)
                    .Select(group => new { UserDataDir = group.Key, Processes = group.ToList() })
                    .ToList();

                foreach (var group in groupedByUserDir)
                {
                    foreach (var procInfo in group.Processes)
                    {
                        procInfo.DirGroupCount = group.Processes.Count;
                    }
                }

                var pidsToKill = processInfoList
                    .Where(p => new[] { "1", "2", "3", "4", "5" }.Contains(p.DirGroupCount.ToString()))
                    .Where(p => (DateTime.UtcNow - p.CreateTime).TotalSeconds > 10)
                    .Select(p => p.Pid)
                    .ToList();

                KillProcesses(pidsToKill);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string GetCommandLine(Process process)
        {
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            {
                var matchEnum = searcher.Get().GetEnumerator();
                if (matchEnum.MoveNext())
                {
                    return matchEnum.Current["CommandLine"]?.ToString();
                }
            }
            return null;
        }

        private ChromeProcessInfo ExtractProcessData(Process proc, string cmdline)
        {
            var processInfo = new ChromeProcessInfo
            {
                Pid = proc.Id,
                Name = proc.ProcessName,
                CreateTime = proc.StartTime
            };

            if (!string.IsNullOrEmpty(cmdline))
            {
                var args = cmdline.Split(' ');
                foreach (var arg in args)
                {
                    if (arg.StartsWith("--renderer-client-id="))
                    {
                        processInfo.RendererClientId = arg.Split('=')[1];
                    }
                    else if (arg.StartsWith("--user-data-dir="))
                    {
                        processInfo.UserDataDir = arg.Split('=')[1];
                    }
                }
            }

            return processInfo;
        }

        private void KillProcesses(List<int> pids)
        {
            foreach (var pid in pids)
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill();
                    Console.WriteLine($"Killed process {pid}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error killing process {pid}: {ex.Message}");
                }
            }
        }

        public class ChromeProcessInfo
        {
            public int Pid { get; set; }
            public string Name { get; set; }
            public DateTime CreateTime { get; set; }
            public string RendererClientId { get; set; }
            public string UserDataDir { get; set; }
            public int DirGroupCount { get; set; }
        }
    }
}
