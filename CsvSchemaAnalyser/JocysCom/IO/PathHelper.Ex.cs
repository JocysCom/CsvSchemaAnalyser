using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Summary description for PathConvert.
	/// Provides Windows path utilities: case-corrected path retrieval via Win32 and special folder placeholder conversion.
	/// </summary>
	public static partial class PathHelper
	{

#if NETFRAMEWORK

		#region Get Cased Path

		/// <summary>
		/// Retrieves the existing file or directory path with proper casing for each path segment by querying Win32 FindFirstFile.
		/// </summary>
		/// <param name="fullName">The full file or directory path to correct.</param>
		/// <returns>The existing file system path with actual casing; returns the original <paramref name="fullName"/> if the path does not exist.</returns>
		public static string GetCasedPath(string fullName)
		{
			bool isFile = System.IO.File.Exists(fullName);
			bool isDir = System.IO.Directory.Exists(fullName);
			if (isDir && !fullName.EndsWith("\\")) fullName += "\\";
			if (!isFile && !isDir) return fullName;
			//throw new System.IO.FileNotFoundException("File doesn’t exist");
			string pathbit = fullName;
			Stack<string> pathStack = new Stack<string>();
			string dirName = System.IO.Path.GetDirectoryName(pathbit);
			while (dirName != null)
			{
				pathStack.Push(dirName);
				dirName = System.IO.Path.GetDirectoryName(dirName);
			}
			string realPath = string.Empty;
			while (pathStack.Count > 0)
			{
				dirName = pathStack.Pop();
				if (System.IO.Path.GetPathRoot(dirName) == dirName) realPath = dirName.ToUpper();
				else SetRealPath(dirName, ref realPath);
			}
			if (isFile) SetRealPath(fullName, ref realPath);
			return realPath;
		}

		/// <summary>
		/// Helper for <see cref="GetCasedPath"/>: appends the correctly-cased file or directory name of <paramref name="fullPath"/> to <paramref name="realPath"/> by using Win32 FindFirstFile.
		/// </summary>
		/// <param name="fullPath">The file or directory to query.</param>
		/// <param name="realPath">The accumulating path, updated with the proper case of the final segment.</param>
		private static void SetRealPath(string fullPath, ref string realPath)
		{
			Win32.WIN32_FIND_DATA data = new Win32.WIN32_FIND_DATA();
			IntPtr findHandle = Win32.NativeMethods.FindFirstFile(fullPath, ref data);
			realPath = System.IO.Path.Combine(realPath, data.cFileName);
			Win32.NativeMethods.FindClose(findHandle);
		}

		#endregion

#endif

		/// <summary>Replaces absolute special folder paths in the given path with placeholder patterns.</summary>
		/// <param name="path">The file system path to convert.</param>
		/// <param name="keyPrefix">Prefix for placeholders (default: "$(").</param>
		/// <param name="keySuffix">Suffix for placeholders (default: ")").</param>
		/// <returns>The converted path with special folder placeholders.</returns>
		public static string ConvertToSpecialFoldersPattern(string path, string keyPrefix = "$(", string keySuffix = ")")
		{
			foreach (var key in SpecialFolders.Keys)
				path = Text.Helper.Replace(path, SpecialFolders[key], keyPrefix + key + keySuffix, StringComparison.OrdinalIgnoreCase);
			return path;
		}

		/// <summary>Replaces placeholder patterns with actual special folder paths in the given path.</summary>
		/// <param name="path">The path containing special folder placeholders.</param>
		/// <param name="keyPrefix">Prefix for placeholders (default: "$(").</param>
		/// <param name="keySuffix">Suffix for placeholders (default: ")").</param>
		/// <returns>The path with placeholders replaced by actual special folder paths.</returns>
		public static string ConvertFromSpecialFoldersPattern(string path, string keyPrefix = "$(", string keySuffix = ")")
		{
			foreach (var key in SpecialFolders.Keys)
				path = Text.Helper.Replace(path, keyPrefix + key + keySuffix, SpecialFolders[key], StringComparison.OrdinalIgnoreCase);
			return path;
		}

	}
}