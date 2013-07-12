using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Spoti15
{
    class Program
    {
        private static NotifyIcon notico;
        private static MenuItem autostartItem;

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] != "-autostart")
            {
                Properties.Settings.Default.HideIcon = false;
                Properties.Settings.Default.Save();
            }

            ContextMenu cm = new ContextMenu();
            MenuItem menu;

            menu = new MenuItem();
            menu.Text = "&Hide tray icon";
            menu.Click += HideClick;
            cm.MenuItems.Add(menu);

            menu = new MenuItem();
            menu.Checked = Autostart.IsEnabled();
            menu.Text = "&Autostart";
            menu.Click += AutostartClick;
            cm.MenuItems.Add(menu);
            autostartItem = menu;

            cm.MenuItems.Add("-");

            menu = new MenuItem();
            menu.Text = "E&xit";
            menu.Click += ExitClick;
            cm.MenuItems.Add(menu);

            notico = new NotifyIcon();
            notico.Text = "Spoti15";
            notico.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notico.ContextMenu = cm;
            notico.Visible = !Properties.Settings.Default.HideIcon;

            Spoti15 spoti15 = new Spoti15();

            Application.Run();

            GC.KeepAlive(spoti15);
        }

        private static void AutostartClick(Object sender, EventArgs e)
        {
            if (Autostart.IsEnabled())
                Autostart.Disable();
            else
                Autostart.Enable();

            autostartItem.Checked = !autostartItem.Checked;
        }

        private static void HideClick(Object sender, EventArgs e)
        {
            Properties.Settings.Default.HideIcon = true;
            Properties.Settings.Default.Save();

            notico.Visible = false;
        }

        private static void ExitClick(Object sender, EventArgs e)
        {
            notico.Dispose();
            Application.Exit();
        }
    }
}
