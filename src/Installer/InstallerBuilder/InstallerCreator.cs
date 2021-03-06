﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static InstallerBuilder.IOMethods;
using static InstallerBuilder.Program;

namespace InstallerBuilder
{
    public static class InstallerCreator
    {
        public static void WriteFile(string version)
        {
            CreateResultsFolder();

            var text = GetFileText(version);
            var filePath = Path.Combine(OutputFolder, "build.iss");
            File.WriteAllText(filePath, text);

            BuildInstaller(filePath);
        }

        private static void BuildInstaller(string issPath)
        {
            var result = Path.GetFullPath(ResultsFolderPath());
            var iss = Path.GetFullPath(issPath);

            var info = new ProcessStartInfo
            {
                UseShellExecute = false,
                WorkingDirectory = GetInnoSetupPath(),
                FileName = Path.Combine(GetInnoSetupPath(), "iscc.exe"),
                Arguments = $"/O\"{result}\" \"{iss}\""
            };

            var process = Process.Start(info);
            process.WaitForExit();
        }

        private static string GetInnoSetupPath()
        {
            return XDocument.Load("paths.xml")
                .Root
                .Element("InnoSetupDirectory")
                .Value;
        }

        private static string GetFileText(string version)
        {
            return File.ReadAllText("template.iss")
                       .Replace("OutputBaseFilename=", $"OutputBaseFilename=QSimPlanner_{version}_setup")
                       .Replace("AppVersion=", $"AppVersion={version}")
                       .Replace("[Files]", "[Files]\n" + FileList());
        }

        private static string FileList()
        {
            var files = AllFiles(OutputFolder)
                .Select(Path.GetFullPath)
                .Select(p => Tuple.Create(p, RelativePath(p, OutputFolder)));

            var lines = files
                 .Select(f =>
                 $"Source: \"{f.Item1}\"; " +
                 $"DestDir: \"{{app}}\\{Path.GetDirectoryName(f.Item2)}\";" +
                 "Flags: ignoreversion");

            return string.Join("\n", lines);
        }

        public static string ResultsFolderPath()
        {
            return Path.Combine(OutputFolder, "../Results");
        }

        private static void CreateResultsFolder()
        {
            var path = ResultsFolderPath();
            ClearDirectory(path);
            Directory.CreateDirectory(path);
        }
    }
}
