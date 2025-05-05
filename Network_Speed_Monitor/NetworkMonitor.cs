using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Network_Speed_Monitor
{
    public class NetworkMonitor
    {
        private PerformanceCounter bytesReceived;
        private PerformanceCounter bytesSent;
        private string interfaceName;
        private bool isValid;

        public NetworkMonitor(string interfaceName)
        {
            this.interfaceName = interfaceName;
            InitializeCounters();
        }

        // Add public InterfaceName property
        public string InterfaceName
        {
            get => interfaceName;
        }

        private void InitializeCounters()
        {
            try
            {
                // Validate interface existence
                if (!IsInterfaceValid(interfaceName))
                {
                    isValid = false;
                    return;
                }

                bytesReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", interfaceName);
                bytesSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", interfaceName);
                isValid = true;
            }
            catch
            {
                isValid = false;
            }
        }

        public (float downloadSpeed, float uploadSpeed) GetSpeeds()
        {
            if (!isValid)
            {
                return (0, 0);
            }

            try
            {
                float download = bytesReceived.NextValue() / 1024; // Convert to KB/s
                float upload = bytesSent.NextValue() / 1024; // Convert to KB/s
                return (download, upload);
            }
            catch (InvalidOperationException)
            {
                // Interface became invalid, mark as invalid and try to reinitialize
                isValid = false;
                InitializeCounters();
                return (0, 0);
            }
            catch
            {
                return (0, 0); // Other errors, return safe values
            }
        }

        public bool IsValidInterface => isValid;

        public void ChangeInterface(string newInterfaceName)
        {
            interfaceName = newInterfaceName;
            InitializeCounters();
        }

        private bool IsInterfaceValid(string interfaceName)
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                return category.GetInstanceNames().Contains(interfaceName);
            }
            catch
            {
                return false;
            }
        }

        public static string[] GetNetworkInterfaces()
        {
            var dd = NetworkInterface.GetAllNetworkInterfaces();
    
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up) // Only active interfaces
                .Select(nic => nic.Description)
                .ToArray();
            
        }

        public static string GetDefaultInterface()
        {
            var activeInterfaces = GetNetworkInterfaces();
            return activeInterfaces.FirstOrDefault() ?? "Unknown";
        }
    }
}

