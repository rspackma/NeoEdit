﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		static void PreExecute__Help_Tutorial()
		{
			//TODO => new TutorialWindow(this);
		}

		static void PreExecute__Help_Update()
		{
			const string location = "https://github.com/rspackma/NeoEdit/releases";
			const string url = location + "/latest";
			const string check = location + "/tag/";
			const string exe = location + "/download/{0}/NeoEdit.msi";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(NEWindow).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			string newVersion;

			var request = WebRequest.Create(url) as HttpWebRequest;
			request.AllowAutoRedirect = false;
			using (var response = request.GetResponse() as HttpWebResponse)
			{
				var redirUrl = response.Headers["Location"];
				if (!redirUrl.StartsWith(check))
					throw new Exception("Version check failed to find latest version");

				newVersion = redirUrl.Substring(check.Length);
			}

			var oldNums = oldVersion.Split('.').Select(str => int.Parse(str)).ToList();
			var newNums = newVersion.Split('.').Select(str => int.Parse(str)).ToList();
			if (oldNums.Count != newNums.Count)
				throw new Exception("Version length mismatch");

			var newer = oldNums.Zip(newNums, (oldNum, newNum) => newNum.IsGreater(oldNum)).NonNull().FirstOrDefault();
			if (!state.NEWindow.neWindowUI.RunDialog_ShowMessage("Download new version?", $"Current version: {oldVersion}\nNewest version: {newVersion}\n\n{(newer ? $"A newer version is available. Download and install it?" : $"Already up to date ({newVersion}). Update anyway?")}\n\nThis will terminate all running instances of NeoEdit.", MessageOptions.YesNo, newer ? MessageOptions.Yes : MessageOptions.No, MessageOptions.No).HasFlag(MessageOptions.Yes))
				return;

			var pid = Process.GetCurrentProcess().Id;
			Process.GetProcessesByName("NeoEdit").Where(process => process.Id != pid).ForEach(process => process.Kill());

			TaskRunner.Run(progress =>
			{
				byte[] result = null;

				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) =>
					{
						try { progress(e.ProgressPercentage); }
						catch { client.CancelAsync(); }
					};
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					finished.WaitOne();
				}

				if (result == null)
					return;

				var location = Path.Combine(Path.GetTempPath(), "NeoEdit.msi");
				File.WriteAllBytes(location, result);
				Process.Start(new ProcessStartInfo(location) { UseShellExecute = true });

				Environment.Exit(0);
			});
		}

		static void PreExecute__Help_Advanced_Shell_Integrate() => INEWindowUI.ShellIntegrateStatic(true);

		static void PreExecute__Help_Advanced_Shell_Unintegrate() => INEWindowUI.ShellIntegrateStatic(false);

		static void PreExecute__Help_Advanced_CopyCommandLine()
		{
			var clipboard = new NEClipboard();
			clipboard.Add(new List<string> { Environment.CommandLine });
			NEClipboard.Current = clipboard;
		}

		static void PreExecute__Help_Advanced_RunGC() => GC.Collect();

		static void PreExecute__Help_About() => state.NEWindow.neWindowUI.RunDialog_PreExecute_Help_About();
	}
}
