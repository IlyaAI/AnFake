using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Deployment;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	public static class TfsDeployer
	{
		private sealed class TfsDeployerImpl : Deployer
		{
			private readonly FileSystemPath DeploymentFolder;
			private readonly Workspace Workspace;

			public TfsDeployerImpl(FileSystemPath deploymentFolder, Workspace workspace)
			{
				DeploymentFolder = deploymentFolder;
				Workspace = workspace;
			}

			public override void DoStep(UpdateFilesStep step)
			{
				var dstPath = DeploymentFolder/step.Destination;
				Files.Copy(step.Files, dstPath, true);
				
				foreach (var file in step.Files)
				{
					Workspace.PendAdd((dstPath / file.RelPath).Full);
				}
			}

			public override void DoStep(UpdateFileInplaceStep step)
			{
				var file = (DeploymentFolder/step.Destination).AsFile();
				step.Updater.Invoke(file);
			}

			public override void DoStep(DeleteFileStep step)
			{
				Workspace.PendDelete((DeploymentFolder / step.Destination).Full);
			}
		}

		public sealed class Params
		{
			public Uri TfsUri;
			public FolderItem LocalFolder;
			public bool DoNotCheckIn;

			internal Params()
			{
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static TfsDeployer()
		{
			Defaults = new Params();
		}
		
		public static void Deploy(DeploymentBatch batch, ServerPath serverPath, Action<Params> setParams)		
		{
			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.TfsUri == null)
				throw new ArgumentException("TfsDeployer.Params.TfsUri must not be null");

			var localFolder = (FolderItem) null;			
			if (parameters.LocalFolder == null)
			{
				localFolder = "[Temp]/AnFake.Deployment".MakeUnique().AsFolder();
				Folders.Create(localFolder);
			}
			else
			{
				localFolder = parameters.LocalFolder;
				Folders.Clean(localFolder);
			}

			var workspaceName = localFolder.Name;

			Trace.InfoFormat("TfsDeployer.Deploy: '{0}' to '{1}'...", batch.Description, serverPath);
			
			var workspace = (Workspace)null;
			var checkinReady = false;
			try
			{
				var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(parameters.TfsUri);
				var vcs = teamProjectCollection.GetService<VersionControlServer>();

				try
				{
					workspace = vcs.GetWorkspace(workspaceName, User.Current);
					workspace.Update(
						workspaceName,						
						workspace.Comment,
						new[] {new WorkingFolder(serverPath.Full, localFolder.Path.Full)});
				}
				catch (WorkspaceNotFoundException)
				{
				}

				if (workspace == null)
				{
					Trace.InfoFormat("Creating workspace '{0}'...", workspaceName);

					workspace = vcs.CreateWorkspace(
						workspaceName,
						User.Current,
						batch.Description,
						new[] {new WorkingFolder(serverPath.Full, localFolder.Path.Full)});

					Trace.Info("Workspace successfuly created.");
				}				

				Trace.Info("Downloading files...");

				var status = workspace.Get();
				if (status.GetFailures().Length > 0)
					throw new InvalidConfigurationException(
						String.Format("Workspace GET operation failed. {0}", status.GetFailures()[0].GetFormattedMessage()));

				Trace.InfoFormat("{0} file(s) downloaded.", status.NumFiles);

				Trace.Info("Deploying batch...");

				new TfsDeployerImpl(localFolder, workspace).Deploy(batch);

				var pendingSets = workspace.QueryPendingSets(
					new[] { localFolder.Path.Full },
					RecursionType.Full,
					workspace.Name,
					workspace.OwnerName,
					false);

				var changes = pendingSets
					.SelectMany(x => x.PendingChanges)
					.ToArray();

				Trace.Info("The following changes has been pended:");
				foreach (var change in changes)
				{
					Trace.InfoFormat("    [{0,-6}] {1}", change.ChangeTypeName, change.FileName);
				}

				checkinReady = true;

				if (parameters.DoNotCheckIn)
				{
					Trace.SummaryFormat("PRE-DEPLOY: check-in pending changes in '{0}' workspace.", workspaceName);
				}
				else
				{
					Trace.Info("Checking in...");
					workspace.CheckIn(changes, batch.Description);
					
					Trace.SummaryFormat("DEPLOY: '{0}' successfuly deployed.", batch.Description);
				}				
			}
			finally
			{
				if (parameters.LocalFolder == null && (!parameters.DoNotCheckIn || !checkinReady))
				{
					Trace.Info("Cleaning up...");

					SafeOp.Try(Folders.Delete, localFolder);

					if (workspace != null)
					{
						Trace.InfoFormat("Deleting workspace '{0}'...", workspace.Name);
						SafeOp.Try(() => workspace.Delete());
					}
				}				

				Trace.Info("Deploy finished.");
			}
		}
	}
}