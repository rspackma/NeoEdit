using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NeoEdit.UI.Controls
{
	public class Win32
	{
		public static void SetupMinMaxInfo(Window window)
		{
			var hwnd = (new WindowInteropHelper(window)).Handle;
			HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WindowProc));
		}

		static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case 0x0024:
					WmGetMinMaxInfo(hwnd, lParam);
					handled = true;
					break;
			}
			return IntPtr.Zero;
		}

		static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
		{
			var mmi = (MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(MinMaxInfo));

			var monitor = MonitorFromWindow(hwnd, 0x00000002);

			if (monitor != IntPtr.Zero)
			{
				var monitorInfo = new MonitorInfo();
				GetMonitorInfo(monitor, monitorInfo);
				mmi.MaxPosition.X = Math.Abs(monitorInfo.Work.Left - monitorInfo.Monitor.Left);
				mmi.MaxPosition.Y = Math.Abs(monitorInfo.Work.Top - monitorInfo.Monitor.Top);
				mmi.MaxSize.X = Math.Abs(monitorInfo.Work.Right - monitorInfo.Work.Left);
				mmi.MaxSize.Y = Math.Abs(monitorInfo.Work.Bottom - monitorInfo.Work.Top);
			}

			Marshal.StructureToPtr(mmi, lParam, true);
		}

		[DllImport("user32")] static extern bool GetMonitorInfo(IntPtr hMonitor, MonitorInfo lpmi);
		[DllImport("User32")] static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

		[StructLayout(LayoutKind.Sequential)]
		struct Point(int x, int y)
		{
			public int X = x, Y = y;
			public override string ToString() => $"({X},{Y})";
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Rect(int left, int top, int right, int bottom)
		{
			public int Left = left, Top = top, Right = right, Bottom = bottom;
			public override string ToString() => $"({Left},{Right})-({Top},{Bottom})";
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MinMaxInfo
		{
			public Point Reserved;
			public Point MaxSize;
			public Point MaxPosition;
			public Point MinTrackSize;
			public Point MaxTrackSize;

			public override string ToString() => $"Reserved: {Reserved}, MaxSize: {MaxSize}, MaxPosition: {MaxPosition}, MinTrackSize: {MinTrackSize}, MaxTrackSize: {MaxTrackSize}";
		};

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		class MonitorInfo
		{
			public int Size = Marshal.SizeOf(typeof(MonitorInfo));
			public Rect Monitor = new();
			public Rect Work = new();
			public int Flags = 0;

			public override string ToString() => $"Size: {Size}, Monitor: {Monitor}, Work: {Work}, Flags: {Flags}";
		}
	}
}
