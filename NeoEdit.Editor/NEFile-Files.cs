﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.Editor.PreExecution;
using NeoEdit.Editor.Searchers;
using NeoEdit.TaskRunning;
using Newtonsoft.Json.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		bool BinarySearchFile(string fileName, BinarySearcher searcher, Action<long> progress)
		{
			try
			{
				if (Directory.Exists(fileName))
					return false;

				return searcher.Find(fileName, progress);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				return QueryUser(nameof(BinarySearchFile), $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?", MessageOptions.None);
			}
		}

		static void CombineFiles(string outputFile, List<string> inputFiles, Action<long> progress)
		{
			var total = inputFiles.Sum(file => new FileInfo(file).Length);
			var written = 0L;
			var buffer = new byte[65536];
			using (var outputStream = File.Create(outputFile))
				foreach (var inputFile in inputFiles)
					using (var inputStream = File.OpenRead(inputFile))
						while (true)
						{
							var block = inputStream.Read(buffer, 0, buffer.Length);
							if (block == 0)
								break;
							outputStream.Write(buffer, 0, block);
							written += block;
							progress(written);
						}
		}

		static void CopyDirectory(string src, string dest)
		{
			var srcDirs = new List<string> { src };
			for (var ctr = 0; ctr < srcDirs.Count; ++ctr)
				srcDirs.AddRange(Directory.GetDirectories(srcDirs[ctr]));

			var srcFiles = new List<string>();
			foreach (var dir in srcDirs)
				srcFiles.AddRange(Directory.GetFiles(dir));

			var destDirs = srcDirs.Select(dir => dest + dir.Substring(src.Length)).ToList();
			var destFiles = srcFiles.Select(file => dest + file.Substring(src.Length)).ToList();
			destDirs.ForEach(dir => Directory.CreateDirectory(dir));
			for (var ctr = 0; ctr < srcFiles.Count; ++ctr)
				File.Copy(srcFiles[ctr], destFiles[ctr]);
		}

		static void FindCommonLength(string str1, string str2, ref int length)
		{
			length = Math.Min(length, Math.Min(str1.Length, str2.Length));
			for (var ctr = 0; ctr < length; ++ctr)
				if (char.ToLowerInvariant(str1[ctr]) != (char.ToLowerInvariant(str2[ctr])))
				{
					length = ctr;
					break;
				}
		}

		static int GetDepthLength(string path, int matchDepth)
		{
			if (matchDepth == 0)
				return 0;

			var depth = 0;
			for (var index = 0; index < path.Length; ++index)
				if (path[index] == '\\')
					if (++depth == matchDepth)
						return index;

			return path.Length;
		}

		static List<string> GetDirectoryContents(string dir, bool recursive, List<string> errors, Action<long> progress)
		{
			var dirs = new Queue<string>();
			dirs.Enqueue(dir);
			var results = new List<string>();
			var current = 0;
			var total = 1;
			int errorCount = 0;
			while (dirs.Count != 0)
			{
				try
				{
					var cur = dirs.Dequeue();
					++current;
					progress((long)current * 100000 / total);
					foreach (var subDir in Directory.GetDirectories(cur))
					{
						dirs.Enqueue(subDir);
						++total;
						results.Add(subDir);
					}
					results.AddRange(Directory.GetFiles(cur));
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					++errorCount;
					if (errors.Count < 10)
						errors.Add(ex.Message);
				}
				if (!recursive)
					break;
			}
			if (errorCount != errors.Count)
				errors.Add($"(Plus {errorCount - errors.Count} more)");
			return results;
		}

		NERange GetPathRange(GetPathType type, NERange range)
		{
			var path = Text.GetString(range);
			var dirLength = Math.Max(0, path.LastIndexOf('\\'));
			if ((path.StartsWith(@"\\")) && (dirLength == 1))
				dirLength = 0;
			var dirTotal = dirLength == 0 ? 0 : dirLength + 1;
			var extLen = Path.GetExtension(path).Length;

			switch (type)
			{
				case GetPathType.FileName: return new NERange(range.Start + dirTotal, range.End);
				case GetPathType.FileNameWoExtension: return new NERange(range.Start + dirTotal, range.End - extLen);
				case GetPathType.Directory: return NERange.FromIndex(range.Start, dirLength);
				case GetPathType.Extension: return new NERange(range.End - extLen, range.End);
				default: throw new ArgumentException();
			}
		}

		static string GetRelativePath(string absolutePath, string relativeDirectory)
		{
			var absoluteDirs = absolutePath.Split('\\').ToList();
			var relativeDirs = relativeDirectory.Split('\\').ToList();
			var use = 0;
			while ((use < absoluteDirs.Count) && (use < relativeDirs.Count) && (absoluteDirs[use].Equals(relativeDirs[use], StringComparison.OrdinalIgnoreCase)))
				++use;
			return use == 0 ? absolutePath : string.Join("\\", relativeDirs.Skip(use).Select(str => "..").Concat(absoluteDirs.Skip(use)));
		}

		static string GetSize(string path)
		{
			if (File.Exists(path))
			{
				var fileinfo = new FileInfo(path);
				return fileinfo.Length.ToString();
			}

			if (Directory.Exists(path))
			{
				var dirs = new List<string> { path };
				for (var ctr = 0; ctr < dirs.Count; ++ctr)
					dirs.AddRange(Directory.EnumerateDirectories(dirs[ctr]));
				int files = 0;
				long totalSize = 0;
				foreach (var dir in dirs)
				{
					foreach (var file in Directory.EnumerateFiles(dir))
					{
						++files;
						var fileinfo = new FileInfo(file);
						totalSize += fileinfo.Length;
					}
				}

				return $"{dirs.Count} directories, {files} files, {totalSize} bytes";
			}

			return "INVALID";
		}

		static void ReencodeFile(string inputFile, Action<long> progress, Coder.CodePage inputCodePage, Coder.CodePage outputCodePage)
		{
			var outputFile = Path.Combine(Path.GetDirectoryName(inputFile), Guid.NewGuid().ToString());
			try
			{
				using (var input = File.OpenRead(inputFile))
				{
					var bom = new byte[Coder.MaxPreambleSize];
					var headerSize = input.Read(bom, 0, (int)Math.Min(input.Length, bom.Length));
					Array.Resize(ref bom, headerSize);
					inputCodePage = Coder.ResolveCodePage(inputCodePage, bom);
					if (inputCodePage == outputCodePage)
						return;
					input.Position = Coder.PreambleSize(inputCodePage);

					using (var output = File.Create(outputFile))
					{
						bom = Coder.StringToBytes("", outputCodePage, true);
						output.Write(bom, 0, bom.Length);
						var decoder = Coder.GetEncoding(inputCodePage).GetDecoder();
						var encoder = Coder.GetEncoding(outputCodePage).GetEncoder();
						var chars = new char[65536];
						var bytes = new byte[chars.Length * 5]; // Should be big enough to hold any resulting output
						while (input.Position != input.Length)
						{
							var inByteCount = input.Read(bytes, 0, (int)Math.Min(chars.Length, input.Length - input.Position));

							var numChars = decoder.GetChars(bytes, 0, inByteCount, chars, 0);
							var outByteCount = encoder.GetBytes(chars, 0, numChars, bytes, 0, false);

							output.Write(bytes, 0, outByteCount);

							progress(input.Position);
						}
					}
				}
				File.Delete(inputFile);
				File.Move(outputFile, inputFile);
			}
			catch
			{
				File.Delete(outputFile);
				throw;
			}
		}

		static string RunCommand(string arguments, string workingDirectory)
		{
			var output = new StringBuilder();
			output.AppendLine($"Command: {arguments}");

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c \"{arguments}\"",
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				},
			};
			process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
			process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.AppendLine($"Error: {e.Data}"); };
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			return output.ToString();
		}

		static string SanitizeFileName(string fileName)
		{
			fileName = fileName.Trim();
			var start = "";
			if ((fileName.Length >= 2) && (fileName[1] == ':'))
				start = fileName.Substring(0, 2);
			fileName = fileName.Replace("/", @"\");
			fileName = Regex.Replace(fileName, "[<>:\"|?*\u0000-\u001f]", "_");
			fileName = start + fileName.Substring(start.Length);
			return fileName;
		}

		static void SetFileSize(string fileName, long value)
		{
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				throw new Exception($"File doesn't exist: {fileName}");

			value = Math.Max(0, value);

			if (fileInfo.Length == value)
				return;

			using (var file = File.Open(fileName, FileMode.Open))
				file.SetLength(value);
		}

		static void SplitFile(string fileName, string outputTemplate, long chunkSize, Action<long> progress)
		{
			if (chunkSize <= 0)
				throw new Exception($"Invalid chunk size: {chunkSize}");

			var chunk = 0;
			var buffer = new byte[65536];
			using (var inputFile = File.OpenRead(fileName))
				while (inputFile.Position < inputFile.Length)
					using (var outputFile = File.Create(string.Format(outputTemplate, ++chunk)))
					{
						var endChunk = Math.Min(inputFile.Position + chunkSize, inputFile.Length);
						while (inputFile.Position < endChunk)
						{
							var block = inputFile.Read(buffer, 0, (int)Math.Min(buffer.Length, endChunk - inputFile.Position));
							if (block <= 0)
								throw new Exception("Failed to read file");
							progress(inputFile.Position);
							outputFile.Write(buffer, 0, block);
						}
					}
		}

		bool TextSearchFile(string fileName, ISearcher searcher, Action<long> progress)
		{
			try
			{
				if (Directory.Exists(fileName))
					return false;

				byte[] buffer;
				using (var input = File.OpenRead(fileName))
				{
					buffer = new byte[input.Length];
					var read = 0;
					while (read < input.Length)
					{
						var block = input.Read(buffer, read, (int)(input.Length - read));
						progress(input.Position);
						read += block;
					}
				}

				return searcher.Find(Coder.BytesToString(buffer, Coder.CodePage.AutoByBOM, true)).Any();
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				return QueryUser(nameof(TextSearchFile), $"Unable to read {fileName}.\n\n{ex.Message}\n\nLeave selected?", MessageOptions.None);
			}
		}

		JToken ExtractPDF(CObject cObject, HashSet<OpCode> opCodes)
		{
			if (cObject is COperator cOperator)
			{
				opCodes.Add(cOperator.OpCode);
				return new JObject { new JProperty(cOperator.OpCode.Name, new JArray(cOperator.Operands.Select(x => ExtractPDF(x, opCodes)))) };
			}
			else if (cObject is CSequence cSequence)
				return new JArray(cSequence.Select(x => ExtractPDF(x, opCodes)).NonNull());
			else if (cObject is CString cString)
				return new JValue(cString.Value);
			else if (cObject is CInteger cInteger)
				return new JValue(cInteger.Value);
			else if (cObject is CReal cReal)
				return new JValue(cReal.Value);
			else if (cObject is CName cName)
				return new JValue(cName.Name);
			else
				return new JObject(new JProperty("Unknown type", cObject.GetType().FullName));
		}

		string GetPDFText(string fileName)
		{
			using var doc = PdfReader.Open(fileName, PdfDocumentOpenMode.Import);
			var opCodes = new HashSet<OpCode>();
			var pages = new JArray(doc.Pages.Cast<PdfPage>().Select(x => ExtractPDF(ContentReader.ReadContent(x), opCodes)));
			var opCodesObject = new JObject(opCodes.Select(x => new JProperty(x.Name, x.Description)));
			var jobject = new JObject(new JProperty("Pages", pages), new JProperty("OpCodes", opCodesObject));
			return jobject.ToString();
		}

		void Execute__Files_Select_Files() => Selections = Selections.AsTaskRunner().Where(range => File.Exists(FileName.RelativeChild(Text.GetString(range)))).ToList();

		void Execute__Files_Select_Directories() => Selections = Selections.AsTaskRunner().Where(range => Directory.Exists(FileName.RelativeChild(Text.GetString(range)))).ToList();

		void Execute__Files_Select_Existing__Files_Select_NonExisting(bool existing) => Selections = Selections.AsTaskRunner().Where(range => Helpers.FileOrDirectoryExists(FileName.RelativeChild(Text.GetString(range))) == existing).ToList();

		void Execute__Files_Select_Name_Directory__Files_Select_Name_Name__Files_Select_Name_NameWOExtension__Files_Select_Name_Extension(GetPathType type) => Selections = Selections.AsTaskRunner().Select(range => GetPathRange(type, range)).ToList();

		void Execute__Files_Select_Name_Next()
		{
			var maxPosition = Text.Length;
			var invalidChars = Path.GetInvalidFileNameChars();

			var sels = new List<NERange>();
			foreach (var range in Selections)
			{
				var endPosition = range.End;
				while ((endPosition < maxPosition) && (((endPosition - range.Start == 1) && (Text[endPosition] == ':')) || ((endPosition - range.End == 0) && ((Text[endPosition] == '\\') || (Text[endPosition] == '/'))) || (!invalidChars.Contains(Text[endPosition]))))
					++endPosition;

				sels.Add(new NERange(range.Start, endPosition));
			}

			Selections = sels;
		}

		void Execute__Files_Select_Name_CommonAncestor()
		{
			var strs = Selections.AsTaskRunner().Select(range => Text.GetString(range) + "\\").ToList();
			var depth = 0;
			if (strs.Any())
			{
				var length = strs[0].Length;
				strs.Skip(1).ForEach(str => FindCommonLength(strs[0], str, ref length));
				depth = strs[0].Substring(0, length).Count(c => c == '\\');
			}
			Selections = Selections.Select((range, index) => NERange.FromIndex(range.Start, GetDepthLength(strs[index], depth))).ToList();
		}

		void Execute__Files_Select_Name_MatchDepth()
		{
			var strs = GetSelectionStrings();
			var minDepth = strs.Select(str => str.Count(c => c == '\\') + 1).DefaultIfEmpty(0).Min();
			Selections = Selections.Select((range, index) => NERange.FromIndex(range.Start, GetDepthLength(strs[index], minDepth))).ToList();
		}

		void Execute__Files_Select_Roots__Files_Select_NonRoots(bool include)
		{
			var sels = Selections.Select(range => new { range = range, str = Text.GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') + @"\" }).ToList();
			var files = sels.Select(obj => obj.str).Distinct().OrderBy().ToList();
			var roots = new HashSet<string>();
			string root = null;
			foreach (var file in files)
			{
				if ((root != null) && (file.StartsWith(root)))
					continue;

				roots.Add(file);
				root = file;
			}

			Selections = sels.AsTaskRunner().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList();
		}

		static void Configure__Files_Select_ByContent() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Select_ByContent(state.NEWindow.Focused.GetVariables());

		void Execute__Files_Select_ByContent()
		{
			var result = state.Configuration as Configuration_Files_Select_ByContent;
			// For each file, determine strings to find
			List<List<string>> stringsToFind;

			if (result.IsExpression)
			{
				var expressionResults = GetExpressionResults<string>(result.Text, result.AlignSelections ? Selections.Count : default(int?));
				if (result.AlignSelections)
					stringsToFind = Enumerable.Range(0, Selections.Count).Select(index => new List<string> { expressionResults[index] }).ToList();
				else
					stringsToFind = Enumerable.Repeat(expressionResults, Selections.Count).ToList();
			}
			else
				stringsToFind = Enumerable.Repeat(new List<string> { result.Text }, Selections.Count).ToList();

			if (result.IsBinary)
			{
				var searchers = stringsToFind
					.Distinct()
					.ToDictionary(
						list => list,
						list => new BinarySearcher(list.SelectMany(
							item => result.CodePages
								.Select(codePage => (Coder.TryStringToBytes(item, codePage), (result.MatchCase) || (Coder.AlwaysCaseSensitive(codePage))))
								.Where(tuple => (tuple.Item1 != null) && (tuple.Item1.Length != 0))
							).Distinct().ToList()));

				Selections = Selections.AsTaskRunner()
					.Select(range => (range, fileName: FileName.RelativeChild(Text.GetString(range))))
					.Where((tuple, index, progress) => BinarySearchFile(tuple.fileName, searchers[stringsToFind[index]], progress), tuple => new FileInfo(tuple.fileName).Length)
					.Select(tuple => tuple.range)
					.ToList();
			}
			else
			{
				// Create searchers
				Dictionary<List<string>, ISearcher> searchers;
				if (result.IsRegex)
				{
					searchers = stringsToFind
						.Distinct()
						.ToDictionary(
							list => list,
							list => new RegexesSearcher(list, matchCase: result.MatchCase, firstMatchOnly: true) as ISearcher);
				}
				else
				{
					searchers = stringsToFind
						.Distinct()
						.ToDictionary(
							list => list,
							list =>
							{
								if (list.Count == 1)
									return new StringSearcher(list[0], matchCase: result.MatchCase, skipSpace: result.SkipSpace, firstMatchOnly: true) as ISearcher;
								return new StringsSearcher(list, matchCase: result.MatchCase, firstMatchOnly: true);
							});
				}

				Selections = Selections.AsTaskRunner()
					.Select(range => (range, fileName: FileName.RelativeChild(Text.GetString(range))))
					.Where((tuple, index, progress) => TextSearchFile(tuple.fileName, searchers[stringsToFind[index]], progress), tuple => new FileInfo(tuple.fileName).Length)
					.Select(tuple => tuple.range)
					.ToList();
			}
		}

		static void Configure__Files_Select_BySourceControlStatus() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Select_BySourceControlStatus();

		static void PreExecute__Files_Select_BySourceControlStatus__Files_Get_SourceControlStatus()
		{
			var files = state.NEWindow.ActiveFiles.AsTaskRunner().SelectMany(file => file.GetSelectionStrings()).Distinct().ToList();
			var statuses = Versioner.GetStatuses(files);
			state.PreExecution = new PreExecution_Files_SelectGet_BySourceControlStatus { Statuses = Enumerable.Range(0, files.Count).ToDictionary(index => files[index], index => statuses[index]) };
		}

		void Execute__Files_Select_BySourceControlStatus()
		{
			var configuration = state.Configuration as Configuration_Files_Select_BySourceControlStatus;
			var preExecution = state.PreExecution as PreExecution_Files_SelectGet_BySourceControlStatus;
			var statuses = RelativeSelectedFiles().Select(x => preExecution.Statuses[x]).ToList();
			var sels = Selections.Zip(statuses, (range, status) => new { range, status }).Where(obj => configuration.Statuses.HasFlag(obj.status)).Select(obj => obj.range).ToList();
			Selections = sels;
		}

		static void Configure__Files_Copy__Files_Move(bool move) => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_CopyMove(state.NEWindow.Focused.GetVariables(), move);

		void Execute__Files_Copy__Files_Move(bool move)
		{
			var result = state.Configuration as Configuration_Files_CopyMove;
			var variables = GetVariables();

			var oldFileNameExpression = state.GetExpression(result.OldFileName);
			var newFileNameExpression = state.GetExpression(result.NewFileName);
			var rowCount = variables.RowCount(oldFileNameExpression, newFileNameExpression);

			var oldFileNames = oldFileNameExpression.Evaluate<string>(variables, rowCount);
			var newFileNames = newFileNameExpression.Evaluate<string>(variables, rowCount);

			const int InvalidCount = 10;
			var invalid = oldFileNames.Distinct().Where(name => !Helpers.FileOrDirectoryExists(name)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Sources don't exist:\n{string.Join("\n", invalid)}");

			invalid = newFileNames.Select(name => Path.GetDirectoryName(name)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			// If user specified a file and a directory, assume they want the file (with the same name) in that directory
			newFileNames = newFileNames.Zip(oldFileNames, (newFileName, oldFileName) => (File.Exists(oldFileName)) && (Directory.Exists(newFileName)) ? Path.Combine(newFileName, Path.GetFileName(oldFileName)) : newFileName).ToList();

			invalid = oldFileNames.Zip(newFileNames, (oldFileName, newFileName) => new { oldFileName, newFileName }).Where(obj => (Directory.Exists(obj.newFileName)) || ((Directory.Exists(obj.oldFileName)) && (File.Exists(obj.newFileName)))).Select(pair => pair.newFileName).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Destinations already exist:\n{string.Join("\n", invalid)}");

			var confirmCopyMove = $"{nameof(Execute__Files_Copy__Files_Move)}_Confirm";
			if (!QueryUser(confirmCopyMove, $"Are you sure you want to {(move ? "move" : "copy")} these {rowCount} files/directories?", MessageOptions.Yes))
				return;

			var overwriteCopyMove = $"{nameof(Execute__Files_Copy__Files_Move)}_Overwrite";
			invalid = newFileNames.Zip(oldFileNames, (newFileName, oldFileName) => new { newFileName, oldFileName }).Where(obj => (!string.Equals(obj.newFileName, obj.oldFileName, StringComparison.OrdinalIgnoreCase)) && (File.Exists(obj.newFileName))).Select(obj => obj.newFileName).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (!QueryUser(overwriteCopyMove, $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}", MessageOptions.Yes))
					return;
			}

			for (var ctr = 0; ctr < oldFileNames.Count; ++ctr)
				if (Directory.Exists(oldFileNames[ctr]))
				{
					if (move)
						Directory.Move(oldFileNames[ctr], newFileNames[ctr]);
					else
						CopyDirectory(oldFileNames[ctr], newFileNames[ctr]);
				}
				else
				{
					if ((File.Exists(newFileNames[ctr])) && (!string.Equals(oldFileNames[ctr], newFileNames[ctr], StringComparison.OrdinalIgnoreCase)))
						File.Delete(newFileNames[ctr]);

					if (move)
						File.Move(oldFileNames[ctr], newFileNames[ctr]);
					else
						File.Copy(oldFileNames[ctr], newFileNames[ctr]);
				}
		}

		void Execute__Files_Delete()
		{
			var files = RelativeSelectedFiles();
			if (!files.Any())
				return;

			var sureAnswer = $"{nameof(Execute__Files_Delete)}_Sure";
			var continueAnswer = $"{nameof(Execute__Files_Delete)}_Continue";

			if (!QueryUser(sureAnswer, "Are you sure you want to delete these files/directories?", MessageOptions.No))
				return;

			foreach (var file in files)
			{
				try
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
				catch (Exception ex)
				{
					if (!QueryUser(continueAnswer, $"An error occurred:\n\n{ex.Message}\n\nContinue?", MessageOptions.Yes))
						throw new OperationCanceledException();
				}
			}
		}

		static void Configure__Files_Name_MakeAbsolute() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Name_MakeAbsoluteRelative(state.NEWindow.Focused.GetVariables(), true, true);

		void Execute__Files_Name_MakeAbsolute()
		{
			var result = state.Configuration as Configuration_Files_Name_MakeAbsoluteRelative;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == Configuration_Files_Name_MakeAbsoluteRelative.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		static void Configure__Files_Name_MakeRelative() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Name_MakeAbsoluteRelative(state.NEWindow.Focused.GetVariables(), false, true);

		void Execute__Files_Name_MakeRelative()
		{
			var result = state.Configuration as Configuration_Files_Name_MakeAbsoluteRelative;
			var results = GetExpressionResults<string>(result.Expression, Selections.Count());
			if (result.Type == Configuration_Files_Name_MakeAbsoluteRelative.ResultType.File)
				results = results.Select(str => Path.GetDirectoryName(str)).ToList();
			ReplaceSelections(GetSelectionStrings().Select((str, index) => GetRelativePath(str, results[index])).ToList());
		}

		void Execute__Files_Name_Simplify() => ReplaceSelections(Selections.Select(range => Path.GetFullPath(Text.GetString(range))).ToList());

		void Execute__Files_Name_Sanitize() => ReplaceSelections(Selections.Select(range => SanitizeFileName(Text.GetString(range))).ToList());

		void Execute__Files_Get_Size() => ReplaceSelections(RelativeSelectedFiles().Select(file => GetSize(file)).ToList());

		void Execute__Files_Get_Time_Write__Files_Get_Time_Access__Files_Get_Time_Create(TimestampType timestampType)
		{
			Func<FileSystemInfo, DateTime> getTime;
			switch (timestampType)
			{
				case TimestampType.Write: getTime = fi => fi.LastWriteTime; break;
				case TimestampType.Access: getTime = fi => fi.LastAccessTime; break;
				case TimestampType.Create: getTime = fi => fi.CreationTime; break;
				default: throw new Exception("Invalid TimestampType");
			}

			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
					strs.Add(getTime(new FileInfo(file)).ToString("yyyy-MM-dd HH:mm:ss.fff"));
				else if (Directory.Exists(file))
					strs.Add(getTime(new DirectoryInfo(file)).ToString("yyyy-MM-dd HH:mm:ss.fff"));
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Execute__Files_Get_Attributes()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.Attributes.ToString());
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.Attributes.ToString());
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		void Execute__Files_Get_Version_File() => ReplaceSelections(Selections.AsTaskRunner().Select(range => FileName.RelativeChild(Text.GetString(range))).Select(file => FileVersionInfo.GetVersionInfo(file).FileVersion).ToList());

		void Execute__Files_Get_Version_Product() => ReplaceSelections(Selections.AsTaskRunner().Select(range => FileName.RelativeChild(Text.GetString(range))).Select(file => FileVersionInfo.GetVersionInfo(file).ProductVersion).ToList());

		void Execute__Files_Get_Version_Assembly() => ReplaceSelections(Selections.AsTaskRunner().Select(range => FileName.RelativeChild(Text.GetString(range))).Select(file => AssemblyName.GetAssemblyName(file).Version.ToString()).ToList());

		static void Configure__Files_Get_Hash() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Get_Hash();

		void Execute__Files_Get_Hash()
		{
			var result = state.Configuration as Configuration_Files_Get_Hash;
			ReplaceSelections(Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.Select((fileName, index, progress) => Hasher.Get(fileName, result.HashType, progress), fileName => new FileInfo(fileName).Length)
				.ToList());
		}

		void Execute__Files_Get_SourceControlStatus()
		{
			var preExecution = state.PreExecution as PreExecution_Files_SelectGet_BySourceControlStatus;
			ReplaceSelections(Selections.AsTaskRunner().Select(range => FileName.RelativeChild(Text.GetString(range))).Select(x => preExecution.Statuses[x].ToString()).ToList());
		}

		void Execute__Files_Get_Children__Files_Get_Descendants(bool recursive)
		{
			var dirs = RelativeSelectedFiles();
			if (dirs.Any(dir => !Directory.Exists(dir)))
				throw new ArgumentException("Path must be of existing directories");

			var errors = new List<string>();
			ReplaceSelections(dirs.AsTaskRunner().Select((dir, index, progress) => string.Join(Text.DefaultEnding, GetDirectoryContents(dir, recursive, errors, progress)), x => 100000).ToList());
			if (errors.Any())
				NEWindow.neWindowUI.RunDialog_ShowMessage("Error", $"The following error(s) occurred:\n{string.Join("\n", errors)}");
		}

		static void Configure__Files_Get_Content() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Get_Content();

		void Execute__Files_Get_Content()
		{
			var result = state.Configuration as Configuration_Files_Get_Content;
			ReplaceSelections(RelativeSelectedFiles().AsTaskRunner().Select(fileName => Coder.BytesToString(File.ReadAllBytes(fileName), result.CodePage, true)).ToList());
		}

		static void Configure__Files_Set_Size()
		{
			var vars = state.NEWindow.Focused.GetVariables();
			var sizes = state.NEWindow.Focused.RelativeSelectedFiles().AsTaskRunner().Select(file => new FileInfo(file).Length).ToList();
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Set_Size(vars);
		}

		void Execute__Files_Set_Size()
		{
			var result = state.Configuration as Configuration_Files_Set_Size;
			var vars = GetVariables();
			var files = RelativeSelectedFiles();
			var sizes = files.AsTaskRunner().Select(file => new FileInfo(file).Length).ToList();
			vars.Add(NEVariable.List("size", "File size", () => sizes));
			var results = state.GetExpression(result.Expression).Evaluate<long>(vars, Selections.Count()).Select(size => size * result.Factor).ToList();
			files.Zip(results, (file, size) => new { file, size }).ForEach(obj => SetFileSize(obj.file, obj.size));
		}

		static void Configure__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Set_Time_Various(state.NEWindow.Focused.GetVariables(), $@"""{DateTime.Now}""");

		void Execute__Files_Set_Time_Write__Files_Set_Time_Access__Files_Set_Time_Create__Files_Set_Time_All(TimestampType type)
		{
			var result = state.Configuration as Configuration_Files_Set_Time_Various;
			var dateTimes = GetExpressionResults<DateTime>(result.Expression, Selections.Count());
			var files = RelativeSelectedFiles();
			for (var ctr = 0; ctr < files.Count; ++ctr)
			{
				var dateTime = dateTimes[ctr];
				var file = files[ctr];
				if (!Helpers.FileOrDirectoryExists(file))
					File.WriteAllBytes(file, new byte[0]);

				if (File.Exists(file))
				{
					var info = new FileInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = dateTime;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = dateTime;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = dateTime;
				}
				else if (Directory.Exists(file))
				{
					var info = new DirectoryInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = dateTime;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = dateTime;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = dateTime;
				}
			}
		}

		static void Configure__Files_Set_Attributes()
		{
			var filesAttrs = state.NEWindow.Focused.Selections.Select(range => state.NEWindow.Focused.Text.GetString(range)).Select(file => new DirectoryInfo(file).Attributes).ToList();
			var availAttrs = Helpers.GetValues<FileAttributes>();
			var current = new Dictionary<FileAttributes, bool?>();
			foreach (var fileAttrs in filesAttrs)
				foreach (var availAttr in availAttrs)
				{
					var fileHasAttr = fileAttrs.HasFlag(availAttr);
					if (!current.ContainsKey(availAttr))
						current[availAttr] = fileHasAttr;
					if (current[availAttr] != fileHasAttr)
						current[availAttr] = null;
				}

			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Set_Attributes(current);
		}

		void Execute__Files_Set_Attributes()
		{
			var result = state.Configuration as Configuration_Files_Set_Attributes;
			FileAttributes andMask = 0, orMask = 0;
			foreach (var pair in result.Attributes)
			{
				andMask |= pair.Key;
				if ((pair.Value.HasValue) && (pair.Value.Value))
					orMask |= pair.Key;
			}
			foreach (var file in Selections.Select(range => Text.GetString(range)))
				new FileInfo(file).Attributes = new FileInfo(file).Attributes & ~andMask | orMask;
		}

		static void Configure__Files_Set_Content() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Set_Content(state.NEWindow.Focused.GetVariables(), state.NEWindow.Focused.CodePage);

		void Execute__Files_Set_Content()
		{
			var result = state.Configuration as Configuration_Files_Set_Content;
			var variables = GetVariables();

			var filenameExpression = state.GetExpression(result.FileName);
			var dataExpression = state.GetExpression(result.Data);
			var rowCount = variables.RowCount(filenameExpression, dataExpression);

			var filename = filenameExpression.Evaluate<string>(variables, rowCount);
			var data = dataExpression.Evaluate<string>(variables, rowCount);
			for (var ctr = 0; ctr < data.Count; ++ctr)
				File.WriteAllBytes(filename[ctr], Coder.StringToBytes(data[ctr], result.CodePage, true));
		}

		static void Configure__Files_Set_Encoding() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Set_Encoding();

		void Execute__Files_Set_Encoding()
		{
			var result = state.Configuration as Configuration_Files_Set_Encoding;
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll(
					(fileName, index, progress) => ReencodeFile(fileName, progress, result.InputCodePage, result.OutputCodePage),
					fileName => new FileInfo(fileName).Length);
		}

		void Execute__Files_Create_Files()
		{
			var files = RelativeSelectedFiles();
			if (files.Any(file => Directory.Exists(file)))
				throw new Exception("Directory already exists");
			files = files.Where(file => !File.Exists(file)).ToList();
			var data = new byte[0];
			foreach (var file in files)
				File.WriteAllBytes(file, data);
		}

		void Execute__Files_Create_Directories()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		static void Configure__Files_Compress() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_CompressDecompress(true);

		void Execute__Files_Compress()
		{
			var result = state.Configuration as Configuration_Files_CompressDecompress;
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll((fileName, index, progress) => Compressor.Compress(fileName, result.CompressorType, progress), fileName => new FileInfo(fileName).Length);
		}

		static void Configure__Files_Decompress() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_CompressDecompress(false);

		void Execute__Files_Decompress()
		{
			var result = state.Configuration as Configuration_Files_CompressDecompress;
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll((fileName, index, progress) => Compressor.Decompress(fileName, result.CompressorType, progress), fileName => new FileInfo(fileName).Length);
		}

		static void Configure__Files_Encrypt() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_EncryptDecrypt(true);

		void Execute__Files_Encrypt()
		{
			var result = state.Configuration as Configuration_Files_EncryptDecrypt;
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll((fileName, index, progress) => Cryptor.Encrypt(fileName, result.CryptorType, result.Key, progress), fileName => new FileInfo(fileName).Length);
		}

		static void Configure__Files_Decrypt() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_EncryptDecrypt(false);

		void Execute__Files_Decrypt()
		{
			var result = state.Configuration as Configuration_Files_EncryptDecrypt;
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll((fileName, index, progress) => Cryptor.Decrypt(fileName, result.CryptorType, result.Key, progress), fileName => new FileInfo(fileName).Length);
		}

		static void Configure__Files_Sign() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Sign();

		void Execute__Files_Sign()
		{
			var result = state.Configuration as Configuration_Files_Sign;
			ReplaceSelections(RelativeSelectedFiles().Select(file => Cryptor.Sign(file, result.CryptorType, result.Key, result.Hash)).ToList());
		}

		void Execute__Files_Advanced_Explore()
		{
			if (Selections.Count != 1)
				throw new Exception("Can only explore one file.");
			Process.Start("explorer.exe", $"/select,\"{RelativeSelectedFiles()[0]}\"");
		}

		void Execute__Files_Advanced_CommandPrompt()
		{
			var dirs = RelativeSelectedFiles().Select(path => File.Exists(path) ? Path.GetDirectoryName(path) : path).Distinct().ToList();
			if (dirs.Count != 1)
				throw new Exception("Too many file locations.");
			Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = dirs[0] });
		}

		void Execute__Files_Advanced_DragDrop()
		{
			var strs = RelativeSelectedFiles();
			if (!StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			strs.ForEach(AddDragFile);
		}

		static void Configure__Files_Advanced_SplitFiles()
		{
			var variables = state.NEWindow.Focused.GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", () => 1));
			state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Advanced_SplitFiles(variables);
		}

		void Execute__Files_Advanced_SplitFiles()
		{
			var result = state.Configuration as Configuration_Files_Advanced_SplitFiles;
			var variables = GetVariables();
			variables.Add(NEVariable.Constant("chunk", "Chunk number", () => "{0}"));
			var files = RelativeSelectedFiles();
			var outputTemplates = state.GetExpression(result.OutputTemplate).Evaluate<string>(variables, Selections.Count);
			var chunkSizes = state.GetExpression(result.ChunkSize, "bytes").Evaluate<long>(variables, Selections.Count);
			Selections.AsTaskRunner()
				.Select(range => FileName.RelativeChild(Text.GetString(range)))
				.ForAll(
					(fileName, index, progress) => SplitFile(fileName, outputTemplates[index], chunkSizes[index], progress),
					fileName => new FileInfo(fileName).Length);
		}

		static void Configure__Files_Advanced_CombineFiles() => state.Configuration = state.NEWindow.neWindowUI.RunDialog_Configure_Files_Advanced_CombineFiles(state.NEWindow.Focused.GetVariables());

		void Execute__Files_Advanced_CombineFiles()
		{
			var result = state.Configuration as Configuration_Files_Advanced_CombineFiles;
			var variables = GetVariables();

			var inputFileCountExpr = state.GetExpression(result.InputFileCount);
			var outputFilesExpr = state.GetExpression(result.OutputFiles);
			var count = variables.RowCount(inputFileCountExpr, outputFilesExpr);

			var inputFiles = state.GetExpression(result.InputFiles).Evaluate<string>(variables);
			var inputFileCount = inputFileCountExpr.Evaluate<int>(variables, count);
			var outputFiles = outputFilesExpr.Evaluate<string>(variables, count);

			if (inputFiles.Count != inputFileCount.Sum())
				throw new Exception("Invalid input file count");

			var current = -1;
			var inputs = new List<List<string>>();
			foreach (var inputFile in inputFiles)
			{
				while ((current < 0) || (inputs[current].Count == inputFileCount[current]))
				{
					++current;
					inputs.Add(new List<string>());
				}

				inputs[current].Add(inputFile);
			}

			TaskRunner.Range(0, outputFiles.Count)
				.ForAll(
					(item, index, progress) => CombineFiles(outputFiles[index], inputs[index], progress),
					item => inputs[item].Sum(fileName => new FileInfo(fileName).Length));
		}

		void Execute__Files_Advanced_ExtractPDF() => ReplaceSelections(Selections.AsTaskRunner().Select(range => GetPDFText(Text.GetString(range))).ToList());
	}
}
