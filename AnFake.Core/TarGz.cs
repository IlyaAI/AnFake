using System;
using System.Collections.Generic;
using System.IO;
using AnFake.Api;
using AnFake.Core.Impl;

namespace AnFake.Core
{
	public static class TarGz
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
			///		Overwrite mode for existing files. Used by <c>Zip.Unpack</c>
			/// </summary>
			public OverwriteMode OverwriteMode;

			internal Params()
			{			
				OverwriteMode = TarGz.OverwriteMode.None;
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default TarGz parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static TarGz()
		{
			Defaults = new Params();
		}

		/// <summary>
		///		Unpacks specified tar.gz archive recursively.
		/// </summary>
		/// <param name="targzFilePath">tar.gz archive path to be unpacked</param>
		/// <param name="targetPath">target path to unpack to</param>		
		public static void Unpack(FileSystemPath targzFilePath, FileSystemPath targetPath)
		{
			Unpack(targzFilePath, targetPath, p => { });
		}

		/// <summary>
		///		Unpacks specified tar.gz archive recursively.
		/// </summary>
		/// <param name="targzFilePath">tar.gz archive path to be unpacked</param>
		/// <param name="targetPath">target path to unpack to</param>
		/// <param name="setParams">action which overrides default parameters</param>
		public static void Unpack(FileSystemPath targzFilePath, FileSystemPath targetPath, Action<Params> setParams)
		{
			if (targzFilePath == null)
				throw new ArgumentException("TarGz.Pack(targzFilePath, targetPath[, setParams]): targzFilePath must not be null");
			if (targetPath == null)
				throw new ArgumentException("TarGz.Pack(targzFilePath, targetPath[, setParams]): targetPath must not be null");
			if (setParams == null)
				throw new ArgumentException("TarGz.Pack(targzFilePath, targetPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			Trace.InfoFormat("TarGz.Unack {0} => {1}...", targzFilePath, targetPath);

			var unzippedFiles = 0;
			using (var targz = new GZipArchiveReader(targzFilePath.AsFile()))
			{
				var gzEntry = targz.NextEntry();
				using (var tar = new TarArchiveReader(gzEntry.AsStream(), gzEntry.Name))
				{
					IArchiveEntry tarEntry;
					while ((tarEntry = tar.NextEntry()) != null)
					{
						var dstPath = targetPath/tarEntry.Name;

						if (tarEntry.IsDirectory)
						{
							Directory.CreateDirectory(dstPath.Full);
							continue;
						}

						Directory.CreateDirectory(dstPath.Parent.Full);

						if (parameters.OverwriteMode == OverwriteMode.Skip && dstPath.AsFile().Exists())
							continue;

						var mode = parameters.OverwriteMode != OverwriteMode.None
							? FileMode.Create
							: FileMode.CreateNew;

						using (var dst = new FileStream(dstPath.Full, mode, FileAccess.Write))
						{
							tarEntry.AsStream().CopyTo(dst);
							unzippedFiles++;
						}
					}
				}
			}

			Trace.InfoFormat("{0} file(s) extracted.", unzippedFiles);
		}

		/// <summary>
		///		Lists files inside tar.gz archive recursively.
		/// </summary>
		/// <param name="targzFilePath">tar.gz archive path to be unpacked</param>	
		public static List<ZippedFileItem> List(FileSystemPath targzFilePath)
		{
			if (targzFilePath == null)
				throw new ArgumentException("TarGz.List(targzFilePath): targzFilePath must not be null");
			
			var files = new List<ZippedFileItem>();
			using (var targz = new GZipArchiveReader(targzFilePath.AsFile()))
			{
				var gzEntry = targz.NextEntry();
				using (var tar = new TarArchiveReader(gzEntry.AsStream(), gzEntry.Name))
				{
					IArchiveEntry tarEntry;
					while ((tarEntry = tar.NextEntry()) != null)
					{
						if (tarEntry.IsDirectory)
							continue;

						files.Add(new ZippedFileItem(tarEntry.Name));
					}
				}			
			}

			return files;			
		}		
	}
}