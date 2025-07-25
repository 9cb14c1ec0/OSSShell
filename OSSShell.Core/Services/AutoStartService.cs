using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OSSShell.Core.Services
{
    public class AutoStartService
    {
        public void StartLatestSimplyShip()
        {
            try
            {
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var simplyShipPath = Path.Combine(programFilesPath, "SimplyShip");

                if (!Directory.Exists(simplyShipPath))
                {
                    return;
                }

                var versionDirs = Directory.GetDirectories(simplyShipPath)
                    .Select(dir => new
                    {
                        Path = dir,
                        VersionMatch = Regex.Match(Path.GetFileName(dir), @"^(\d+)\.(\d+)$")
                    })
                    .Where(x => x.VersionMatch.Success)
                    .Select(x => new
                    {
                        x.Path,
                        Major = int.Parse(x.VersionMatch.Groups[1].Value),
                        Minor = int.Parse(x.VersionMatch.Groups[2].Value)
                    })
                    .OrderByDescending(x => x.Major)
                    .ThenByDescending(x => x.Minor)
                    .FirstOrDefault();

                if (versionDirs != null)
                {
                    var exePath = Path.Combine(versionDirs.Path, "shipping.exe");
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = versionDirs.Path,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start SimplyShip: {ex.Message}");
            }
        }
    }
}