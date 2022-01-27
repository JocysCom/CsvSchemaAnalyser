using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Summary description for PathConvert.
	/// </summary>
	public static partial class PathHelper
	{

#if NETSTANDARD
#elif NETCOREAPP
#else

		#region Get Cased Path

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

		private static void SetRealPath(string fullPath, ref string realPath)
		{
			Win32.WIN32_FIND_DATA data = new Win32.WIN32_FIND_DATA();
			IntPtr findHandle = Win32.NativeMethods.FindFirstFile(fullPath, ref data);
			realPath = System.IO.Path.Combine(realPath, data.cFileName);
			Win32.NativeMethods.FindClose(findHandle);
		}

		#endregion

#endif

		public static string ConvertToSpecialFoldersPattern(string path, string keyPrefix = "$(", string keySuffix = ")")
		{
			foreach (var key in SpecialFolders.Keys)
				path = Text.Helper.Replace(path, SpecialFolders[key], keyPrefix + key + keySuffix, StringComparison.OrdinalIgnoreCase);
			return path;
		}

		public static string ConvertFromSpecialFoldersPattern(string path, string keyPrefix = "$(", string keySuffix = ")")
		{
			foreach (var key in SpecialFolders.Keys)
				path = Text.Helper.Replace(path, keyPrefix + key + keySuffix, SpecialFolders[key], StringComparison.OrdinalIgnoreCase);
			return path;
		}

	}
}
