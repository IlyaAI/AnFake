using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using AnFake.Integration.Tfs2012;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	public static class TfsWorkspace
	{
		public sealed class Params
		{
			public string WorkspaceFile;
			public string NameGenerationFormat;
			public int MaxGeneratedNames;
			
			internal Params()
			{
				WorkspaceFile = ".workspace";				
			}

			public Params Clone()
			{
				return (Params)MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static TfsWorkspace()
		{
			Defaults = new Params();
		}

		private static TfsPlugin _impl;

		private static TfsPlugin Impl
		{
			get
			{
				if (_impl != null)
					return _impl;

				_impl = Plugin.Get<TfsPlugin>();
				_impl.Disposed += () => _impl = null;

				return _impl;
			}
		}

		private static VersionControlServer Vcs
		{
			get { return Impl.Vcs; }
		}

		public static Predicate<string> UniqueName
		{
			get
			{
				var usedNames = Vcs.QueryWorkspaces(null, User.Current, null)
					.ToLookup(x => x.Name);

				return name => !usedNames.Contains(name);
			}
		}

		public static void UpdateInfoCache()
		{
			Workstation.Current.UpdateWorkspaceInfoCache(Vcs, User.Current);
		}

		public static void Create(ServerPath serverPath, FileSystemPath localPath, string workspaceName)
		{
			Create(serverPath, localPath, workspaceName, p => { });
		}

		public static void Create(ServerPath serverPath, FileSystemPath localPath, string workspaceName, Action<Params> setParams)
		{
			if (serverPath == null)
				throw new ArgumentException("TfsWorkspace.Create(serverPath, localPath, workspaceName[, setParams]): serverPath must not be null");
			if (!serverPath.IsRooted)
				throw new ArgumentException("TfsWorkspace.Create(serverPath, localPath, workspaceName[, setParams]): serverPath must be an absolute path");

			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.Create(serverPath, localPath, workspaceName[, setParams]): localPath must not be null");
			
			if (String.IsNullOrEmpty(workspaceName))
				throw new ArgumentException("TfsWorkspace.Create(serverPath, localPath, workspaceName[, setParams]): workspaceName must not be null or empty");

			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.Create(serverPath, localPath, workspaceName, setParams): setParams must not be null");

			var ws = Impl.FindWorkspace(workspaceName);
			if (ws != null)
				throw new InvalidConfigurationException(String.Format("Unable to create workspace '{0}' because it already exists.", workspaceName));

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);			

			Trace.InfoFormat("TfsWorkspace.Create\n ServerPath: {0}\n LocalPath: {1}\n Workspace: {2} (from '{3}')",
				serverPath, localPath, workspaceName, parameters.WorkspaceFile);

			var wsFile = (localPath / parameters.WorkspaceFile).AsFile();
			if (!wsFile.Exists())
			{
				using (var writer = new StreamWriter(wsFile.Path.Full, false, Encoding.UTF8))
				{
					WriteWorkspaceHeader(writer);
				}
			}

			var wsDesc = GetTextContent(wsFile);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			TraceMappings(mappings);

			ws = Vcs.CreateWorkspace(workspaceName, User.Current, String.Format("AnFake: {0} => {1}", serverPath, localPath), mappings);
			Trace.InfoFormat("Workspace '{0}' successfully created for '{1}'.", workspaceName, User.Current);

			UpdateFiles(ws);
		}		

		public static void Checkout(ServerPath serverPath, FileSystemPath localPath, string workspaceName)
		{
			Checkout(serverPath, localPath, workspaceName, p => { });
		}

		public static void Checkout(ServerPath serverPath, FileSystemPath localPath, string workspaceName, Action<Params> setParams)
		{
			if (serverPath == null)
				throw new ArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): serverPath must not be null");
			if (!serverPath.IsRooted)
				throw new ArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): serverPath must be an absolute path");

			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): localPath must not be null");
			if (!localPath.AsFolder().IsEmpty())
				throw new InvalidConfigurationException(String.Format("TfsWorkspace.Checkout intended for initial downloading only but target directory '{0}' is not empty.", localPath));

			if (String.IsNullOrEmpty(workspaceName))
				throw new ArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): workspaceName must not be null or empty");

			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName, setParams): setParams must not be null");

			var ws = Impl.FindWorkspace(workspaceName);			
			if (ws != null)
				throw new InvalidConfigurationException(String.Format("TfsWorkspace.Checkout intended for initial downloading only but workspace '{0}' already exists.", workspaceName));

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);
			
			var wsPath = serverPath / parameters.WorkspaceFile;

			Trace.InfoFormat("TfsWorkspace.Checkout\n ServerPath: {0}\n LocalPath: {1}\n Workspace: {2} (from '{3}')",
				serverPath, localPath, workspaceName, parameters.WorkspaceFile);

			var wsDesc = GetTextContent(wsPath);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			TraceMappings(mappings);

			ws = Vcs.CreateWorkspace(workspaceName, User.Current, String.Format("AnFake: {0} => {1}", serverPath, localPath), mappings);
			Trace.InfoFormat("Workspace '{0}' successfully created for '{1}'.", workspaceName, User.Current);

			UpdateFiles(ws);
		}

		public static void Sync(FileSystemPath localPath)
		{
			Sync(localPath, p => { });
		}

		public static void Sync(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.Sync(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.Sync(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);

			var wsFile = LocateWorkspaceFile(localPath, parameters.WorkspaceFile);
			localPath = wsFile.Folder;

			Trace.InfoFormat("TfsWorkspace.Sync: {0}", localPath);

			var ws = Impl.GetWorkspace(wsFile.Path);
			var wsPath = ws.GetServerItemForLocalItem(wsFile.Path.Full).AsServerPath();

			Trace.DebugFormat("Synchronizing workspace: {0} => {1}", wsPath, ws.Name);
			var wsDesc = GetTextContent(wsPath);
			var mappings = VcsMappings.Parse(wsDesc, wsPath.Parent.Full, localPath.Full);

			TraceMappings(mappings);

			ws.Update(ws.Name, ws.Comment, mappings);
			Trace.InfoFormat("Workspace '{0}' successfully updated.", ws.Name);

			UpdateFiles(ws);
		}

		public static void SyncLocal(FileSystemPath localPath)
		{
			SyncLocal(localPath, p => { });
		}

		public static void SyncLocal(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.SyncLocal(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.SyncLocal(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);

			var wsFile = LocateWorkspaceFile(localPath, parameters.WorkspaceFile);
			localPath = wsFile.Folder;			

			Trace.InfoFormat("TfsWorkspace.SyncLocal: {0}", localPath);

			var ws = Impl.GetWorkspace(wsFile.Path);
			var serverPath = ws.GetServerItemForLocalItem(localPath.Full).AsServerPath();

			Trace.DebugFormat("Synchronizing workspace: {0} => {1}", wsFile, ws.Name);
			var wsDesc = GetTextContent(wsFile);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			TraceMappings(mappings);

			ws.Update(ws.Name, ws.Comment, mappings);
			Trace.InfoFormat("Workspace '{0}' successfully updated.", ws.Name);

			UpdateFiles(ws);
		}

		public static void SaveLocal(FileSystemPath localPath)
		{
			SaveLocal(localPath, p => { });
		}

		public static void SaveLocal(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.SyncLocal(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.SyncLocal(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);
			
			var wsFile = (localPath / parameters.WorkspaceFile).AsFile();
			if (wsFile.Exists())
			{
				Files.Copy(wsFile, wsFile.Path.Full.MakeUnique().AsFile());
			}

			var ws = Impl.GetWorkspace(wsFile.Path);
			var serverPath = ws.GetServerItemForLocalItem(localPath.Full).AsServerPath();

			Trace.InfoFormat("TfsWorkspace.SaveLocal:\n  WorkspaceFile: {0}\n  ServerRoot: {1}\n  LocalRoot: {2}", 
				wsFile.Path.Full, serverPath.Full, localPath.Full);
			
			var errors = 0;
			using (var writer = new StreamWriter(wsFile.Path.Full, false, Encoding.UTF8))
			{
				WriteWorkspaceHeader(writer);

				foreach (var workingFolder in ws.Folders)
				{
					var relServerPath = workingFolder.ServerItem
						.AsServerPath()
						.ToRelative(serverPath);

					var err = (string)null;

					if (workingFolder.IsCloaked)
					{
						if (relServerPath.Spec == String.Empty)
							err = "Unable to cloak server root.";

						if (err != null)
						{
							writer.Write("# ERROR: ");
							writer.WriteLine(err);
							writer.Write("# -");
							writer.WriteLine(workingFolder.ServerItem);

							Trace.Message(new TraceMessage(TraceMessageLevel.Error, err) {File = wsFile.Path.Full});
							errors++;

							continue;
						}

						writer.Write('-');
						writer.WriteLine(relServerPath.Full);						
					}
					else
					{
						var relLocalPath = workingFolder.LocalItem
							.AsPath()
							.ToRelative(localPath);

						if (relServerPath.Spec == String.Empty && relLocalPath.Spec == String.Empty)
							continue;

						if (relLocalPath.IsRooted)						
							err = "All local sub-pathes should be under the same root.";						

						if (relServerPath.Spec == String.Empty && relLocalPath.Spec != String.Empty)						
							err = "Server root should be mapped to local root only.";

						if (relServerPath.Spec != String.Empty && relLocalPath.Spec == String.Empty)
							err = "Local root should be mapped to server root only.";

						if (err != null)
						{
							writer.Write("# ERROR: ");
							writer.WriteLine(err);
							writer.Write("# ");
							writer.Write(workingFolder.ServerItem);
							writer.Write(": ");
							writer.WriteLine(workingFolder.LocalItem);
							writer.Write("# ");
							writer.Write(relServerPath.Spec);
							writer.Write(": ");
							writer.WriteLine(relLocalPath.Spec);
							writer.WriteLine();

							Trace.Message(new TraceMessage(TraceMessageLevel.Error, err) {File = wsFile.Path.Full});
							errors++;

							continue;
						}

						writer.Write(relServerPath.Spec);
						writer.Write(": ");
						writer.WriteLine(relLocalPath.Spec);						
					}
				}
			}

			if (errors > 0)
				throw new TargetFailureException("TfsWorkspace.SaveLocal failed due to incompatibilities in workspace.");
			
			Trace.InfoFormat("Workspace '{0}' successfully saved.", ws.Name);
		}		

		public static void PendAdd(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("TfsWorkspace.PendAdd(files): files must not be null");

			var filePathes = files
				.Select(x => x.Path)
				.ToArray();

			var ws = Impl.GetWorkspace(filePathes[0]);
			
			Trace.InfoFormat("TfsWorkspace.PendAdd");

			foreach (var path in filePathes)
			{
				Trace.DebugFormat("  {0}", path);
			}

			var pended = ws.PendAdd(
				filePathes.Select(x => x.Full).ToArray());

			Trace.InfoFormat("{0} file(s) pended for add.", pended);
		}

		public static void Undo(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("TfsWorkspace.Undo(files): files must not be null");

			var filesArray = files.ToArray();

			var ws = Impl.GetWorkspace(filesArray[0].Path);
			
			Trace.InfoFormat("TfsWorkspace.Undo");

			foreach (var file in filesArray)
			{
				Trace.DebugFormat("  {0}", file);
			}

			ws.Undo(
				filesArray
					.Select(x => new ItemSpec(x.Path.Full, RecursionType.None))
					.ToArray());

			Trace.InfoFormat("{0} file(s) reverted.", filesArray.Length);
		}

		private static void EnsureWorkspaceFile(Params parameters)
		{
			if (String.IsNullOrEmpty(parameters.WorkspaceFile))
				throw new ArgumentException("TfsWorkspace.Params.WorkspaceFile must not be null or empty");
		}

		private static void TraceMappings(IEnumerable<WorkingFolder> mappings)
		{
			Trace.DebugFormat(
				"Mappings:\n  {0}", 
				String.Join("\n  ", 
					mappings.Select(
						m => String.Format("{0} => {1}", 
							m.ServerItem, 
							m.IsCloaked ? "(cloacked)" : m.LocalItem))));
		}

		private static string GetTextContent(ServerPath serverPath)
		{
			var item = Vcs.GetItem(serverPath.Full);
			using (var downstream = item.DownloadFile())
			{
				return new StreamReader(downstream).ReadToEnd();
			}
		}

		private static string GetTextContent(FileSystemPath localPath)
		{			
			using (var reader = new StreamReader(localPath.Full))
			{
				return reader.ReadToEnd();
			}
		}

		private static FileItem LocateWorkspaceFile(FileSystemPath localPath, string wsFileName)
		{
			var wsFile = localPath.AsFile();
			if (wsFile.Exists())
			{
				if (!wsFile.Name.Equals(wsFileName, StringComparison.OrdinalIgnoreCase))
					throw new InvalidConfigurationException(String.Format("Local path should points to workspace definition file '{0}' but really '{1}'", wsFileName, localPath));
			}
			else
			{
				wsFile = (localPath / wsFileName).AsFile();
				if (!wsFile.Exists())
					throw new InvalidConfigurationException(String.Format("Unable to locate workspace definition file '{0}' in '{1}'", wsFileName, localPath));
			}

			return wsFile;
		}

		private static void WriteWorkspaceHeader(TextWriter writer)
		{
			writer.WriteLine("# AnFake Workspace Definition File");
			writer.WriteLine("# Mapping '<project-root>: <local-root>' always added automatically");
			writer.WriteLine();
		}

		private static void UpdateFiles(Workspace ws)
		{
			Trace.Info("Updating files...");
			var status = ws.Get();

			var failures = status.GetFailures();
			foreach (var failure in status.GetFailures())
			{
				var msg = failure.GetFormattedMessage();

				Trace.Warn(msg);				
			}

			Trace.InfoFormat("Files updated. {0} warning(s)", failures.Length);			
		}
	}
}