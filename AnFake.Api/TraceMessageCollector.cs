using System.Collections;
using System.Collections.Generic;

namespace AnFake.Api
{
	/// <summary>
	///     Represents trace message collector.
	/// </summary>
	/// <remarks>
	///     Collects important (Summary, Warning, Error) messages produced by some operation.
	/// </remarks>
	public sealed class TraceMessageCollector : IEnumerable<TraceMessage>
	{
		private readonly IList<TraceMessage> _messages = new List<TraceMessage>();
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
					_messages.Add(msg);
					_summariesCount++;
					break;
				case TraceMessageLevel.Warning:
					_messages.Add(msg);
					_warningsCount++;
					break;
				case TraceMessageLevel.Error:
					_messages.Add(msg);
					_errorsCount++;
					break;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TraceMessage> GetEnumerator()
		{
			return _messages.GetEnumerator();
		}
	}
}