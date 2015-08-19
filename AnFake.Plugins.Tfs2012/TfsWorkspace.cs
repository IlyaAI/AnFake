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
	/// <summary>
	///		Represents tools related to TFS workspace.
	/// </summary>
	public static class TfsWorkspace
	{
		/// <summary>
		///		Workspace parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Workspace definition file name. Default '.workspace'.
			/// </summary>
			public string WorkspaceFile;

			/// <summary>
			///		Version specification. Default 'T' = Latest. Used by <c>Checkout</c> and <c>Get</c> methods.
			/// </summary>
			public string VersionSpec;
			
			internal Params()
			{
				WorkspaceFile = ".workspace";
				VersionSpec = "T";
			}

			/// <summary>
			///		Clones Params structure.
			/// </summary>
			/// <returns></returns>
			public Params Clone()
			{
				return (Params)MemberwiseClone();
			}
		}

		/// <summary>
		///		Default workspace parameters.
		/// </summary>
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

		/// <summary>
		///		Predicate to use with <c>NameGen</c>. Returns true if workspace with given name doesn't exist.
		/// </summary>
		public static Predicate<string> UniqueName
		{
			get
			{
				return name => Impl.FindWorkspace(name) == null;
			}
		}

		/// <summary>
		///		Updates local workspace info cache.
		/// </summary>
		public static void UpdateInfoCache()
		{
			Workstation.Current.UpdateWorkspaceInfoCache(Vcs, User.Current);
		}

		/// <summary>
		///		Creates new workspace using workspace definition file.
		/// </summary>
		/// <param name="serverPath">TFS path (not null)</param>
		/// <param name="localPath">local path (not null)</param>
		/// <param name="workspaceName">workspace name (not null)</param>		
		public static void Create(ServerPath serverPath, FileSystemPath localPath, string workspaceName)
		{
			Create(serverPath, localPath, workspaceName, p => { });
		}

		/// <summary>
		///		Creates new workspace using workspace definition file.
		/// </summary>
		/// <param name="serverPath">TFS path (not null)</param>
		/// <param name="localPath">local path (not null)</param>
		/// <param name="workspaceName">workspace name (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
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

			ws = Vcs.CreateWorkspace(
				workspaceName, 
				User.Current, 
				String.Format("AnFake: {0} => {1}", serverPath, localPath), 
				mappings.AsTfsMappings());
			Trace.InfoFormat("Workspace '{0}' successfully created for '{1}'.", workspaceName, User.Current);

			GetFiles(ws, mappings, VersionSpec.Latest);
		}		

		/// <summary>
		///		Checkouts branch with default parameters. 
		/// </summary>		
		/// <param name="serverPath">TFS path (not null)</param>
		/// <param name="localPath">local path (not null)</param>
		/// <param name="workspaceName">workspace name (not null)</param>
		/// <seealso cref="Checkout(AnFake.Plugins.Tfs2012.ServerPath,AnFake.Core.FileSystemPath,string,Action{Params})"/>
		public static void Checkout(ServerPath serverPath, FileSystemPath localPath, string workspaceName)
		{
			Checkout(serverPath, localPath, workspaceName, p => { });
		}

		/// <summary>
		///		Checkouts branch.
		/// </summary>
		/// <remarks>
		///		<para>First method lookups workspace definition file at specified server path. If file not found then exception is thrown.</para>
		///		<para>Then method creates new workspace with given name and mappings generated by workspace definition file.</para>
		///		<para>Finally method gets items of created workspace.</para>
		/// 
		///		<para>IMPORTANT! Method uses Params.VersionSpec so it is possible to obtain any brunch revision with appropriate mappings.</para>
		/// </remarks>
		/// <param name="serverPath">TFS path (not null)</param>
		/// <param name="localPath">local path (not null)</param>
		/// <param name="workspaceName">workspace name (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
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

			var versionSpec = ParseVersionSpec(parameters);

			EnsureWorkspaceFile(parameters);
			
			var wsPath = serverPath / parameters.WorkspaceFile;

			Trace.InfoFormat(
				"TfsWorkspace.Checkout\n ServerPath: {4} {0}\n LocalPath: {1}\n Workspace: {2} (from '{3}')",
				serverPath, localPath, workspaceName, parameters.WorkspaceFile, versionSpec.DisplayString);

			var wsDesc = GetTextContent(wsPath, versionSpec);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			TraceMappings(mappings);

			ws = Vcs.CreateWorkspace(
				workspaceName, 
				User.Current, 
				String.Format("AnFake: {0} => {1}", serverPath, localPath), 
				mappings.AsTfsMappings());
			Trace.InfoFormat("Workspace '{0}' successfully created for '{1}'.", workspaceName, User.Current);

			GetFiles(ws, mappings, versionSpec);
		}

		/// <summary>
		///		Gets workspace items with default parameters.
		/// </summary>
		/// <param name="localPath">local path (not null)</param>
		/// <seealso cref="Get(AnFake.Core.FileSystemPath, Action{Params})"/>
		public static void Get(FileSystemPath localPath)
		{
			Get(localPath, p => { });
		}

		/// <summary>
		///		Gets workspace items.
		/// </summary>
		/// <remarks>
		///		<para>First method locates workspace definition file and obtains workspace related to specified local path.</para>
		///		<para>
		///		Then method updates workspace definition file from source control (probably with auto-merge). 
		///		If some conflicts over here then exception is thrown and user should resolve conflicts in usual way, e.g. in Visual Studio.
		///		If there is no conflict then workspace mappings are updated according to merged definition file.
		///		</para>
		///		<para>Finally method gets items of updated workspace.</para>
		/// 
		///		<para>IMPORTANT! Method uses Params.VersionSpec so it is possible to obtain any revision with appropriate mappings.</para>
		/// </remarks>
		/// <param name="localPath">local path (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
		public static void Get(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new ArgumentException("TfsWorkspace.Get(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new ArgumentException("TfsWorkspace.Get(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			var versionSpec = ParseVersionSpec(parameters);

			EnsureWorkspaceFile(parameters);

			var wsFile = LocateWorkspaceFile(localPath, parameters.WorkspaceFile);
			localPath = wsFile.Folder;

			Trace.InfoFormat("TfsWorkspace.Get: {0} {1}", versionSpec.DisplayString, localPath);

			var ws = Impl.GetWorkspace(wsFile.Path);
			var wsPath = ws.GetServerItemForLocalItem(wsFile.Path.Full).AsServerPath();

			Trace.DebugFormat("Getting workspace: {0} {1} => {2}", versionSpec.DisplayString, wsPath, ws.Name);

			var status = ws.Get(
				new GetRequest(wsPath.Full, RecursionType.None, versionSpec), 
				GetOptions.None);

			if (status.NumConflicts > 0)
				throw new InvalidConfigurationException(
					String.Format(
						"There are conflicts in workspace definition file '{0}'.\n" +
						"Resolve conflicts in Visual Studio and re-run command.",
						wsFile));

			var wsDesc = Text.ReadFrom(wsFile);
			var mappings = VcsMappings.Parse(wsDesc, wsPath.Parent.Full, localPath.Full);

			TraceMappings(mappings);

			ws.Update(ws.Name, ws.Comment, mappings.AsTfsMappings());

			Trace.InfoFormat("Workspace '{0}' successfully updated.", ws.Name);

			GetFiles(ws, mappings, versionSpec);
		}		

		/// <summary>
		///		Saves workspace to local workspace definition file.
		/// </summary>
		/// <param name="localPath">local workspace root (not null)</param>
		public static void SaveLocal(FileSystemPath localPath)
		{
			SaveLocal(localPath, p => { });
		}

		/// <summary>
		///		Saves workspace to local workspace definition file.
		/// </summary>
		/// <param name="localPath">local workspace root (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
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

		/// <summary>
		///		Pends addition.
		/// </summary>
		/// <param name="files">files to be added (not null)</param>
		public static void PendAdd(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("TfsWorkspace.PendAdd(files): files must not be null");

			var filePathes = files
				.Select(x => x.Path)
				.ToArray();

			var ws = Impl.GetWorkspace(filePathes[0]);
			
			Trace.InfoFormat("TfsWorkspace.PendAdd: {{{0}}}", files.ToFormattedString());

			foreach (var path in filePathes)
			{
				Trace.DebugFormat("  {0}", path);
			}

			var pended = ws.PendAdd(
				filePathes.Select(x => x.Full).ToArray());

			Trace.InfoFormat("{0} file(s) pended for add.", pended);
		}

		/// <summary>
		///		Undoes changes.
		/// </summary>
		/// <param name="files">files to be undone (not null)</param>
		public static void Undo(IEnumerable<FileItem> files)
		{
			if (files == null)
				throw new ArgumentException("TfsWorkspace.Undo(files): files must not be null");

			var filesArray = files.ToArray();

			var ws = Impl.GetWorkspace(filesArray[0].Path);
			
			Trace.InfoFormat("TfsWorkspace.Undo: {{{0}}}", files.ToFormattedString());

			foreach (var file in filesArray)
			{
				Trace.DebugFormat("  {0}", file);
			}

			var reverted = ws.Undo(
				filesArray
					.Select(x => new ItemSpec(x.Path.Full, RecursionType.None))
					.ToArray());

			Trace.InfoFormat("{0} file(s) reverted.", reverted);
		}

		/// <summary>
		///		Undoes all changes in specified folders recursively.
		/// </summary>
		/// <param name="folders">folders to be undone (not null)</param>
		public static void Undo(IEnumerable<FolderItem> folders)
		{
			if (folders == null)
				throw new ArgumentException("TfsWorkspace.Undo(folders): folders must not be null");

			var foldersArray = folders.ToArray();

			var ws = Impl.GetWorkspace(foldersArray[0].Path);

			Trace.InfoFormat("TfsWorkspace.Undo: {{{0}}}", folders.ToFormattedString());

			foreach (var folder in foldersArray)
			{
				Trace.DebugFormat("  {0}", folder);
			}

			var reverted = ws.Undo(
				foldersArray
					.Select(x => new ItemSpec(x.Path.Full, RecursionType.Full))
					.ToArray());

			Trace.InfoFormat("{0} item(s) reverted.", reverted);
		}

		// ReSharper disable once UnusedParameter.Local
		private static void EnsureWorkspaceFile(Params parameters)
		{
			if (String.IsNullOrEmpty(parameters.WorkspaceFile))
				throw new ArgumentException("TfsWorkspace.Params.WorkspaceFile must not be null or empty");
		}

		private static VersionSpec ParseVersionSpec(Params parameters)
		{
			if (String.IsNullOrEmpty(parameters.VersionSpec))
				throw new ArgumentException("TfsWorkspace.Params.VersionSpec must not be null or empty");

			return VersionSpec.ParseSingleSpec(parameters.VersionSpec, User.Current);
		}

		private static void TraceMappings(IEnumerable<ExtendedMapping> mappings)
		{
			Trace.DebugFormat(
				"Mappings:\n  {0}", 
				String.Join("\n  ", 
					mappings.Select(
						m => String.Format("{0} => {1}", 
							m.VersionSpec != null ? m.VersionSpec.Format(m.ServerItem) : m.ServerItem, 
							m.IsCloaked ? "(cloacked)" : m.LocalItem))));
		}

		private static string GetTextContent(ServerPath serverPath, VersionSpec versionSpec)
		{
			var item = Vcs.GetItem(serverPath.Full, versionSpec);
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

		private static void GetFiles(Workspace ws, ExtendedMapping[] mappings, VersionSpec versionSpec)
		{
			Trace.InfoFormat("Getting files @ {0}...", versionSpec.DisplayString);
			
			var status = ws.Get(versionSpec, GetOptions.None);

			foreach (var failure in status.GetFailures())
			{
				Trace.Warn(failure.GetFormattedMessage());
			}

			var numConflicts = status.NumConflicts;
			var numFiles = status.NumFiles;
			var numFailures = status.NumFailures;

			foreach (var mapping in mappings.Where(x => x.VersionSpec != null && x.VersionSpec != versionSpec))
			{
				Trace.InfoFormat(">> Getting '{0}' @ {1}...", mapping.ServerItem, mapping.VersionSpec.DisplayString);

				status = ws.Get(
					new GetRequest(mapping.ServerItem, mapping.Depth, mapping.VersionSpec),
					GetOptions.None);

				foreach (var failure in status.GetFailures())
				{
					Trace.Warn(failure.GetFormattedMessage());
				}

				numConflicts += status.NumConflicts;
				numFiles += status.NumFiles;
				numFailures += status.NumFailures;
			}

			if (numConflicts > 0)
			{
				Trace.WarnFormat("There are {0} conflicts detected.", numConflicts);
			}

			Trace.InfoFormat(
				"{0}: {1} file(s) updated. {2} warning(s), {3} conflict(s)", 
				versionSpec.DisplayString, numFiles, numFailures, numConflicts);
		}
	}
}