using System;
using AnFake.Plugins.TeamCity.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.TeamCity.Test
{
	[TestClass]
	public class TeamCityClientTest : TeamCityTestSuite
	{
		[TestInitialize]
		public override void Initialize()
		{
			base.Initialize();
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void CheckConnection_should_be_success_with_valid_credentials()
		{
			// arrange
			var client = new TeamCityClient(TcUri, TcUser, TcPassword);

			// act
			client.CheckConnection();			
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void CheckConnection_should_throw_with_invalid_credentials()
		{
			// arrange
			var client = new TeamCityClient(TcUri, "someUser", "no-pass");

			// act & assert
			try
			{
				client.CheckConnection();
				Assert.Fail("Exception is expected.");
			}
			catch (Exception)
			{
				// it's ok
			}
		}		
	}
}
