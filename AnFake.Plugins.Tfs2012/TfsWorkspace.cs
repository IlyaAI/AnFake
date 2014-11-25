using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
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
				NameGenerationFormat = "{0}@{1}";
				MaxGeneratedNames = 10;
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

		private static VersionControlServer _vcs;

		private static VersionControlServer Vcs
		{
			get { return _vcs ?? (_vcs = Plugin.Get<TfsPlugin>().TeamProjectCollection.GetService<VersionControlServer>()); }
		}		

		public static string GenerateUniqueName(string workspaceName)
		{
			return GenerateUniqueName(workspaceName, p => { });
		}

		public static string GenerateUniqueName(string workspaceName, Action<Params> setParams)
		{
			if (String.IsNullOrEmpty(workspaceName))
				throw new AnFakeArgumentException("TfsWorkspace.GenerateUniqueName(workspaceName, setParams): workspaceName must not be null or empty");

			if (setParams == null)
				throw new AnFakeArgumentException("TfsWorkspace.GenerateUniqueName(workspaceName, setParams): setParams must not be null");

			var usedNames = Vcs.QueryWorkspaces(null, GetCurrentUser(), null)				
				.ToLookup(x => x.Name);

			if (!usedNames.Contains(workspaceName))
				return workspaceName;

			var parameters = Defaults.Clone();
			setParams(parameters);

			var index = 2;
			while (index <= parameters.MaxGeneratedNames)
			{
				var uniqueName = String.Format(parameters.NameGenerationFormat, workspaceName, index);
				if (!usedNames.Contains(uniqueName))
					return uniqueName;

				index++;
			}

			throw new InvalidConfigurationException(String.Format("TfsWorkspace.GenerateUniqueName: too many ({0}+) workspaces based on the same name '{1}'", index, workspaceName));
		}

		public static Api.IToolExecutionResult Checkout(ServerPath serverPath, FileSystemPath localPath, string workspaceName)
		{
			return Checkout(serverPath, localPath, workspaceName, p => { });
		}

		public static Api.IToolExecutionResult Checkout(ServerPath serverPath, FileSystemPath localPath, string workspaceName, Action<Params> setParams)
		{
			if (serverPath == null)
				throw new AnFakeArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): serverPath must not be null");
			if (!serverPath.IsRooted)
				throw new AnFakeArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): serverPath must be an absolute path");

			if (localPath == null)
				throw new AnFakeArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): localPath must not be null");
			if (!localPath.AsFolder().IsEmpty())
				throw new InvalidConfigurationException(String.Format("TfsWorkspace.Checkout intended for initial downloading only but target directory '{0}' is not empty.", localPath));

			if (String.IsNullOrEmpty(workspaceName))
				throw new AnFakeArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName[, setParams]): workspaceName must not be null or empty");

			if (setParams == null)
				throw new AnFakeArgumentException("TfsWorkspace.Checkout(serverPath, localPath, workspaceName, setParams): setParams must not be null");

			var ws = FindWorkspace(workspaceName);			
			if (ws != null)
				throw new InvalidConfigurationException(String.Format("TfsWorkspace.Checkout intended for initial downloading only but workspace '{0}' already exists.", workspaceName));

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);
			
			var wsPath = serverPath / parameters.WorkspaceFile;

			Logger.DebugFormat("TfsWorkspace.Checkout\n ServerPath: {0}\n LocalPath: {1}\n Workspace: {2} (from '{3}')",
				serverPath, localPath, workspaceName, parameters.WorkspaceFile);

			var wsDesc = GetTextContent(wsPath);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			LogMappings(mappings);

			ws = Vcs.CreateWorkspace(workspaceName, GetCurrentUser(), String.Format("AnFake: {0} => {1}", serverPath, localPath), mappings);
			Logger.DebugFormat("Workspace '{0}' successfully created for '{1}'.", workspaceName, GetCurrentUser());

			var result = UpdateFiles(ws);

			return result;
		}

		public static Api.IToolExecutionResult Sync(FileSystemPath localPath)
		{
			return Sync(localPath, p => { });
		}

		public static Api.IToolExecutionResult Sync(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new AnFakeArgumentException("TfsWorkspace.Sync(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new AnFakeArgumentException("TfsWorkspace.Sync(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);

			var wsFile = LocateWorkspaceFile(localPath, parameters.WorkspaceFile);
			localPath = wsFile.Folder;

			Logger.DebugFormat("TfsWorkspace.Sync: {0}", localPath);

			var ws = Vcs.GetWorkspace(wsFile.Path.Full);
			var wsPath = ws.GetServerItemForLocalItem(wsFile.Path.Full).AsServerPath();

			Logger.DebugFormat("Synchronizing workspace: {0} => {1}", wsPath, ws.Name);
			var wsDesc = GetTextContent(wsPath);
			var mappings = VcsMappings.Parse(wsDesc, wsPath.Parent.Full, localPath.Full);

			LogMappings(mappings);

			ws.Update(ws.Name, ws.Comment, mappings);
			Logger.DebugFormat("Workspace '{0}' successfully updated.", ws.Name);

			var result = UpdateFiles(ws);

			return result;
		}

		public static Api.IToolExecutionResult SyncLocal(FileSystemPath localPath)
		{
			return SyncLocal(localPath, p => { });
		}

		public static Api.IToolExecutionResult SyncLocal(FileSystemPath localPath, Action<Params> setParams)
		{
			if (localPath == null)
				throw new AnFakeArgumentException("TfsWorkspace.SyncLocal(localPath[, setParams]): localPath must not be null");
			if (setParams == null)
				throw new AnFakeArgumentException("TfsWorkspace.SyncLocal(localPath, setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			EnsureWorkspaceFile(parameters);

			var wsFile = LocateWorkspaceFile(localPath, parameters.WorkspaceFile);
			localPath = wsFile.Folder;			

			Logger.DebugFormat("TfsWorkspace.SyncLocal: {0}", localPath);

			var ws = Vcs.GetWorkspace(wsFile.Path.Full);
			var serverPath = ws.GetServerItemForLocalItem(localPath.Full).AsServerPath();

			Logger.DebugFormat("Synchronizing workspace: {0} => {1}", wsFile, ws.Name);
			var wsDesc = GetTextContent(wsFile);
			var mappings = VcsMappings.Parse(wsDesc, serverPath.Full, localPath.Full);

			LogMappings(mappings);

			ws.Update(ws.Name, ws.Comment, mappings);
			Logger.DebugFormat("Workspace '{0}' successfully updated.", ws.Name);

			var result = UpdateFiles(ws);

			return result;
		}

		public static Api.IToolExecutionResult Update(FileSystemPath localPath, Action<Params> setParams)
		{
			return null;
		}

		private static void EnsureWorkspaceFile(Params parameters)
		{
			if (String.IsNullOrEmpty(parameters.WorkspaceFile))
				throw new AnFakeArgumentException("TfsWorkspace.Params.WorkspaceFile must not be null or empty");
		}

		private static void LogMappings(IEnumerable<WorkingFolder> mappings)
		{
			Logger.Debug("Mappings:");
			foreach (var mapping in mappings)
			{
				Logger.DebugFormat("  {0} => {1}", mapping.ServerItem, mapping.IsCloaked ? "(cloacked)" : mapping.LocalItem);
			}
		}

		private static Workspace FindWorkspace(string workspaceName)
		{
			try
			{
				var ws = Vcs.GetWorkspace(workspaceName, GetCurrentUser());

				if (!ws.IsDeleted && ws.MappingsAvailable)
					return ws;
			}
			catch (WorkspaceNotFoundException)
			{
			}

			return null;
		}

		private static string GetCurrentUser()
		{
			var identity = WindowsIdentity.GetCurrent();
			if (identity == null)
				throw new InvalidConfigurationException("TFS plugin requires authenticated user.");

			return identity.Name;
		}

		private static string GetTextContent(ServerPath serverPath)
		{
			/*var item = Vcs.GetItem(serverPath.Full);
			using (var downstream = item.DownloadFile())
			{
				return new StreamReader(downstream).ReadToEnd();
			}*/

			return "$/DLPR_Internals/external/NuGet/2.7.0: .nuget\n" +
				"$/DLPR_Internals/external/FAKE/3.5.4: .FAKE\n" +
				"-$/DLPR_Internals/external/FAKE/3.5.4/FAKE.3.5.4.nupkg";
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

		private static Api.IToolExecutionResult UpdateFiles(Workspace ws)
		{
			Logger.Debug("Updating files...");
			var status = ws.Get();

			var failures = status.GetFailures();
			foreach (var failure in status.GetFailures())
			{
				var msg = failure.GetFormattedMessage();

				Tracer.Warn(msg);
				Logger.Warn(msg);
			}

			return new ToolExecutionResult(0, failures.Length);
		}
	}
}