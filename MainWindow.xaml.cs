using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PlooshLauncher
{
    public partial class MainWindow : FluentWindow
    {
        private const string backendUrl = "http://26.159.168.72:3551/";

        public MainWindow()
        {
            ApplicationThemeManager.ApplySystemTheme(true);
            InitializeComponent();

            pathText.Content = Settings.Default.path;
            if (pathText.Content as string != "") pathPlaceholder.Visibility = Visibility.Hidden;

            username.Text = Settings.Default.username;
            password.Password = Settings.Default.password;

            titleBar.Background = ApplicationAccentColorManager.SystemAccentBrush;

            bool isWindows10 = Environment.OSVersion.Version.Build < 22000;
            if (isWindows10)
            {
                WindowBackdropType = WindowBackdropType.None;
                Background = ApplicationAccentColorManager.SystemAccentBrush;
            }
        }

        private void setUsername(object sender, RoutedEventArgs e)
        {
            Settings.Default.username = username.Text;
            Settings.Default.Save();
        }

        private void setPassword(object sender, RoutedEventArgs e)
        {
            Settings.Default.password = password.Password;
            Settings.Default.Save();
        }

        private void selectFolder(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            if (openFolderDialog.ShowDialog() == true)
            {
                pathText.Content = openFolderDialog.FolderName;
                if (pathText.Content as string != "") pathPlaceholder.Visibility = Visibility.Hidden;
                Settings.Default.path = pathText.Content.ToString()!;
                Settings.Default.Save();
            }
        }

        private async void launchGame(object sender, RoutedEventArgs e)
        {
            string gamePath = pathText.Content.ToString();
            if (string.IsNullOrEmpty(gamePath)) return;

            string exePath = Path.Join(gamePath, "FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe");
            if (File.Exists(exePath))
            {
                launch.Content = "Launching...";
                launch.IsEnabled = false;

                Process.GetProcessesByName("FortniteClient-Win64-Shipping").ToList().ForEach(proc =>
                {
                    try { proc.Kill(); } catch { }
                });

                try
                {
                    ResourceManager rm = new ResourceManager("PlooshLauncher.g", Assembly.GetExecutingAssembly());
                    Stream? manifestResourceStream = rm.GetStream("starfall.dll");
                    string tempFileName = Path.GetTempFileName();
                    FileStream fileStream = File.OpenWrite(tempFileName);
                    manifestResourceStream?.Seek(0L, SeekOrigin.Begin);
                    manifestResourceStream?.CopyTo(fileStream);
                    fileStream.Close();
                    File.Copy(tempFileName, Path.Join(gamePath, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64", "GFSDK_Aftermath_Lib.x64.dll"), true);
                    File.Copy(tempFileName, Path.Join(gamePath, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64", "GFSDK_Aftermath_Lib.dll"), true);
                    File.Delete(tempFileName);
                }
                catch { return; }

                string args = $"-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -nobe -fromfl=eac -fltoken=h1cdhchd10150221h130eB56 -skippatchcheck -caldera=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2NvdW50X2lkIjoiMTM5ZDAzOGFmOTM2NDcyODgxMTdlYWU3MWYxZGQ5ZTQiLCJnZW5lcmF0ZWQiOjE3MDQ0MTE5MDQsImNhbGRlcmFHdWlkIjoiODhjZmQ5NzYtM2U2OS00MWYzLWI2ODEtYzQyOTcxM2ZkMWFlIiwiYWNQcm92aWRlciI6IkVhc3lBbnRpQ2hlYXQiLCJub3RlcyI6IiIsImZhbGxiYWNrIjpmYWxzZX0.Q8hdxvrW2sH-3on6JEBLANB0rkPAGUwbZYPrCOMTtvA " +
                    $"-AUTH_LOGIN={username.Text} -AUTH_PASSWORD={password.Password} -AUTH_TYPE=epic -backend={backendUrl}";

                Process? FN = Proc.Start(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe", args);
                Process? AC = Proc.Start(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping_EAC.exe", "-skippatchcheck", suspend: true);
                Process? LC = Proc.Start(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteLauncher.exe", "-skippatchcheck", suspend: true);

                launch.Content = "Loading...";
                await Task.Run(() => FN!.WaitForInputIdle());
                launch.Content = "Running";
                await FN!.WaitForExitAsync();

                try { AC?.Kill(); LC?.Kill(); } catch { }

                launch.Content = "Launch FN";
                launch.IsEnabled = true;
            }
        }
    }
}

