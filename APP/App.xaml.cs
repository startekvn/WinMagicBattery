using System;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Threading.Tasks;

namespace MagicKeyboardMonitor
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private BatteryMonitor? _batteryMonitor;
        private DashboardWindow? _dashboardWindow; // 宣告儀表板視窗
        private int _currentBatteryLevel = -1;     // 記住當前電量

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            _notifyIcon = new NotifyIcon();

            // ==========================================
            // 👉 未來你有自己的 icon.ico 時，改成這樣寫：
            // _notifyIcon.Icon = new System.Drawing.Icon("你的圖示路徑.ico");
            // 現在先暫時用系統內建的安全圖示代替
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            // ==========================================

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Magic Keyboard Monitor (Waiting...)";

            // 綁定右鍵選單
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, OnSettingsClicked);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, OnExitClicked);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 綁定滑鼠點擊事件 (用來捕捉左鍵)
            _notifyIcon.MouseClick += OnTrayIconClicked;

            // 自動啟動邏輯
            int savedPid = APP.Properties.Settings.Default.TargetPid;
            if (savedPid != 0)
            {
                StartBatteryMonitor(savedPid);
            }
        }

        // 捕捉點擊事件
        private void OnTrayIconClicked(object? sender, MouseEventArgs e)
        {
            // 如果使用者點擊的是滑鼠左鍵
            if (e.Button == MouseButtons.Left)
            {
                ShowDashboard();
            }
        }

        // 顯示儀表板
        private void ShowDashboard()
        {
            if (_dashboardWindow == null)
            {
                _dashboardWindow = new DashboardWindow();
            }

            // 更新最新電量並顯示
            _dashboardWindow.UpdateBattery(_currentBatteryLevel);
            _dashboardWindow.Show();
            _dashboardWindow.Activate(); // 讓視窗取得焦點，這樣點其他地方才能觸發 Deactivated 自動隱藏
        }

        public void StartBatteryMonitor(int targetPid)
        {
            _batteryMonitor?.StopMonitoring();
            _batteryMonitor = new BatteryMonitor();
            _batteryMonitor.OnBatteryUpdated += UpdateTrayText;
            _batteryMonitor.OnDeviceLost += ShowDeviceLost;

            Task.Run(() => _batteryMonitor.StartMonitoringAsync(targetPid));
        }

        // 更新電量邏輯 (只更新文字與儀表板，不再畫圖)
        private void UpdateTrayText(int batteryLevel)
        {
            _currentBatteryLevel = batteryLevel; // 記住電量
            Dispatcher.Invoke(() =>
            {
                if (_notifyIcon != null) _notifyIcon.Text = $"Magic Keyboard: {batteryLevel}%";

                // 如果儀表板正在畫面上，即時更新裡面的數字
                if (_dashboardWindow != null && _dashboardWindow.IsVisible)
                {
                    _dashboardWindow.UpdateBattery(batteryLevel);
                }
            });
        }

        private void ShowDeviceLost()
        {
            _currentBatteryLevel = -1;
            Dispatcher.Invoke(() =>
            {
                if (_notifyIcon != null) _notifyIcon.Text = "Magic Keyboard: Disconnected";
                if (_dashboardWindow != null && _dashboardWindow.IsVisible)
                {
                    _dashboardWindow.UpdateBattery(-1);
                }
            });
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            new MainWindow().Show();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _batteryMonitor?.StopMonitoring();
            _notifyIcon?.Dispose();
            Current.Shutdown();
        }
    }
}