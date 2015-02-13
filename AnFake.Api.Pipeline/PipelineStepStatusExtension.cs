namespace AnFake.Api.Pipeline
{
	public static class PipelineStepStatusExtension
	{
		public static string ToHumanReadable(this PipelineStepStatus status)
		{
			switch (status)
			{
				case PipelineStepStatus.InProgress:
					return "In Progress";

				case PipelineStepStatus.PartiallySucceeded:
					return "Partially Succeeded";

				default:
					return status.ToString();
			}
		}

		public static string ToUpperHumanReadable(this PipelineStepStatus status)
		{
			return ToHumanReadable(status).ToUpperInvariant();
		}
	}
}