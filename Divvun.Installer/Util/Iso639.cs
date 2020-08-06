using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Iterable;

namespace Divvun.Installer.Util
{
    public class Iso639Data
    {
        public string? Tag1;
        public string? Tag3;
        public string? Name;
        public string? Autonym;
        public string? Source;
    }

    public sealed class Iso639DataMap : ClassMap<Iso639Data>
    {
        public Iso639DataMap()
        {
            Map(m => m.Tag1).Name("tag1");
            Map(m => m.Tag3).Name("tag3");
            Map(m => m.Name).Name("name");
            Map(m => m.Autonym).Name("autonym");
            Map(m => m.Source).Name("source");
        }
    }

    public static class Iso639
    {
        private static Iso639Data[] _data;

        public static Iso639Data? GetTag(string tag) {
            if (_data == null) {
                var uri = new Uri("pack://application:,,,/Util/iso639-3_native.tsv");
                var reader = new StreamReader(Application.GetResourceStream(uri)?.Stream ?? throw new NullReferenceException());
                var csv = new CsvHelper.CsvReader(reader);
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.RegisterClassMap<Iso639DataMap>();
                _data = csv.GetRecords<Iso639Data>().ToArray();
            }

            return _data.First(x => x.Tag1 == tag) ?? _data.First(y => y.Tag3 == tag);
        }
    }
}
