using System;
using System.IO;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Pahkat.Sdk.Rpc;

namespace Divvun.Installer.Models {

public abstract class Config<TFile> {
    public readonly string FilePath;
    private readonly BehaviorSubject<TFile> _changeSubject;

    private readonly TFile _state;

    internal Config(string path) {
        FilePath = path;

        var parentPath = Path.GetPathRoot(path);
        Directory.CreateDirectory(parentPath);

        // Assume the file may not exist or contains invalid data.
        string jsonString;
        TFile state;
        try {
            jsonString = File.ReadAllText(path, Encoding.UTF8);
            state = JsonConvert.DeserializeObject<TFile>(jsonString);
            if (state == null) {
                throw new Exception("Invalid JSON file");
            }
        }
        catch (Exception e) {
            jsonString = "{}";
            state = JsonConvert.DeserializeObject<TFile>(jsonString);
            Debug.Assert(state != null);
        }

        _state = state;
        _changeSubject = new BehaviorSubject<TFile>(_state);
    }

    public void Save() {
        var data = JsonConvert.SerializeObject(_state, Json.Settings.Value);
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

public class SettingsFile {
    public string? Language = null;
    public Uri? SelectedRepository = null;
}

public class Settings : Config<SettingsFile> {
    private Settings([NotNull] string path) : base(path) {
    }

    public IObservable<string?> Language => Observe()
        .Select(x => x.Language)
        .DistinctUntilChanged();

    public IObservable<Uri?> SelectedRepository => Observe()
        .Select(x => x.SelectedRepository)
        .DistinctUntilChanged();

    public static Settings Create() {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var projectPath = Path.Combine(appData, "Divvun Manager");
        Directory.CreateDirectory(projectPath);
        return new Settings(Path.Combine(projectPath, "settings.json"));
    }

    public string? GetLanguage() {
        return Language.Take(1).ToTask().GetAwaiter().GetResult();
    }

    public Uri? GetSelectedRepository() {
        return SelectedRepository.Take(1).ToTask().GetAwaiter().GetResult();
    }
}

}
