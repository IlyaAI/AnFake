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

		public static string GetStringField(this ITicket ticket, string name, string defaultValue)
		{
			return (string)ticket.GetField(name, defaultValue);
		}

		public static int GetIntField(this ITicket ticket, string name)
		{
			return (int) ticket.GetField(name);
		}

		public static int GetIntField(this ITicket ticket, string name, int defaultValue)
		{
			return (int)ticket.GetField(name, defaultValue);
		}

		public static bool GetBoolField(this ITicket ticket, string name)
		{
			return (bool) ticket.GetField(name);
		}

		public static bool GetBoolField(this ITicket ticket, string name, bool defaultValue)
		{
			return (bool)ticket.GetField(name, defaultValue);
		}

		public static DateTime GetDateField(this ITicket ticket, string name)
		{
			return (DateTime) ticket.GetField(name);
		}

		public static DateTime GetDateField(this ITicket ticket, string name, DateTime defaultValue)
		{
			return (DateTime)ticket.GetField(name, defaultValue);
		}

		public static void SetStringField(this ITicket ticket, string name, string value)
		{
			ticket.SetField(name, value);
		}

		public static void SetIntField(this ITicket ticket, string name, int value)
		{
			ticket.SetField(name, value);
		}

		public static void SetBoolField(this ITicket ticket, string name, bool value)
		{
			ticket.SetField(name, value);
		}

		public static void SetDateField(this ITicket ticket, string name, DateTime value)
		{
			ticket.SetField(name, value);
		}
	}
}