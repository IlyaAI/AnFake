using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AnFake.Core
{
	public static class SymLink
	{
		private enum SymbolicLink
		{
			File = 0,
			Directory = 1
		}

		[DllImport("kernel32.dll")]
		private static extern bool CreateSymbolicLink(string symlinkPath, string targetPath, SymbolicLink flags);

		public static void Create(FileSystemPath symlinkPath, FolderItem targetFolder)
		{
			if (!CreateSymbolicLink(symlinkPath.Full, targetFolder.Path.Full, SymbolicLink.Directory))
				throw new Win32Exception();
		}

		public static void Create(FileSystemPath symlinkPath, FileItem targetFile)
		{
			if (!CreateSymbolicLink(symlinkPath.Full, targetFile.Path.Full, SymbolicLink.File))
				throw new Win32Exception();
		}
	}
}