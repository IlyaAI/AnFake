using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Core
{
	public static class ReleaseNotes
	{
		public const string CategoryOther = "Other";

		public static Integration.Tracking.ReleaseNotes Create<T>(
			string productName, Version productVersion,
			IEnumerable<T> tickets,
			Action<T, Integration.Tracking.ReleaseNote> noter)
			where T : Integration.Tracking.ITicket
		{
			return Create(productName, productVersion, DateTime.UtcNow, tickets, noter);
		}

		public static Integration.Tracking.ReleaseNotes Create<T>(
			string productName, Version productVersion, DateTime releaseDate,
			IEnumerable<T> tickets,
			Action<T, Integration.Tracking.ReleaseNote> noter)
			where T : Integration.Tracking.ITicket
		{
			var categorizedNotes = tickets
				.Select(ticket =>
				{
					var note = new Integration.Tracking.ReleaseNote(ticket.Id)
					{
						Uri = ticket.Uri,
						Summary = ticket.Summary,
						State = ticket.State,
						Category = CategoryOther
					};
					noter(ticket, note);

					return note;
				})
				.OrderBy(n => n.Ordinal)
				.GroupBy(n => n.Category);

			return new Integration.Tracking.ReleaseNotes
			{
				ProductName = productName,
				ProductVersion = productVersion,
				ReleaseDate = releaseDate.Date,
				CategorizedNotes = categorizedNotes
			};
		}		
	}
}