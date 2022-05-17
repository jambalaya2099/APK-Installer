﻿using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using NativeMethods.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace APKInstaller.Helpers
{
    // Helper class to allow the app to find the Window that contains an
    // arbitrary UIElement (GetWindowForElement).  To do this, we keep track
    // of all active Windows.  The app code must call WindowHelper.CreateWindow
    // rather than "new Window" so we can keep track of all the relevant
    // windows.  In the future, we would like to support this in platform APIs.
    public static class WindowHelper
    {
        public static Window CreateWindow()
        {
            Window newWindow = new();
            TrackWindow(newWindow);
            return newWindow;
        }

        public static void TrackWindow(this Window window)
        {
            window.Closed += (sender, args) =>
            {
                ActiveWindows.Remove(window);
            };
            ActiveWindows.Add(window);
        }

        public static Window GetWindowForElement(this UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in ActiveWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null;
        }

        public static AppWindow GetAppWindowForCurrentWindow(this Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        public static AppWindow GetAppWindowForCurrentWindow() => UIHelper.MainWindow.GetAppWindowForCurrentWindow();

        #region Window Immersive Dark Mode

        /// <summary>
        /// Tries to remove ImmersiveDarkMode effect from the <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window to which the effect is to be applied.</param>
        /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
        public static bool RemoveWindowDarkMode(this Window window)
            => GetHandle(window, out IntPtr windowHandle) && RemoveWindowDarkMode(windowHandle);

        /// <summary>
        /// Tries to remove ImmersiveDarkMode effect from the window handle.
        /// </summary>
        /// <param name="handle">Window handle.</param>
        /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
        public static bool RemoveWindowDarkMode(IntPtr handle)
        {
            int pvAttribute = 0x0; // Disable
            Dwmapi.DWMWINDOWATTRIBUTE dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (!22523.IsOSVersonGreater())
            {
                dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            // TODO: Validate HRESULT
            _ = Dwmapi.DwmSetWindowAttribute(
                handle,
                dwAttribute,
                ref pvAttribute,
                Marshal.SizeOf(typeof(int)));

            return true;
        }

        /// <summary>
        /// Tries to apply ImmersiveDarkMode effect for the <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window to which the effect is to be applied.</param>
        /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
        public static bool ApplyWindowDarkMode(this Window window)
            => GetHandle(window, out IntPtr windowHandle) && ApplyWindowDarkMode(windowHandle);

        /// <summary>
        /// Tries to apply ImmersiveDarkMode effect for the window handle.
        /// </summary>
        /// <param name="handle">Window handle.</param>
        /// <returns><see langword="true"/> if invocation of native Windows function succeeds.</returns>
        public static bool ApplyWindowDarkMode(IntPtr handle)
        {
            int pvAttribute = 0x1; // Enable
            Dwmapi.DWMWINDOWATTRIBUTE dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (!22523.IsOSVersonGreater())
            {
                dwAttribute = Dwmapi.DWMWINDOWATTRIBUTE.DMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            // TODO: Validate HRESULT
            _ = Dwmapi.DwmSetWindowAttribute(
                handle,
                dwAttribute,
                ref pvAttribute,
                Marshal.SizeOf(typeof(int)));

            return true;
        }

        #endregion

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public static bool SetTitleBar(Window window, FrameworkElement element)
            => GetHandle(window, out IntPtr windowHandle) && SetTitleBar(windowHandle, element);

        public static bool SetTitleBar(IntPtr handle, FrameworkElement element)
        {
            void On_PointerReleased(object sender, PointerRoutedEventArgs e)
            {
                ReleaseCapture();
                PointerPoint Point = e.GetCurrentPoint(sender as FrameworkElement);
                switch (Point.Properties.PointerUpdateKind)
                {
                    case PointerUpdateKind.RightButtonReleased:
                        User32.SendMessage(handle, User32.WM.NCRBUTTONUP, (IntPtr)User32.WM_NCHITTEST.CAPTION, (IntPtr)0);
                        break;
                }
            }

            void On_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
            {
                ReleaseCapture();
                User32.SendMessage(handle, User32.WM.NCLBUTTONDBLCLK, (IntPtr)User32.WM_NCHITTEST.CAPTION, (IntPtr)0);
            }

            element.PointerReleased += On_PointerReleased;
            element.DoubleTapped += On_DoubleTapped;
            return true;
        }

        /// <summary>
        /// Tries to get the pointer to the window handle.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="windowHandle"></param>
        /// <returns><see langword="true"/> if the handle is not <see cref="IntPtr.Zero"/>.</returns>
        private static bool GetHandle(Window window, out IntPtr windowHandle)
        {
            windowHandle = WindowNative.GetWindowHandle(window);

            return windowHandle != IntPtr.Zero;
        }

        public static List<Window> ActiveWindows { get; } = new List<Window>();
    }
}
