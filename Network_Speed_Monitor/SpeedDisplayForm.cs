using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Network_Speed_Monitor
{

    public partial class SpeedDisplayForm : Form
    {
        private Label uploadLabel;
        private Label downloadLabel;
        private System.Windows.Forms.Timer positionTimer;
        private bool isDragging = false;
        private Point dragStartPosition; 
        private bool hasCustomPosition = false; // New flag to track custom position
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        public SpeedDisplayForm()
        {
            // Form settings  
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            TopMost = true;
            ShowInTaskbar = false;
            Size = new Size(120, 40);

            // Upload label (↑)
            uploadLabel = new Label
            {
                Text = "↑: 0.00 KB/s",
                ForeColor = Color.White,
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 8)
            };

            // Download label (↓)
            downloadLabel = new Label
            {
                Text = "↓: 0.00 KB/s",
                ForeColor = Color.White,
                Location = new Point(0, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 8)
            };

            // Add labels to form
            Controls.Add(uploadLabel);
            Controls.Add(downloadLabel);

            // Position form on taskbar
            bool positionLoaded = false;
            if (File.Exists("position.json"))
            {
                try
                {
                    string json = File.ReadAllText("position.json");
                    // Log the contents for debugging
                    Console.WriteLine($"position.json contents: {json}");

                    var pos = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                    if (pos != null && pos.X != null && pos.Y != null)
                    {
                        Location = new Point((int)pos.X, (int)pos.Y);
                        positionLoaded = true;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error for debugging
                    Console.WriteLine($"Failed to load position.json: {ex.Message}");
                }
            }

            if (!positionLoaded)
            {
                PositionFormOnTaskbar();
            }

            // Set up timer to reposition periodically
            positionTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            positionTimer.Tick += (s, e) =>
            {
                if (!isDragging)
                {
                    if (!hasCustomPosition)
                    {
                        PositionFormOnTaskbar();
                    }
                    // Ensure the form stays on top
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                }
            };
            positionTimer.Start();

            // Enable dragging
            MouseDown += SpeedDisplayForm_MouseDown;
            MouseMove += SpeedDisplayForm_MouseMove;
            MouseUp += SpeedDisplayForm_MouseUp;

            // Allow labels to trigger dragging
            uploadLabel.MouseDown += SpeedDisplayForm_MouseDown;
            uploadLabel.MouseMove += SpeedDisplayForm_MouseMove;
            uploadLabel.MouseUp += SpeedDisplayForm_MouseUp;

            downloadLabel.MouseDown += SpeedDisplayForm_MouseDown;
            downloadLabel.MouseMove += SpeedDisplayForm_MouseMove;
            downloadLabel.MouseUp += SpeedDisplayForm_MouseUp;
            Load += (s, e) =>
            {
                int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            };
        }
        private void SpeedDisplayForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPosition = new Point(e.X, e.Y);
                positionTimer.Stop();

                // Visual feedback: Change label color instead of background
                uploadLabel.ForeColor = Color.Yellow;
                downloadLabel.ForeColor = Color.Yellow;

                // Alternative: Use a solid background color
                // BackColor = Color.LightGray;
                // TransparencyKey = Color.Empty;
            }
        }

        private void SpeedDisplayForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false; 
                hasCustomPosition = true;
                positionTimer.Start();

                // Restore label color
                uploadLabel.ForeColor = Color.White;
                downloadLabel.ForeColor = Color.White;

                try
                {
                    File.WriteAllText("position.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { X = Location.X, Y = Location.Y }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save position.json: {ex.Message}");
                }
                // Alternative: Restore transparency
                // BackColor = Color.Black;
                // TransparencyKey = Color.Black;
            }
        }
        //private void SpeedDisplayForm_MouseDown(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        isDragging = true;
        //        dragStartPosition = new Point(e.X, e.Y);
        //        positionTimer.Stop(); // Stop auto-repositioning while dragging  
        //    }
        //}

        private void SpeedDisplayForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newPoint = PointToScreen(new Point(e.X, e.Y));
                newPoint.X -= dragStartPosition.X;
                newPoint.Y -= dragStartPosition.Y;
                Location = newPoint;
                File.WriteAllText("position.json", JsonConvert.SerializeObject(new { X = Location.X, Y = Location.Y }));
            }
        }

        //private void SpeedDisplayForm_MouseUp(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        isDragging = false;
        //        positionTimer.Start(); // Resume auto-repositioning  
        //        File.WriteAllText("position.json", JsonConvert.SerializeObject(new { X = Location.X, Y = Location.Y })); // Simplified member name (IDE0037)  
        //    }
        //}

        public void UpdateSpeeds(float uploadSpeed, float downloadSpeed)
        {
            // Check if we need to invoke on the UI thread  
            if (uploadLabel.InvokeRequired || downloadLabel.InvokeRequired)
            {
                // Marshal the call to the UI thread  
                Invoke(new Action<float, float>(UpdateSpeeds), uploadSpeed, downloadSpeed);
            }
            else
            {
                // Safe to update UI directly  
                uploadLabel.Text = $"↑: {uploadSpeed:F2} KB/s";
                downloadLabel.Text = $"↓: {downloadSpeed:F2} KB/s";
            }
        }

        public void ResetPosition()
        {
            hasCustomPosition = false; // Reset custom position flag
            PositionFormOnTaskbar();
            try
            {
                if (File.Exists("position.json"))
                {
                    File.Delete("position.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete position.json: {ex.Message}");
            }
        }

        private void PositionFormOnTaskbar()
        {
            // Get screen bounds  
            Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
            int taskbarHeight = screenBounds.Height - Screen.PrimaryScreen.Bounds.Height;

            // Position form above the taskbar, near the system tray  
            Location = new Point(screenBounds.Width - Width - 10, screenBounds.Height - Height - 5);

            // Adjust for taskbar position (bottom, top, left, right)  
            if (taskbarHeight > 0) // Taskbar at bottom  
            {
                Location = new Point(screenBounds.Width - Width - 10, screenBounds.Height - Height - 5);
            }
            else if (screenBounds.Width != Screen.PrimaryScreen.Bounds.Width) // Taskbar on left or right  
            {
                Location = new Point(screenBounds.Width - Width - 10, screenBounds.Height - Height - 5);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW: Don’t show in Alt+Tab  
                return cp;
            }
        }
    }
}


