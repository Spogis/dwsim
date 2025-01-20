using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using DWSIM.FileStorage;

namespace DWSIM.AI.ConvergenceHelper
{
    public class Manager
    {

        public static ConvergenceHelper Instance = new ConvergenceHelper();

        public static FileDatabaseProvider Database = new FileDatabaseProvider();

        public static string HomeDirectory = Path.Combine(GlobalSettings.Settings.GetConfigFileDir(), "ConvergenceHelper");

        public static void Initialize()
        {
            if (!Directory.Exists(HomeDirectory)) { Directory.CreateDirectory(HomeDirectory); }
            var datadir = Path.Combine(HomeDirectory, "data");
            if (!Directory.Exists(datadir)) { Directory.CreateDirectory(datadir); }
            var modelsdir = Path.Combine(HomeDirectory, "models");
            if (!Directory.Exists(modelsdir)) { Directory.CreateDirectory(modelsdir); }
            var configdir = Path.Combine(HomeDirectory, "config");
            if (!Directory.Exists(configdir)) { Directory.CreateDirectory(configdir); }
            LoadSettings();

            var dbfile = Path.Combine(datadir, "data.db.zip");
            if (!File.Exists(dbfile))
            {
                Database.CreateDatabase();
                Database.GetDatabaseObject().GetCollection<ConvergenceHelperTrainingData>("TrainingData");
            }
            else
            {
                ZipFile.ExtractToDirectory(dbfile, datadir);
                var dbfile2 = Path.Combine(datadir, "data.db");
                Database.LoadDatabase(dbfile2);
                File.Delete(dbfile2);
            }

            FlowsheetSolver.FlowsheetSolver.FlowsheetCalculationFinished += FlowsheetSolver_FlowsheetCalculationFinished;

        }

        private static void FlowsheetSolver_FlowsheetCalculationFinished(object sender, EventArgs e, object extrainfo)
        {
            Task.Run(() => SaveDatabaseToFile());
        }

        public static void StoreData(ConvergenceHelperTrainingData data)
        {
            Task.Run(() =>
            {
                lock (Database)
                {
                    data.Hash = data.GetBase64StringHash();
                    var col = Database.GetDatabaseObject().GetCollection<ConvergenceHelperTrainingData>("TrainingData");
                    var entries = col.Query().Where(x => x.Hash == data.Hash);
                    if (entries.Count() == 0)
                    {
                        col.Insert(data);
                    }
                }
            });
        }

        public static void SaveDatabaseToFile()
        {
            var datadir = Path.Combine(HomeDirectory, "data");
            var zipfile = Path.Combine(datadir, "data.db.zip");
            var dbfile = Path.Combine(datadir, "data.db");
            Database.ExportDatabase(dbfile);
            using (var fstream = new FileStream(zipfile, FileMode.OpenOrCreate))
            {
                fstream.Position = 0;
                using (var archive = new ZipArchive(fstream, ZipArchiveMode.Create))
                    ZipFileExtensions.CreateEntryFromFile(archive, dbfile, "data.db", CompressionLevel.Optimal);
            }
            File.Delete(dbfile);
        }

        public static void LoadSettings()
        {
            var configfile = Path.Combine(HomeDirectory, "config", "settings.json");
            if (File.Exists(configfile))
            {

            }
        }

        public static void SaveSettings()
        {

        }

    }
}
