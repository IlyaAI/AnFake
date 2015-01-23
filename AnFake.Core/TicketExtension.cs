using System;
using AnFake.Core.Integration.Tracking;

namespace AnFake.Core
{
	public static class TicketExtension
	{
		public static string GetStringField(this ITicket ticket, string name)
		{
			return (string) ticket.GetField(name);
		}

		public static int GetIntField(this ITicket ticket, string name)
		{
			return (int) ticket.GetField(name);
		}

		public static bool GetBoolField(this ITicket ticket, string name)
		{
			return (bool) ticket.GetField(name);
		}

		public static DateTime GetDateField(this ITicket ticket, string name)
		{
			return (DateTime) ticket.GetField(name);
		}
	}
}