using System;
using System.Diagnostics;
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
using System.Runtime.InteropServices;
using Microsoft.Win32;
using OSSShell.Interop;

namespace OSSShell.Taskbar
{
    public partial class StartMenu : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;

        private const string SHELL_REGISTRY_KEY = @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon";
        private const string PASSWORD_REGISTRY_KEY = @"SOFTWARE\OSSShell";
        private const string TASKMGR_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";

        private bool _preventAutoClose = false;

        // NumLock key constants
        private const int VK_NUMLOCK = 0x90;
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;

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
            EnsureNumLockEnabled();
        }

        private void PopulateMenu()
        {
            AddSimplyShipVersions();
            AddApplication("RustDesk", @"C:\Program Files\RustDesk\rustdesk.exe", Colors.DarkOrange);
            AddApplication("AnyDesk", @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe", Colors.DarkRed);
            AddScreenshotTool();
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

        private void AddScreenshotTool()
        {
            var button = new System.Windows.Controls.Button
            {
                Content = "Screenshot",
                Width = 150,
                Height = 150,
                Style = (Style)FindResource("TileButton"),
                Background = new SolidColorBrush(Colors.DarkGreen)
            };

            button.Click += ScreenshotButton_Click;
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
            if (!_preventAutoClose)
            {
                Dispatcher.BeginInvoke(new Action(() => Close()));
            }
        }

        private void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("C:\\Windows\\System32\\shutdown.exe", "/s /t 0");
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("C:\\Windows\\System32\\shutdown.exe", "/l");
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TakeScreenshot();
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to take screenshot: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TakeScreenshot()
        {
            // Get the virtual screen bounds (all monitors combined)
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            using (Bitmap bitmap = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(left, top, 0, 0, bitmap.Size);
                }

                // Save to desktop with timestamp
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                string filePath = Path.Combine(desktop, fileName);
                
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                
                // Show notification
                System.Windows.MessageBox.Show($"Screenshot saved to:\n{filePath}", "Screenshot Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RegeditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GetStoredPassword();
                
                if (string.IsNullOrEmpty(password))
                {
                    // First time setup - ask user to set password
                    if (SetupPassword())
                    {
                        System.Windows.MessageBox.Show("Password has been set. Click 'Registry Editor' again to access the registry editor.", 
                            "Password Set", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                // Ask for password
                _preventAutoClose = true;
                var passwordDialog = new PasswordDialog("Enter administrator password to access Registry Editor:");
                
                // Only set owner if this window is still open
                try
                {
                    if (this.IsLoaded && this.IsVisible)
                    {
                        passwordDialog.Owner = this;
                    }
                }
                catch
                {
                    // Window is closed, don't set owner
                }
                
                bool? result = passwordDialog.ShowDialog();
                _preventAutoClose = false;
                
                if (result != true || !passwordDialog.IsConfirmed)
                    return;

                string enteredPassword = passwordDialog.Password;
                
                if (enteredPassword == password)
                {
                    LaunchRegedit();
                    Close();
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect password!", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to access Registry Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskMgrButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GetStoredPassword();
                
                if (string.IsNullOrEmpty(password))
                {
                    // First time setup - ask user to set password
                    if (SetupPassword())
                    {
                        System.Windows.MessageBox.Show("Password has been set. Click 'Task Manager' again to toggle Task Manager access.", 
                            "Password Set", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                // Ask for password
                _preventAutoClose = true;
                var passwordDialog = new PasswordDialog("Enter administrator password to toggle Task Manager access:");
                
                // Only set owner if this window is still open
                try
                {
                    if (this.IsLoaded && this.IsVisible)
                    {
                        passwordDialog.Owner = this;
                    }
                }
                catch
                {
                    // Window is closed, don't set owner
                }
                
                bool? result = passwordDialog.ShowDialog();
                _preventAutoClose = false;
                
                if (result != true || !passwordDialog.IsConfirmed)
                    return;

                string enteredPassword = passwordDialog.Password;
                
                if (enteredPassword == password)
                {
                    ToggleTaskManager();
                    Close();
                }
                else
                {
                    System.Windows.MessageBox.Show("Incorrect password!", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to toggle Task Manager: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SetupPassword()
        {
            _preventAutoClose = true;
            
            var passwordDialog = new PasswordDialog("Set administrator password for shell management:");
            
            // Only set owner if this window is still open
            try
            {
                if (this.IsLoaded && this.IsVisible)
                {
                    passwordDialog.Owner = this;
                }
            }
            catch
            {
                // Window is closed, don't set owner
            }
            
            bool? result1 = passwordDialog.ShowDialog();
            if (result1 != true || !passwordDialog.IsConfirmed)
            {
                _preventAutoClose = false;
                return false;
            }

            string password = passwordDialog.Password;
            if (string.IsNullOrEmpty(password))
            {
                _preventAutoClose = false;
                return false;
            }

            var confirmDialog = new PasswordDialog("Confirm administrator password:");
            
            // Only set owner if this window is still open
            try
            {
                if (this.IsLoaded && this.IsVisible)
                {
                    confirmDialog.Owner = this;
                }
            }
            catch
            {
                // Window is closed, don't set owner
            }
            
            bool? result2 = confirmDialog.ShowDialog();
            _preventAutoClose = false;
            
            if (result2 != true || !confirmDialog.IsConfirmed)
                return false;

            string confirmPassword = confirmDialog.Password;

            if (password != confirmPassword)
            {
                System.Windows.MessageBox.Show("Passwords do not match!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            StorePassword(password);
            return true;
        }

        private string GetStoredPassword()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(PASSWORD_REGISTRY_KEY))
                {
                    return key?.GetValue("AdminPassword") as string ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private void StorePassword(string password)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(PASSWORD_REGISTRY_KEY))
                {
                    key.SetValue("AdminPassword", password);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to store password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchRegedit()
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    UseShellExecute = true,
                    Verb = "runas" // Request administrator privileges
                };

                System.Diagnostics.Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch Registry Editor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleTaskManager()
        {
            try
            {
                bool isCurrentlyDisabled = IsTaskManagerDisabled();
                
                // Ensure the full registry path exists
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies")?.Close();
                
                using (var key = Registry.CurrentUser.CreateSubKey(TASKMGR_REGISTRY_KEY, true))
                {
                    if (isCurrentlyDisabled)
                    {
                        // Enable Task Manager by removing the registry value
                        key.DeleteValue("DisableTaskMgr", false);
                        key.Flush();
                        System.Windows.MessageBox.Show(
                            "Task Manager has been enabled. You can now access Task Manager through Ctrl+Alt+Delete or other methods.",
                            "Task Manager Enabled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        // Disable Task Manager by setting the registry value to 1
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                        key.Flush();
                        System.Windows.MessageBox.Show(
                            "Task Manager has been disabled. Users will not be able to open Task Manager through normal means.",
                            "Task Manager Disabled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to toggle Task Manager: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsTaskManagerDisabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(TASKMGR_REGISTRY_KEY))
                {
                    var value = key?.GetValue("DisableTaskMgr");
                    return value != null && Convert.ToInt32(value) == 1;
                }
            }
            catch
            {
                return false; // If we can't read it, assume it's not disabled
            }
        }

        private void EnsureNumLockEnabled()
        {
            try
            {
                // Check if NumLock is currently enabled
                bool numLockState = (GetKeyState(VK_NUMLOCK) & 0x0001) != 0;
                
                if (!numLockState)
                {
                    // Simulate NumLock key press to enable it
                    keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                    keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set NumLock state: {ex.Message}");
            }
        }
    }
}