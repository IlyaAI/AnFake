namespace AnFake.Core.Deployment
{
	public sealed class DeleteFileStep : DeploymentStep
	{
		public readonly string Destination;

		public DeleteFileStep(string destination)
		{
			Destination = destination;
		}

		public override void Accept(Deployer deployer)
		{
			deployer.DoStep(this);
		}
	}
}