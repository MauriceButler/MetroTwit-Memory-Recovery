namespace MetroTwitMemoryRecovery
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    using MetroTwitMemoryRecovery.Properties;

    /// <summary>
    ///   The form 1.
    /// </summary>
    public partial class Form1 : Form
    {
        #region Constants and Fields

        private const string ApplicationName = "MetroTwit Memory Recovery";

        private const string CurrentlyUsingXMb = "{0} is currently Using {1} mb";

        private const string Exit = "Exit";

        private const string GigabyteNotation = "gb";

        private const string HourNotation = "hour";

        private const string HoursNotation = "hours";

        private const string IconName = "TwitRam.ico";

        private const string MegabyteNotation = "mb";

        private const string MenuItemTypeName = "System.Windows.Forms.MenuItem";

        private const string MinuteNotation = "min";

        private const string MinutesNotation = "mins";

        private const string RecycleNow = "Recycle Now";

        private const string ApplicationToMonitor = "Application To Monitor";

        private const string RecycleWhenMemoryReaches = "Recycle when memory reaches";

        private const string SeconsNotation = "secs";

        private const string UsageCheckInterval = "Usage check interval";

        private ContextMenu contextMenu;

        private NotifyIcon notifyIcon;

        private Timer timer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="Form1" /> class.
        /// </summary>
        public Form1()
        {
            this.SetupContextMenu();

            this.SetupNotificationIcon();

            this.SetupTimer();
        }

        #endregion

        #region Methods

        /// <summary>
        ///   The dispose.
        /// </summary>
        /// <param name="disposing"> The disposing. </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (this.notifyIcon != null)
                {
                    this.notifyIcon.Dispose();
                }

                if (this.components != null)
                {
                    this.components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///   The exit click event handler.
        /// </summary>
        /// <param name="sender"> The sender. </param>
        /// <param name="e"> The e. </param>
        protected void ExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        ///   The recycle event handler.
        /// </summary>
        /// <param name="sender"> The sender. </param>
        /// <param name="e"> The e. </param>
        protected void Recycle(object sender, EventArgs e)
        {
            TryReycleMetroTwit(sender.GetType().FullName == MenuItemTypeName);
        }

        /// <summary>
        ///   The set checked event handler.
        /// </summary>
        /// <param name="sender"> The sender. </param>
        /// <param name="e"> The e. </param>
        protected void SetChecked(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null) { return; }

            foreach (MenuItem item in menuItem.Parent.MenuItems)
            {
                item.Checked = false;
            }

            menuItem.Checked = true;

            this.SaveSettings(menuItem);
        }

        /// <summary>
        ///   The set visible core.
        /// </summary>
        /// <param name="value"> true to make the control visible; otherwise, false. </param>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }  

        /// <summary>
        ///   The get memory usage.
        /// </summary>
        /// <param name="metroTwit"> The metro twit. </param>
        private static long GetMemoryUsage(Process metroTwit)
        {
            return new PerformanceCounter("Process", "Working Set - Private", metroTwit.ProcessName).RawValue / 1048576;
        }

        /// <summary>
        ///   The get metro twit process.
        /// </summary>
        private static Process GetMetroTwitProcess()
        {
            Process metroTwit = null;

            var metroTwitProcess = Process.GetProcessesByName(Settings.Default.ProcessName);
            if (metroTwitProcess.Length > 0)
            {
                metroTwit = metroTwitProcess[0];
            }

            return metroTwit;
        }

        /// <summary>
        ///   Reycles the metro twit.
        /// </summary>
        /// <param name="forceRecycle"> if set to <c>true</c> [force recycle]. </param>
        private static void TryReycleMetroTwit(bool forceRecycle)
        {
            var metroTwit = GetMetroTwitProcess();
            if (metroTwit == null) { return; }

            if (forceRecycle || GetMemoryUsage(metroTwit) > Settings.Default.MemoryLimit)
            {
                var metroPath = metroTwit.MainModule.FileName;

                metroTwit.CloseMainWindow();
                metroTwit.WaitForExit();

                Process.Start(metroPath);
            }
        }

        private static int GetLableAsInteger(MenuItem menuItem)
        {
            var lableParts = menuItem.Text.Split(' ');
            return int.Parse(lableParts[0]) * GetModifier(lableParts[1]);
        }

        private static int GetModifier(string notation)
        {
            int modifier;

            switch (notation)
            {
                case SeconsNotation:
                    modifier = 1000;
                    break;
                case MinuteNotation:
                case MinutesNotation:
                    modifier = 60000;
                    break;
                case HourNotation:
                case HoursNotation:
                    modifier = 3600000;
                    break;
                case GigabyteNotation:
                    modifier = 1024;
                    break;
                case MegabyteNotation:
                    modifier = 1;
                    break;
                default:
                    modifier = 1;
                    break;
            }

            return modifier;
        }

        private void SaveSettings(MenuItem menuItem)
        {
            var parent = menuItem.Parent as MenuItem;

            if (parent == null)
            {
                return;
            }

            switch (parent.Text)
            {
                case RecycleWhenMemoryReaches:
                    Settings.Default.MemoryLimit = GetLableAsInteger(menuItem);
                    break;
                case UsageCheckInterval:
                    Settings.Default.CheckInterval = GetLableAsInteger(menuItem);
                    this.timer.Interval = Settings.Default.CheckInterval;
                    break;
                case ApplicationToMonitor:
                    Settings.Default.ProcessName = menuItem.Text;
                    break;
            }

            Settings.Default.Save();
        }     

        /// <summary>
        ///   The setup context menu.
        /// </summary>
        private void SetupContextMenu()
        {
            this.contextMenu = new ContextMenu();

            this.contextMenu.MenuItems.Add(new MenuItem(string.Format(CurrentlyUsingXMb, Settings.Default.ProcessName, 0)));
            this.contextMenu.MenuItems.Add(this.CreateRecycleMenuItems());
            this.contextMenu.MenuItems.Add(this.CreateUsageCheckIntervalMenuItems());
            this.contextMenu.MenuItems.Add(new MenuItem(RecycleNow, this.Recycle));
            this.contextMenu.MenuItems.Add(this.CreateApplciationMenuItems());
            this.contextMenu.MenuItems.Add(new MenuItem(Exit, this.ExitClick));

            this.contextMenu.Popup += this.UpdateMemoryUsage;
        }

        private MenuItem CreateUsageCheckIntervalMenuItems()
        {
            var usageCheckIntervalMenuItem = new MenuItem(UsageCheckInterval);

            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("30 " + SeconsNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("1 " + MinuteNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("2 " + MinutesNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("5 " + MinutesNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("10 " + MinutesNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("15 " + MinutesNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("30 " + MinutesNotation, this.SetChecked));
            usageCheckIntervalMenuItem.MenuItems.Add(new MenuItem("1 " + HourNotation, this.SetChecked));

            foreach (
                var menuItem in
                    usageCheckIntervalMenuItem.MenuItems.Cast<MenuItem>().Where(menuItem => Settings.Default.CheckInterval == GetLableAsInteger(menuItem)))
            {
                menuItem.Checked = true;
            }

            return usageCheckIntervalMenuItem;
        }       

        private MenuItem CreateRecycleMenuItems()
        {
            var recycleMenuItem = new MenuItem(RecycleWhenMemoryReaches);

            recycleMenuItem.MenuItems.Add(new MenuItem("300 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("400 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("500 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("600 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("700 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("800 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("900 " + MegabyteNotation, this.SetChecked));
            recycleMenuItem.MenuItems.Add(new MenuItem("1 " + GigabyteNotation, this.SetChecked));

            foreach (var menuItem in recycleMenuItem.MenuItems.Cast<MenuItem>().Where(menuItem => Settings.Default.MemoryLimit == GetLableAsInteger(menuItem)))
            {
                menuItem.Checked = true;
            }

            return recycleMenuItem;
        }

        private MenuItem CreateApplciationMenuItems()
        {
            var applicationMenuItem = new MenuItem(ApplicationToMonitor);

            applicationMenuItem.MenuItems.Add(new MenuItem("MetroTwit", this.SetChecked));
            applicationMenuItem.MenuItems.Add(new MenuItem("MetroTwitLoop", this.SetChecked));

            foreach (var menuItem in applicationMenuItem.MenuItems.Cast<MenuItem>().Where(menuItem => Settings.Default.ProcessName == menuItem.Text))
            {
                menuItem.Checked = true;
            }

            return applicationMenuItem;
        }

        /// <summary>
        ///   The setup notification icon.
        /// </summary>
        private void SetupNotificationIcon()
        {
            this.notifyIcon = new NotifyIcon
                {
                    Text = ApplicationName, 
                    Visible = true, 
                    Icon = new Icon(this.GetType(), IconName), 
                    ContextMenu = this.contextMenu
                };
        }

        /// <summary>
        ///   The setup timer.
        /// </summary>
        private void SetupTimer()
        {
            this.timer = new Timer();

            this.timer.Tick += this.Recycle;
            this.timer.Interval = Settings.Default.CheckInterval;
            this.timer.Start();
        }

        /// <summary>
        ///   The update memory usage.
        /// </summary>
        /// <param name="sender"> The sender. </param>
        /// <param name="e"> The e. </param>
        private void UpdateMemoryUsage(object sender, EventArgs e)
        {
            var metroTwit = GetMetroTwitProcess();

            this.contextMenu.MenuItems[0].Text = string.Format(CurrentlyUsingXMb, Settings.Default.ProcessName, metroTwit != null ? GetMemoryUsage(metroTwit) : 0);
        }

        #endregion
    }
}