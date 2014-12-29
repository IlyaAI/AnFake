namespace AnFake.Core.Integration
{
	public interface IVersionControl
	{
		int CurrentChangesetId { get; }

		IChangeset GetChangeset(int changesetId);
	}
}