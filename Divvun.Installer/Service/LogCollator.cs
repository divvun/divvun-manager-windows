using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;

namespace Divvun.Installer.Service
{
    public class LogCollator: IDisposable
    {
        private ZipArchive _file;
        private List<string> logList = new List<string>();
        
        private LogCollator(ZipArchive zipFile) {
            _file = zipFile;
        }
        
        public static void Run(string fileName) {
            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }
            
            var zipFile = ZipFile.Open(fileName, ZipArchiveMode.Create, Encoding.UTF8);
            if (zipFile == null) {
                MessageBox.Show("Could not save zip file. Choose another directory.");
                return;
            }
            
            var collator = new LogCollator(zipFile);
            collator.Run();
        }

        private void Finish() {
            logList.Add($"Finishing collation at {DateTime.Now}.");
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
            
            if (!Directory.Exists(dirPath)) {
                logList.Add($"No {name} logs exist at {dirPath}");
                return;
            }

            foreach (var file in Directory.EnumerateFiles(dirPath)) {
                logList.Add($"Adding {file}");
                var entry = _file.CreateEntry(@$"{innerPath}\{Path.GetFileName(file)}", CompressionLevel.Optimal);
                using var reader = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamWriter writer = new StreamWriter(entry.Open());
                
                reader.CopyTo(writer.BaseStream);
            }
        }
        
        private void AddPahkatServiceLogs() {
            ZipDirectory(@"C:\ProgramData\Pahkat\log", "pahkat", "Pahkat Service");
        }

        private void Run() {
            logList.Add($"Starting collation at {DateTime.Now}.");

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ZipDirectory(@"C:\ProgramData\Pahkat\log", "pahkat", "Pahkat Service");
            ZipDirectory(@"C:\ProgramData\WinDivvun\log", "windivvun", "WinDivvun");
            ZipDirectory(Path.Combine(appdata, @"Divvun Installer\log"), "divvun-installer", "Divvun Installer");
            Finish();
        }

        public void Dispose() {
            _file.Dispose();
        }
    }
}