using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Core.Deployment
{
	public sealed class DeploymentBatch
	{
		private readonly List<DeploymentStep> _deploymentSteps = new List<DeploymentStep>();
		private readonly string _description;

		public DeploymentBatch(string description)
		{
			_description = description;
		}

		public string Description
		{
			get { return _description; }
		}

		public void UpdateFileInplace(string destination, Action<FileItem> updater)
		{
			_deploymentSteps.Add(
				new UpdateFileInplaceStep(destination, updater));
		}

		public void UpdateFiles(IEnumerable<FileItem> files, string destination)
		{
			_deploymentSteps.Add(
				new UpdateFilesStep(files.ToArray(), destination));
		}

		public void DeleteFile(string destination)
		{
			_deploymentSteps.Add(
				new DeleteFileStep(destination));
		}

		internal IEnumerable<DeploymentStep> GetDeploymentSteps()
		{
			return _deploymentSteps;
		}
	}
}