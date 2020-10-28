using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Pahkat.Sdk;
using Serilog;

namespace Divvun.Installer.Service
{
    public class LogCollator: IDisposable
    {
        private ZipArchive _file;
        private List<string> logList = new List<string>();
        
        private LogCollator(ZipArchive zipFile) {
            _file = zipFile;
        }
        
        public static async Task Run(string fileName) {
            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
            
            var zipFile = ZipFile.Open(fileName, ZipArchiveMode.Create, Encoding.UTF8);
            if (zipFile == null) {
                MessageBox.Show("Could not save zip file. Choose another directory.");
                return;
            }
            
            var collator = new LogCollator(zipFile);
            await collator.Run();
        }

        private void Finish() {
            AddLog($"Finishing collation at {DateTime.Now}.");
            var entry = _file.CreateEntry("run.log", CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                foreach (var line in logList) {
                    writer.WriteLine(line);
                }
            }
            Dispose();
        }

        private void ZipDirectory(string dirPath, string innerPath, string name) {
            try {
                if (!Directory.Exists(dirPath)) {
                    AddLog($"No {name} logs exist at {dirPath}");
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(dirPath)) {
                    AddLog($"Adding {file}");
                    var entry = _file.CreateEntry(@$"{innerPath}\{Path.GetFileName(file)}", CompressionLevel.Optimal);
                    using var reader = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using StreamWriter writer = new StreamWriter(entry.Open());

                    reader.CopyTo(writer.BaseStream);
                }
            }
            catch (Exception e) {
                AddLog($"There was an exception for {name}: {e}");
            }
        }

        private void ZipLogs() {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ZipDirectory(@"C:\ProgramData\Pahkat\log", "pahkat-log", "Pahkat Service");
            ZipDirectory(@"C:\ProgramData\Pahkat\config", "pahkat-config", "Pahkat Service (config)");
            ZipDirectory(@"C:\ProgramData\WinDivvun\log", "windivvun", "WinDivvun");
            ZipDirectory(Path.Combine(appdata, @"Divvun Manager\log"), "divvun-manager", "Divvun Manager");
            ZipDirectory(Path.Combine(appdata, @"kbdi\log"), "kbdi", "kbdi");
        }

        void AddLog(string message) {
            logList.Add(message);
            Log.Information(message);
        }

        private async Task CollectPahkatData() {
            AddLog("Collecting Pahkat installation data");
            
            var results = new Dictionary<PackageKey, PackageStatus>();

            try {
                var store = PahkatApp.Current.PackageStore;

                var repos = await store.RepoIndexes();
                foreach (var repo in repos.Values) {
                    foreach (var package in repo.Packages.Packages.Values) {
                        var key = repo.PackageKey(package!);
                        var status = await store.Status(key);
                        results.Add(key, status);
                    }
                }
                
                AddLog($"Adding packages.log");
                var entry = _file.CreateEntry("packages.log", CompressionLevel.Optimal);
                using StreamWriter writer = new StreamWriter(entry.Open());

                foreach (var pair in results) {
                    writer.WriteLine($"{pair.Key}: {pair.Value}");
                }
                AddLog($"Written packages.log");
            }
            catch (Exception e) {
                AddLog($"There was an exception collecting Pahkat data: {e}");
            }
        }

        private string RegArgs(bool is32Bit, string tmpFile) {
            if (is32Bit) {
                return $"export HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall \"{tmpFile}\" /reg:32 /y";
            }
            else {
                return $"export HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall \"{tmpFile}\" /reg:64 /y";
            }
        }

        void WriteFile(string name, string path) {
            var entry = _file.CreateEntry(name, CompressionLevel.Optimal);
            using var reader = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamWriter writer = new StreamWriter(entry.Open());
            reader.CopyTo(writer.BaseStream);
        }
        
        void ScrapeUninstallKeys() {
            AddLog("Collecting Uninstall keys");
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            
            try {
                var regFile32 = Path.GetTempFileName();
                var reg32 = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        CreateNoWindow = true,
                        FileName = "reg",
                        Arguments = RegArgs(true, regFile32),
                        WorkingDirectory = desktop
                    }
                };

                var regFile64 = Path.GetTempFileName();
                var reg64 = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        CreateNoWindow = true,
                        FileName = "reg",
                        Arguments = RegArgs(false, regFile64),
                        WorkingDirectory = desktop
                    }
                };

                reg32.Start();
                reg32.WaitForExit();

                reg64.Start();
                reg64.WaitForExit();
                
                WriteFile("uninstall_32.reg", regFile32);
                WriteFile("uninstall_64.reg", regFile64);
            
                AddLog("Done collecting Uninstall keys");
            }
            catch (Exception e) {
                AddLog($"Error running reg: {e}");
            }
        }

        private async Task Run() {
            AddLog($"Starting collation at {DateTime.Now}.");

            await CollectPahkatData();
            ScrapeUninstallKeys();
            ZipLogs();
            
            Finish();
        }

        public void Dispose() {
            _file.Dispose();
        }
    }
}