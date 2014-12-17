using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using ICSharpCode.SharpZipLib.Zip;

namespace AnFake.Core
{
	public static class Zip
	{
		/// <summary>
		///		Overwrite mode for existing files. Used by <c>Zip.Unpack</c>
		/// </summary>
		public enum OverwriteMode
		{
			/// <summary>
			///		Do not overwrite. Throws an exception if destination file exists.
			/// </summary>
			None,

			/// <summary>
			///		Do not overwrite. Skip zipped one of destination file exists.
			/// </summary>
			Skip,

			/// <summary>
			///		Do overwrite.
			/// </summary>
			Overwrite
		}

		public sealed class Params
		{
			/// <summary>
			///     Compression level: 0 = no compression; 9 = the best compression.
			/// </summary>
			public int Level;

			/// <summary>
			///		Overwrite mode for existing files. Used by <c>Zip.Unpack</c>
			/// </summary>
			public OverwriteMode OverwriteMode;

			internal Params()
			{
				Level = 9;
				OverwriteMode = Zip.OverwriteMode.None;
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static Zip()
		{
			Defaults = new Params();
		}

		public static IToolExecutionResult Pack(IEnumerable<FileItem> files, FileSystemPath zipFilePath)
		{
			return Pack(files, zipFilePath, p => { });
		}

		public static IToolExecutionResult Pack(IEnumerable<FileItem> files, FileSystemPath zipFilePath, Action<Params> setParams)
		{
			if (files == null)
				throw new ArgumentException("Zip.Pack(files, zipFilePath[, setParams]): files must not be null");
			if (zipFilePath == null)
				throw new ArgumentException("Zip.Pack(files, zipFilePath[, setParams]): zipFilePath must not be null");
			if (setParams == null)
				throw new ArgumentException("Zip.Pack(files, zipFilePath, setParams): setParams must not be null");

			var filesToZip = files.ToArray();
			if (filesToZip.Length == 0)
				throw new ArgumentException("Zip.Pack(files, zipFilePath[, setParams]): files must contain at least one file");

			var parameters = Defaults.Clone();
			setParams(parameters);

			Trace.Info("Zip.Pack...");

			using (var zip = new ZipOutputStream(new FileStream(zipFilePath.Full, FileMode.Create, FileAccess.Write)))
			{
				zip.SetLevel(parameters.Level);

				foreach (var file in filesToZip)
				{
					Trace.DebugFormat("  {0}", file.RelPath);

					var entry = new ZipEntry(file.RelPath.Spec);
					zip.PutNextEntry(entry);

					using (var src = new FileStream(file.Path.Full, FileMode.Open, FileAccess.Read))
					{
						src.CopyTo(zip);
					}
				}				
			}

			Trace.InfoFormat("{0} file(s) zipped.", filesToZip.Length);

			return new ToolExecutionResult();
		}

		public static IToolExecutionResult Unpack(FileSystemPath zipFilePath, FileSystemPath targetPath)
		{
			return Unpack(zipFilePath, targetPath, p => { });
		}

		public static IToolExecutionResult Unpack(FileSystemPath zipFilePath, FileSystemPath targetPath, Action<Params> setParams)
		{
			if (zipFilePath == null)
				throw new ArgumentException("Zip.Pack(zipFilePath, targetPath[, setParams]): zipFilePath must not be null");
			if (targetPath == null)
				throw new ArgumentException("Zip.Pack(zipFilePath, targetPath[, setParams]): targetPath must not be null");
			if (setParams == null)
				throw new ArgumentException("Zip.Pack(zipFilePath, targetPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			Trace.InfoFormat("Zip.Unack {0} => {1}...", zipFilePath, targetPath);

			var unzippedFiles = 0;
			using (var zip = new ZipInputStream(new FileStream(zipFilePath.Full, FileMode.Open, FileAccess.Read)))
			{
				ZipEntry entry;
				while ((entry = zip.GetNextEntry()) != null)
				{
					Trace.DebugFormat("  {0}", entry.Name);

					var srcPath = entry.Name.AsPath();
					var dstPath = targetPath / srcPath;
					
					Directory.CreateDirectory(dstPath.Parent.Full);

					if (parameters.OverwriteMode == OverwriteMode.Skip && dstPath.AsFile().Exists())
						continue;

					var mode = parameters.OverwriteMode != OverwriteMode.None
						? FileMode.Create
						: FileMode.CreateNew;

					using (var dst = new FileStream(dstPath.Full, mode, FileAccess.Write))
					{
						zip.CopyTo(dst);
						unzippedFiles++;
					}					
				}
			}

			Trace.InfoFormat("{0} file(s) unzipped.", unzippedFiles);

			return new ToolExecutionResult();
		}
	}
}