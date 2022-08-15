﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shell32;

namespace ToggleDesktopIcons
{
    static class Program
    {
        //https://stackoverflow.com/questions/17503289/how-to-refresh-reload-desktop

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        private const int WM_COMMAND = 0x111;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetShellWindow();

        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0)
            {
                var builder = new StringBuilder(size);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        public static IEnumerable<IntPtr> FindWindowsWithClass(string className)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                StringBuilder cl = new StringBuilder(256);
                GetClassName(wnd, cl, cl.Capacity);
                if (cl.ToString() == className && (GetWindowText(wnd) == "" || GetWindowText(wnd) == null))
                {
                    windows.Add(wnd);
                }
                return true;
            },
                        IntPtr.Zero);

            return windows;
        }

        static void ToggleDesktopIcons()
        {
            var toggleDesktopCommand = new IntPtr(0x7402);
            IntPtr hWnd = IntPtr.Zero;
            if (Environment.OSVersion.Version.Major < 6 || Environment.OSVersion.Version.Minor < 2) //7 and -
                hWnd = GetWindow(FindWindow("Progman", "Program Manager"), GetWindow_Cmd.GW_CHILD);
            else
            {
                IEnumerable<IntPtr> ptrs = FindWindowsWithClass("WorkerW");
                int i = 0;
                while (hWnd == IntPtr.Zero && i < ptrs.Count())
                {
                    hWnd = FindWindowEx(ptrs.ElementAt(i), IntPtr.Zero, "SHELLDLL_DefView", null);
                    i++;
                }
            }
            if (hWnd == IntPtr.Zero)
            {
                //"SHELLDLL_DefView" was not found as a child within WorkerW - Lets check the current ShellWindow
                IntPtr desktop = GetShellWindow();
                hWnd = FindWindowEx(desktop, IntPtr.Zero, "SHELLDLL_DefView", null);
            }
            if (hWnd != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_COMMAND, toggleDesktopCommand, IntPtr.Zero);
            }

        }

        [STAThread]
  
       //Added by neilgrz. Runs program to toggle icons and minimize windows 
        static void Main(string[] args)
        {
            ToggleDesktopIcons();
            Shell32.ShellClass shell = new Shell32.ShellClass();
            shell.MinimizeAll();
        }
    }
}
