using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Timers;

namespace Network_Speed_Monitor
{
    class Program
    {
        //class Program
        //{
        //    private static NotifyIcon trayIcon;
        //    private static NetworkMonitor monitor;
        //    private static System.Timers.Timer timer;

        //    [STAThread]
        //    static void Main()
        //    {
        //        Application.EnableVisualStyles();
        //        Application.SetCompatibleTextRenderingDefault(false);

        //        // Initialize system tray icon
        //        trayIcon = new NotifyIcon
        //        {
        //            Icon = new Icon("internet_speed_meter_icon.ico"),
        //            Visible = true,
        //            Text = "Internet Speed Meter"
        //        };

        //        // Initialize network monitor (use first interface for now)
        //        var interfaces = NetworkMonitor.GetNetworkInterfaces();
        //        monitor = new NetworkMonitor(interfaces.FirstOrDefault() ?? "Unknown");

        //        // Set up timer to update speeds every second
        //        timer = new System.Timers.Timer(1000);
        //        timer.Elapsed += UpdateSpeeds;
        //        timer.AutoReset = true;
        //        timer.Start();

        //        // Set up context menu
        //        trayIcon.ContextMenuStrip = CreateContextMenu();

        //        // Run the application
        //        Application.Run();
        //    }

        //    private static void UpdateSpeeds(object sender, ElapsedEventArgs e)
        //    {
        //        var (download, upload) = monitor.GetSpeeds();
        //        trayIcon.Text = $"↓ {download:F1} KB/s ↑ {upload:F1} KB/s";
        //    }

        //    private static ContextMenuStrip CreateContextMenu()
        //    {
        //        var menu = new ContextMenuStrip();

        //        // Add interface selection menu
        //        var interfacesMenu = new ToolStripMenuItem("Select Interface");
        //        foreach (var nic in NetworkMonitor.GetNetworkInterfaces())
        //        {
        //            interfacesMenu.DropDownItems.Add(nic, null, (s, e) =>
        //            {
        //                monitor = new NetworkMonitor(nic);
        //            });
        //        }

        //        menu.Items.Add(interfacesMenu);
        //        menu.Items.Add("Exit", null, (s, e) =>
        //        {
        //            trayIcon.Visible = false;
        //            Application.Exit();
        //        });

        //        return menu;
        //    }
        //}
        private static NotifyIcon trayIcon;
        private static NetworkMonitor monitor;
        private static System.Timers.Timer timer;
        private static SpeedDisplayForm speedForm;
        private static bool isDraggingEnabled;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize system tray icon
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Replace with your custom icon
                Visible = true,
                Text = "Internet Speed Meter"
            };

            // Initialize network monitor with default active interface
            monitor = new NetworkMonitor(NetworkMonitor.GetDefaultInterface());

            // Initialize speed display form
            speedForm = new SpeedDisplayForm();
            speedForm.Show();

            // Set up timer
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += UpdateSpeeds;
            timer.AutoReset = true;
            timer.Start();

            // Set up context menu
            trayIcon.ContextMenuStrip = CreateContextMenu();

            // Handle network changes
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += (s, e) =>
            {
                trayIcon.ContextMenuStrip = CreateContextMenu(); // Refresh menu
                if (!monitor.IsValidInterface)
                {
                    monitor.ChangeInterface(NetworkMonitor.GetDefaultInterface());
                    trayIcon.ShowBalloonTip(3000, "Network Change", "Selected new interface: " + monitor.InterfaceName, ToolTipIcon.Info);
                }
            };

            Application.Run();
        }

        private static void UpdateSpeeds(object sender, ElapsedEventArgs e)
        {
            var (download, upload) = monitor.GetSpeeds();
            if (monitor.IsValidInterface)
            {
                speedForm.UpdateSpeeds(upload, download);
                trayIcon.Text = $"↑: {upload:F2} KB/s ↓: {download:F2} KB/s"; // Tooltip fallback
            }
            else
            {
                speedForm.UpdateSpeeds(0, 0);
                trayIcon.Text = "No network";
            }
        }

        private static ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();
            var interfacesMenu = new ToolStripMenuItem("Select Interface");
            foreach (var nic in NetworkMonitor.GetNetworkInterfaces())
            {
                interfacesMenu.DropDownItems.Add(nic, null, (s, e) =>
                {
                    monitor.ChangeInterface(nic);
                    trayIcon.ShowBalloonTip(3000, "Interface Changed", $"Monitoring: {nic}", ToolTipIcon.Info);
                });
            }

            menu.Items.Add(interfacesMenu);
            menu.Items.Add("Reset Position", null, (s, e) =>
            {
                speedForm.ResetPosition();
            });
            menu.Items.Add("Toggle Dragging", null, (s, e) =>
            {
                isDraggingEnabled = !isDraggingEnabled;
                ((ToolStripMenuItem)s).Text = isDraggingEnabled ? "Disable Dragging" : "Enable Dragging";
            });
            menu.Items.Add("Exit", null, (s, e) =>
            {
                trayIcon.Visible = false;
                speedForm.Close();
                Application.Exit();
            });

            return menu;
        }
    }
}