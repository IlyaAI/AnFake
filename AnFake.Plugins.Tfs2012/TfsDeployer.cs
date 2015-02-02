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

			public override void DoStep(UpdateFilesStep updateFiles)
			{
				var dstPath = DeploymentFolder/updateFiles.Destination;
				Files.Copy(updateFiles.Files, dstPath, true);
				
				foreach (var file in updateFiles.Files)
				{
					Workspace.PendAdd((dstPath / file.RelPath).Full);
				}
			}

			public override void DoStep(DeleteFileStep deleteFile)
			{
				Workspace.PendDelete((DeploymentFolder / deleteFile.Destination).Full);
			}
		}

		public sealed class Params
		{
			public Uri TfsUri;

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

			Trace.InfoFormat("TfsDeployer.Deploy: '{0}' to '{1}'...", batch.Description, serverPath);

			var timestamp = String.Format("{0:yyyyMMdd.HHmmss.ff}", DateTime.Now);
			var deploymentFolder = ("[Temp]/AnFakeDeployment." + timestamp).AsPath();
			Folders.Create(deploymentFolder);

			var workspace = (Workspace) null;
			try
			{
				var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(parameters.TfsUri);
				var vcs = teamProjectCollection.GetService<VersionControlServer>();

				var wsName = String.Format("Deployment-{0}", timestamp);
				Trace.InfoFormat("Creating temporary workspace '{0}'...", wsName);

				workspace = vcs.CreateWorkspace(
					wsName,
					User.Current,
					batch.Description,
					new[] { new WorkingFolder(serverPath.Full, deploymentFolder.Full) });

				Trace.Info("Downloading files...");

				var status = workspace.Get();
				if (status.GetFailures().Length > 0)
					throw new InvalidConfigurationException("");

				Trace.Info("Deploying batch...");

				new TfsDeployerImpl(deploymentFolder, workspace).Deploy(batch);

				var pendingSets = workspace.QueryPendingSets(
					new[] { deploymentFolder.Full },
					RecursionType.Full,
					workspace.Name,
					workspace.OwnerName,
					false);

				var changes = pendingSets
					.SelectMany(x => x.PendingChanges)
					.ToArray();

				Trace.Info("Pending changes ready to check-in:");
				foreach (var change in changes)
				{
					Trace.InfoFormat("  [{0,-6}] {1}", change.ChangeTypeName, change.FileName);
				}

				Trace.Info("Checking in...");
				workspace.CheckIn(changes, batch.Description);
			}
			finally
			{
				Trace.Info("Cleaning up...");

				SafeOp.Try(Folders.Delete, deploymentFolder);

				if (workspace != null)
				{
					SafeOp.Try(() => workspace.Delete());
				}
			}						
		}
	}
}