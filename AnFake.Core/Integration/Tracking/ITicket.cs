using System;

namespace AnFake.Core.Integration.Tracking
{
	public interface ITicket
	{
		Uri Uri { get; }
		
		string Id { get; }
		
		string Type { get; }
		
		string Summary { get; }
		
		string State { get; }

		string Reason { get; }

		object GetField(string name);

		object GetField(string name, object defaultValue);

		void SetField(string name, object value);

		void Save();
	}
}
