namespace AnFake.Core.Integration
{
	public interface IVersionControl
	{
		string CurrentChangesetId { get; }

		IChangeset GetChangeset(string changesetId);
	}
}