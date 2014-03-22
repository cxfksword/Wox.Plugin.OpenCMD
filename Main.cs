using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Web;


namespace Wox.Plugin.OpenCMD
{
    public class Main : IPlugin
    {
        private PluginInitContext context;
        private static List<SystemWindow> openingWindows = new List<SystemWindow>();

        public List<Result> Query(Query query)
        {
            var list = new List<Result>();

            SystemWindow win = null;
            GetOpeningWindows();
            if (openingWindows.Count > 1)
            {
                // the first window is Wox, skip it!
                if (openingWindows[1].Process.ProcessName == "explorer")
                {
                    win = openingWindows[1];
                }
            }

            if (win != null)
            {
                foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindowsClass())
                {
                    var filename = Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                    if (filename.ToLowerInvariant() == "explorer")
                    {
                        if (!window.LocationURL.ToLower().Contains("file:"))
                            continue;

                        // immediately open the windows command
                        if (win.HWnd == (IntPtr)window.HWND)
                        {
                            var path = window.LocationURL.Replace("file:///", "");
                            StartShell(path);
                            this.context.HideApp();
                            return list;
                        }
                    }

                }
            }

            if (list.Count <= 0)
            {
                // list all opening folder
                foreach (SHDocVw.InternetExplorer window in new SHDocVw.ShellWindowsClass())
                {
                    var filename = Path.GetFileNameWithoutExtension(window.FullName).ToLower();
                    if (filename.ToLowerInvariant() == "explorer")
                    {
                        if (!window.LocationURL.ToLower().Contains("file:"))
                            continue;

                        var path = window.LocationURL.Replace("file:///", "");
                        path = HttpUtility.UrlDecode(path);
                        if (!Directory.Exists(path))
                            continue;

                        list.Add(new Result()
                        {
                            IcoPath = "Images\\app.png",
                            Title = path,
                            SubTitle = "Open cmd in this path",
                            Action = (c) =>
                            {
                                StartShell(path);
                                return true;
                            }
                        });

                    }
                }
            }

            return list;
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
        }

        private static void StartShell(string path)
        {
            path = HttpUtility.UrlDecode(path);
            var cmder = Environment.GetEnvironmentVariable("CMDER_ROOT");
            if (!string.IsNullOrEmpty(cmder))
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine(cmder, "cmder.exe"),
                    Arguments = "/START \"" + path + "\""
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd",
                    WorkingDirectory = path
                });
            }
        }

        private static void GetOpeningWindows()
        {
            openingWindows = new List<SystemWindow>();
            WinApi.EnumWindowsProc callback = EnumWindows;
            WinApi.EnumWindows(callback, 0);
        }

        private static bool EnumWindows(IntPtr hWnd, int lParam)
        {
            if (!WinApi.IsWindowVisible(hWnd))
                return true;

            var title = new StringBuilder(256);
            WinApi.GetWindowText(hWnd, title, 256);

            if (string.IsNullOrEmpty(title.ToString()))
            {
                return true;
            }

            if (title.Length != 0 || (title.Length == 0 & hWnd != WinApi.statusbar))
            {
                var window = new SystemWindow(hWnd);
                if (window.IsAltTabWindow())
                {
                    openingWindows.Add(window);
                }
            }

            return true;
        }


    }
}
