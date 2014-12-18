namespace AnFake.Api
{
	/// <summary>
	///     Represents trace message counter.
	/// </summary>
	/// <remarks>
	///     Counts important (Summary, Warning, Error) messages produced by some operation.
	/// </remarks>
	public sealed class TraceMessageCounter
	{		
		private int _summariesCount;
		private int _warningsCount;
		private int _errorsCount;

		public int SummariesCount
		{
			get { return _summariesCount; }
		}

		public int WarningsCount
		{
			get { return _warningsCount; }
		}

		public int ErrorsCount
		{
			get { return _errorsCount; }
		}

		public void OnMessage(object sender, TraceMessage msg)
		{
			switch (msg.Level)
			{
				case TraceMessageLevel.Summary:			
					_summariesCount++;
					break;
				case TraceMessageLevel.Warning:					
					_warningsCount++;
					break;
				case TraceMessageLevel.Error:					
					_errorsCount++;
					break;
			}
		}		
	}
}