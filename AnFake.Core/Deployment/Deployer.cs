namespace AnFake.Core.Deployment
{
	public class Deployer
	{
		public virtual void DoStep(UpdateFilesStep step)
		{
		}

		public virtual void DoStep(UpdateFileInplaceStep step)
		{
		}

		public virtual void DoStep(DeleteFileStep step)
		{
		}

		public virtual void DoStep(DeploymentStep step)
		{
		}

		public void Deploy(DeploymentBatch batch)
		{
			foreach (var step in batch.GetDeploymentSteps())
			{
				step.Accept(this);
			}
		}
	}
}