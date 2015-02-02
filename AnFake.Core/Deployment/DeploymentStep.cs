namespace AnFake.Core.Deployment
{
	public abstract class DeploymentStep
	{
		public abstract void Accept(Deployer deployer);
	}
}