using System.Collections.Generic;
using System.Linq;
using HidLibrary;

namespace MagicKeyboardMonitor
{
    public class DeviceScanner
    {
        // Apple 設備的專屬 Vendor ID
        private const int AppleVendorId = 0x004C;

        // 建立 PID 與設備名稱的映射表 
        private readonly Dictionary<int, string> _knownDevices = new Dictionary<int, string>
        {
            { 0x0267, "Magic Keyboard" },
            { 0x026C, "Magic Keyboard with Numeric Keypad" },
            { 0x029C, "Magic Keyboard (Mac Studio Edition)" }
        };

        public List<AppleDeviceModel> ScanForAppleDevices()
        {
            var resultList = new List<AppleDeviceModel>();

            // 呼叫 HidLibrary 取得所有 VID 為 0x004C 的設備
            var hidDevices = HidDevices.Enumerate(AppleVendorId);

            foreach (var device in hidDevices)
            {
                int pid = device.Attributes.ProductId;

                // 檢查是否為我們已知的鍵盤型號，若不是則顯示其 PID
                string deviceName = _knownDevices.ContainsKey(pid)
                    ? _knownDevices[pid]
                    : $"Unknown Apple Device (PID: 0x{pid:X4})";

                resultList.Add(new AppleDeviceModel
                {
                    DisplayName = deviceName,
                    ProductId = pid,
                    DevicePath = device.DevicePath
                });
            }

            // 移除重複的設備
            // 藍牙設備常常會註冊多個相同 PID 的虛擬介面，我們只需保留不重複的 DevicePath
            var uniqueDevices = resultList
                .GroupBy(d => d.DevicePath)
                .Select(g => g.First())
                .ToList();

            return uniqueDevices;
        }
    }
}