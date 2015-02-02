using System;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	/// <summary>
	///		Represents Robocopy tool.
	/// </summary>
	/// <seealso cref="https://technet.microsoft.com/en-us/library/cc733145.aspx"/>
	public static class Robocopy
	{
		private static readonly string[] Locations =
		{
			"[System]/Robocopy.exe"
		};

		/// <summary>
		///		Sub-folders processing mode.
		/// </summary>
		public enum RecursionMode
		{
			/// <summary>
			///		Do not copy sub-folders (default).
			/// </summary>
			None,

			/// <summary>
			///		Copy only non empty sub-folders. Equals to '/s' Robocopy command line option.
			/// </summary>
			NonEmptyOnly,

			/// <summary>
			///		Copy all sub-folders. Equals to '/e' Robocopy command line option.
			/// </summary>
			All
		}

		/// <summary>
		///		Robocopy parameters.
		/// </summary>
		public sealed class Params
		{
			public RecursionMode Recursion;			
			public string ExcludeFiles;
			public string ExcludeFolders;
			public bool Purge;
			public bool DeleteSourceFiles;
			public TimeSpan Timeout;
			public FileSystemPath ToolPath;
			public string ToolArguments;

			internal Params()
			{				
				Timeout = TimeSpan.MaxValue;
				ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default Robocopy parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static Robocopy()
		{
			Defaults = new Params();
		}

		/// <summary>
		///		Copies all files from 'sourcePath' to 'destinationPath' without recursion.
		/// </summary>
		/// <param name="sourcePath">source path to copy from (not null)</param>
		/// <param name="destinationPath">destination path to copy to (not null)</param>
		public static void Copy(FileSystemPath sourcePath, FileSystemPath destinationPath)
		{
			Copy(sourcePath, destinationPath, "*.*", p => { });
		}

		/// <summary>
		///		Copies files matched by given 'filesMask' from 'sourcePath' to 'destinationPath' without recursion.
		/// </summary>
		/// <param name="sourcePath">source path to copy from (not null)</param>
		/// <param name="destinationPath">destination path to copy to (not null)</param>
		/// <param name="filesMask">action which overrides default parameters (not null)</param>
		public static void Copy(FileSystemPath sourcePath, FileSystemPath destinationPath, string filesMask)
		{
			Copy(sourcePath, destinationPath, filesMask, p => { });
		}

		/// <summary>
		///		Copies all files from 'sourcePath' to 'destinationPath' with parameters provided by 'setParams'.
		/// </summary>
		/// <param name="sourcePath">source path to copy from (not null)</param>
		/// <param name="destinationPath">destination path to copy to (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
		public static void Copy(FileSystemPath sourcePath, FileSystemPath destinationPath, Action<Params> setParams)
		{
			Copy(sourcePath, destinationPath, "*.*", setParams);
		}

		/// <summary>
		///		Runs Robocopy with specified arguments.
		/// </summary>
		/// <param name="sourcePath">source path to copy from (not null)</param>
		/// <param name="destinationPath">destination path to copy to (not null)</param>
		/// <param name="filesMask">mask of files to be copied (not null or empty)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
		/// <remarks>
		///		<para>The 'filesMask' parameter might include several wildcards separated by space, e.g. '*.txt *.doc'.</para>
		/// </remarks>
		public static void Copy(FileSystemPath sourcePath, FileSystemPath destinationPath, string filesMask, Action<Params> setParams)
		{
			if (sourcePath == null)
				throw new ArgumentException("Robocopy.Copy(sourcePath, destinationPath, filesMask[, setParams]): sourcePath must not be null");
			if (destinationPath == null)
				throw new ArgumentException("Robocopy.Copy(sourcePath, destinationPath, filesMask[, setParams]): destinationPath must not be null");
			if (String.IsNullOrEmpty(filesMask))
				throw new ArgumentException("Robocopy.Copy(sourcePath, destinationPath, filesMask[, setParams]): filesMask must not be null or empty");
			if (setParams == null)
				throw new ArgumentException("Robocopy.Copy(sourcePath, destinationPath, filesMask, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"Robocopy.Params.ToolPath must not be null.\nHint: probably, Robocopy.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));

			Trace.InfoFormat("Robocopy.Copy: '{0}' to '{1}'...", sourcePath, destinationPath);

			var args = new Args("/", " ")
				.Param(sourcePath.Full)
				.Param(destinationPath.Full)
				.Space().NonQuotedValue(filesMask)
				.Option("PURGE", parameters.Purge)
				.Option("mov", parameters.DeleteSourceFiles);
			
			switch (parameters.Recursion)
			{
				case RecursionMode.NonEmptyOnly:
					args.Option("s");
					break;
				case RecursionMode.All:
					args.Option("e");
					break;
			}

			if (!String.IsNullOrEmpty(parameters.ExcludeFiles))
			{
				args.Space().NonQuotedValue("/XF")
					.Space().NonQuotedValue(parameters.ExcludeFiles);
			}

			if (!String.IsNullOrEmpty(parameters.ExcludeFolders))
			{
				args.Space().NonQuotedValue("/XD")
					.Space().NonQuotedValue(parameters.ExcludeFolders);
			}

			args.Other(parameters.ToolArguments);

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();
			});

			result
				.FailIfAnyError("Target terminated due to Robocopy errors.")
				.FailIf(
					r => r.ExitCode > 4,
					String.Format("Robocopy failed with exit code {0}.", result.ExitCode));
		}
	}
}