using System.Linq;
using System.Windows;
using Microsoft.Win32; // 用來操作 Windows 登錄檔

namespace MagicKeyboardMonitor
{

    public partial class MainWindow : Window
    {
        // 定義註冊表路徑與我們程式的名稱
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WinMagicBattery";
        public MainWindow()
        {
            InitializeComponent();

            // 視窗一載入，就開始掃描設備
            LoadDevices();

            // 視窗載入時，檢查目前是否已經設定為開機啟動
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
            {
                if (key != null)
                {
                    object? value = key.GetValue(AppName);
                    // 如果登錄檔裡面的路徑跟我們現在執行的路徑一樣，就把 CheckBox 打勾
                    if (value != null && value.ToString() == Environment.ProcessPath)
                    {
                        AutoStartCheckBox.IsChecked = true;
                    }
                }
            }

        }

        private void LoadDevices()
        {
            var scanner = new DeviceScanner();
            var rawDevices = scanner.ScanForAppleDevices();

            // 關鍵：將相同 PID 的裝置分組，只取第一個。這樣畫面上就不會出現重複的鍵盤！
            var displayDevices = rawDevices
                .GroupBy(d => d.ProductId)
                .Select(g => g.First())
                .ToList();

            if (displayDevices.Count > 0)
            {
                // 將過濾後的設備綁定到下拉選單
                DeviceComboBox.ItemsSource = displayDevices;
                // 設定下拉選單要顯示模型中的哪一個字串
                DeviceComboBox.DisplayMemberPath = "DisplayName";
                // 預設選擇第一個
                DeviceComboBox.SelectedIndex = 0;

                StatusText.Text = $"Scan complete! Found {displayDevices.Count} Apple device(s).";
            }
            else
            {
                StatusText.Text = "No Apple devices found. Check Bluetooth/USB connection.";
                DeviceComboBox.IsEnabled = false;
                SaveButton.IsEnabled = false;
            }
        }

        // 點擊儲存按鈕的事件
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem is AppleDeviceModel selectedDevice)
            {
                int targetPid = selectedDevice.ProductId;

                // 1. 將 PID 存入 Windows 設定檔
                APP.Properties.Settings.Default.TargetPid = targetPid;
                APP.Properties.Settings.Default.Save();

                // 處理開機自動啟動的邏輯
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    if (key != null)
                    {
                        if (AutoStartCheckBox.IsChecked == true)
                        {
                            // 打勾：把目前程式的絕對路徑寫入登錄檔
                            key.SetValue(AppName, Environment.ProcessPath!);
                        }
                        else
                        {
                            // 沒打勾：從登錄檔中刪除，取消開機啟動
                            key.DeleteValue(AppName, false);
                        }
                    }
                }

                // 3. 啟動背景監控
                var app = (App)System.Windows.Application.Current;
                app.StartBatteryMonitor(targetPid);
                this.Close();
            }
        }
    }
}