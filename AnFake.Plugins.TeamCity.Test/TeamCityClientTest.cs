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
		public void BasicAuth_should_be_success_with_valid_credentials()
		{
			// arrange

			// act
			using (var client = TeamCityClient.BasicAuth(TcUri, TcUser, TcPassword))
			{
				// assert
				Assert.IsNotNull(client);
			}
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void BasicAuth_should_throw_with_invalid_credentials()
		{
			// arrange

			try
			{
				// act
				using (TeamCityClient.BasicAuth(TcUri, "someUser", "no-pass"))
				{
					// assert					
					Assert.Fail("Exception is expected.");
				}
			}
			catch (Exception)
			{
				// it's ok				
			}			
		}

		[TestCategory("Integration")]
		[TestMethod]
		public void NtlmAuth_should_be_success_with_valid_credentials()
		{
			// arrange

			// act
			using (var client = TeamCityClient.NtlmAuth(TcUri))
			{
				// assert
				Assert.IsNotNull(client);
			}
		}
	}
}
