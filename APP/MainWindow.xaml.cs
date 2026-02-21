using System.Linq;
using System.Windows;

namespace MagicKeyboardMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 視窗一載入，就開始掃描設備
            LoadDevices();
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

                StatusText.Text = $"掃描完成！找到 {displayDevices.Count} 個實體 Apple 設備。";
            }
            else
            {
                StatusText.Text = "找不到任何 Apple 設備，請檢查藍牙或 USB 連線。";
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

                // 取得目前正在執行的 App 實例，並呼叫我們剛剛寫的啟動方法
                var app = (App)System.Windows.Application.Current;
                app.StartBatteryMonitor(targetPid);

                // 關閉設定視窗
                this.Close();
            }
        }
    }
}