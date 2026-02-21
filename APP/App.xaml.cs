using System;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace MagicKeyboardMonitor
{
    public partial class App : Application
    {
        private NotifyIcon _notifyIcon;
        private BatteryMonitor _batteryMonitor; // 宣告我們的監控器

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Information;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Magic Keyboard Monitor (Waiting...)";

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, OnSettingsClicked);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, OnExitClicked);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        // 這是開放給 MainWindow 呼叫的方法！
        public void StartBatteryMonitor(int targetPid)
        {
            // 如果已經有在監控的，先停止它
            if (_batteryMonitor != null)
            {
                _batteryMonitor.StopMonitoring();
            }

            _batteryMonitor = new BatteryMonitor();

            // 綁定事件：當監控器讀到電量時，呼叫 UpdateTrayText
            _batteryMonitor.OnBatteryUpdated += UpdateTrayText;
            _batteryMonitor.OnDeviceLost += ShowDeviceLost;

            // 啟動背景監控 (使用 _ = 是告訴編譯器我們不需要等待它結束，讓它自己在背景跑)
            _ = _batteryMonitor.StartMonitoringAsync(targetPid);
        }

        private void UpdateTrayText(int batteryLevel)
        {
            // 因為這是從背景執行緒傳來的，必須切換回 UI 執行緒才能更新畫面
            Dispatcher.Invoke(() =>
            {
                _notifyIcon.Text = $"Magic Keyboard: {batteryLevel}%";
                // [進階任務] 未來你可以依據 batteryLevel 的數值，動態替換 _notifyIcon.Icon 的圖片！
            });
        }

        private void ShowDeviceLost()
        {
            Dispatcher.Invoke(() =>
            {
                _notifyIcon.Text = "Magic Keyboard: Disconnected / Sleeping";
            });
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            var settingsWindow = new MainWindow();
            settingsWindow.Show();
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            _batteryMonitor?.StopMonitoring();
            _notifyIcon.Dispose();
            Current.Shutdown();
        }
    }
}