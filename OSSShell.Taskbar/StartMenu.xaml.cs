using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using OSSShell.Interop;

namespace OSSShell.Taskbar
{
    public partial class StartMenu : Window
    {
        private class AppInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public BitmapSource Icon { get; set; }
        }

        public StartMenu()
        {
            InitializeComponent();
            PopulateMenu();
        }

        private void PopulateMenu()
        {
            AddSimplyShipVersions();
            AddApplication("RustDesk", @"C:\Program Files\RustDesk\rustdesk.exe", Colors.DarkOrange);
            AddApplication("AnyDesk", @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe", Colors.DarkRed);
        }

        private void AddSimplyShipVersions()
        {
            try
            {
                var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var simplyShipPath = Path.Combine(programFilesPath, "SimplyShip");

                if (!Directory.Exists(simplyShipPath))
                {
                    return;
                }

                var versions = Directory.GetDirectories(simplyShipPath)
                    .Select(dir => new
                    {
                        Path = dir,
                        VersionMatch = Regex.Match(Path.GetFileName(dir), @"^(\d+)\.(\d+)$")
                    })
                    .Where(x => x.VersionMatch.Success)
                    .Select(x => new
                    {
                        x.Path,
                        Version = Path.GetFileName(x.Path),
                        Major = int.Parse(x.VersionMatch.Groups[1].Value),
                        Minor = int.Parse(x.VersionMatch.Groups[2].Value)
                    })
                    .OrderByDescending(x => x.Major)
                    .ThenByDescending(x => x.Minor);

                foreach (var version in versions)
                {
                    var exePath = Path.Combine(version.Path, "shipping.exe");
                    if (File.Exists(exePath))
                    {
                        AddApplication($"SimplyShip {version.Version}", exePath, Colors.DarkBlue);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to enumerate SimplyShip versions: {ex.Message}");
            }
        }

        private void AddApplication(string name, string path, System.Windows.Media.Color tileColor)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var appInfo = new AppInfo
            {
                Name = name,
                Path = path,
                Icon = ExtractIcon(path)
            };

            var button = new Button
            {
                Content = name,
                Width = 150,
                Height = 150,
                Style = (Style)FindResource("TileButton"),
                Background = new SolidColorBrush(tileColor),
                Tag = appInfo
            };

            button.Click += ApplicationButton_Click;

            TilesPanel.Children.Add(button);
        }

        private BitmapSource ExtractIcon(string filePath)
        {
            try
            {
                System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch
            {
            }
            
            // Return a default icon if extraction fails
            return BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4);
        }

        private void ApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AppInfo appInfo)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = appInfo.Path,
                        WorkingDirectory = Path.GetDirectoryName(appInfo.Path),
                        UseShellExecute = true
                    });
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => Close()));
        }

        private void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to reboot the system?", "Confirm Reboot", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    Close();
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reboot: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to log out?", "Confirm Log Out", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    Close();
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/l",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to log out: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}