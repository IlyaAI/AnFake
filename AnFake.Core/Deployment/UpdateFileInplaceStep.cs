using System;

namespace AnFake.Core.Deployment
{
	public sealed class UpdateFileInplaceStep : DeploymentStep
	{
		public readonly string Destination;
		public readonly Action<FileItem> Updater;

		public UpdateFileInplaceStep(string destination, Action<FileItem> updater)
		{
			Destination = destination;
			Updater = updater;
		}

		public override void Accept(Deployer deployer)
		{
			deployer.DoStep(this);
		}
	}
}