using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AnFake.Core;
using AnFake.Core.Exceptions;

namespace AnFake.Plugins.TeamCity.Rest
{
	internal sealed class TeamCityClient : IDisposable
	{
		private sealed class LocatorBuilder
		{
			private readonly StringBuilder _locator = new StringBuilder();

			public LocatorBuilder Append(string name, string value)
			{
				if (_locator.Length > 0)
				{
					_locator.Append(',');
				}

				_locator.Append(name).Append(':');

				if (value.IndexOfAny(new[] {':', ','}) >= 0)
				{
					_locator.Append('(').Append(value).Append(')');
				}
				else
				{
					_locator.Append(value);
				}
				
				return this;
			}

			public string ToQuery()
			{
				return "?locator=" + WebUtility.UrlEncode(_locator.ToString());
			}

			/*public string ToPath()
			{
				return WebUtility.UrlEncode(_locator.ToString());
			}*/
		}
		
		private readonly HttpClient _httpClient;

		public TeamCityClient(Uri uri, string user, string password)
		{
			var handler = new HttpClientHandler
			{
				Credentials = new NetworkCredential(user, password),
				PreAuthenticate = true
			};

			_httpClient = new HttpClient(handler) {BaseAddress = new Uri(uri, "httpAuth/app/rest/")};
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_httpClient.DefaultRequestHeaders.ExpectContinue = false;			
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}

		public void CheckConnection()
        {            
            var info = RestGet<ServerVersion>("server");

            if (String.IsNullOrWhiteSpace(info.Version))
				throw new NotSupportedException("Connection to TeamCity established but API version isn't recognized.");
        }

		public Build GetLastGoodBuild(string configurationName)
		{
			var locatorBuilder = new LocatorBuilder();
			locatorBuilder
				.Append("buildType", configurationName)
				.Append("status", "SUCCESS")
				.Append("count", "1");

			var builds = RestGet<BuildsList>("builds/" + locatorBuilder.ToQuery());

			if (builds.Items.Count == 0)
				throw new InvalidConfigurationException(String.Format("There is no good build of '{0}'.", configurationName));

			return builds.Items[0];
		}

		public Build GetLastTaggedBuild(string configurationName, Tag[] tags)
		{
			var joinedTagNames = String.Join(",", tags.Select(x => x.Name));

			var locatorBuilder = new LocatorBuilder();
			locatorBuilder
				.Append("buildType", configurationName)				
				.Append("tags", joinedTagNames)
				.Append("count", "1");

			var builds = RestGet<BuildsList>("builds/" + locatorBuilder.ToQuery());

			if (builds.Items.Count == 0)
				throw new InvalidConfigurationException(String.Format("There is no build of '{0}' tagged as '{1}'.", configurationName, String.Join(",", joinedTagNames)));

			return builds.Items[0];
		}

		public Build GetBuildDetails(Uri buildHref)
		{
			return RestGet<Build>(buildHref.ToString());
		}

		public List<Property> GetBuildProps(Uri buildHref)
		{
			return RestGet<PropertiesList>(buildHref + "/resulting-properties/").Items;
		}

		public List<Tag> GetBuildTags(Uri buildHref)
		{
			return RestGet<TagsList>(buildHref + "/tags/").Items;
		}

		public void SetBuildTags(Uri buildHref, List<Tag> tags)
		{			
			RestPut(buildHref + "/tags/", new TagsList(tags));
		}

		public void GetArchivedArtifacts(Uri buildHref, string path, string pattern, string zipPath)
		{
			var artifactsUri = new StringBuilder();
			artifactsUri.Append(buildHref).Append("/artifacts/archived/");

			if (!String.IsNullOrEmpty(path))
			{
				artifactsUri.Append(path);
			}

			if (!String.IsNullOrEmpty(pattern))
			{
				artifactsUri.Append(new LocatorBuilder().Append("pattern", pattern).ToQuery());
			}

			using (var task = _httpClient.GetStreamAsync(artifactsUri.ToString()))
			{
				task.Wait();

				try
				{
					using (var zip = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
					{
						task.Result.CopyTo(zip);
					}
				}
				finally
				{
					task.Result.Close();
				}
			}			
		}

		private T RestGet<T>(string subUri)
			where T : class, new()
		{
			using (var task = _httpClient.GetStringAsync(subUri))
			{
				task.Wait();

				return Json.ReadAs<T>(task.Result);
			}			
		}
		
		private void RestPut<T>(string subUri, T entity)
		{
			var content = new StringContent(Json.Write(entity), Encoding.UTF8, "application/json");

			using (var task = _httpClient.PutAsync(subUri, content))
			{
				task.Wait();
				task.Result.EnsureSuccessStatusCode();
			}			
		}
	}
}
