using System;
using Newtonsoft.Json;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using JetBrains.Annotations;

namespace Divvun.Installer.Models
{
    abstract class Config<TFile>
    {
        public readonly string FilePath;

        private TFile _state;
        private BehaviorSubject<TFile> _changeSubject;

        internal Config(string path) {
            FilePath = path;

            var parentPath = Path.GetPathRoot(path);
            Directory.CreateDirectory(parentPath);

            // Assume the file may not exist.
            string jsonString;
            try {
                jsonString = File.ReadAllText(path, Encoding.UTF8);
            } catch (Exception e) {
                jsonString = "{}";
            }
            
            _state = JsonConvert.DeserializeObject<TFile>(jsonString);
            _changeSubject = new BehaviorSubject<TFile>(_state);
        }

        public void Save() {
            var data = JsonConvert.SerializeObject(_state);
            File.WriteAllText(FilePath, data, Encoding.UTF8);
        }

        public void Mutate(Action<TFile> mutator) {
            mutator(_state);
            Save();
            _changeSubject.OnNext(_state);
        }

        public IObservable<TFile> Observe() {
            return _changeSubject.AsObservable();
        }
    }

    class SettingsFile
    {
        internal string? Language = null;
    }
    
    class Settings : Config<SettingsFile>
    {
        public static Settings Create() {
            return new Settings("");
        }
        
        private Settings([NotNull] string path) : base(path) { }

        public IObservable<string?> Language => Observe()
            .Select(x => x.Language)
            .DistinctUntilChanged();
    }
}