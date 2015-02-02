namespace AnFake.Core.Deployment
{
	public sealed class UpdateFilesStep : DeploymentStep
	{
		public readonly FileItem[] Files;
		public readonly string Destination;

		public UpdateFilesStep(FileItem[] files, string destination)
		{
			Files = files;
			Destination = destination;
		}

		public override void Accept(Deployer deployer)
		{
			deployer.DoStep(this);
		}
	}
}