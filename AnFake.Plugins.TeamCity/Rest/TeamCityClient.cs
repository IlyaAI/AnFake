using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AnFake.Core;

namespace AnFake.Plugins.TeamCity.Rest
{
	internal sealed class TeamCityClient : IDisposable
	{
		private readonly HttpClient _httpClient;

		private TeamCityClient(Uri uri, string authType, ICredentials credentials)
		{
			var handler = new HttpClientHandler { Credentials = credentials, PreAuthenticate = true };
			
			_httpClient = new HttpClient(handler) { BaseAddress = new Uri(uri, "app/rest/") };
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.5));
			_httpClient.DefaultRequestHeaders.ExpectContinue = false;

			try
			{
				Authenticate(authType);
			}
			catch (Exception)
			{
				_httpClient.Dispose();
				throw;
			}
		}

		public static TeamCityClient BasicAuth(Uri uri, string user, string password)
		{
			return new TeamCityClient(uri, "httpAuth", new NetworkCredential(user, password));
		}

		public static TeamCityClient NtlmAuth(Uri uri)
		{
			return new TeamCityClient(uri, "ntlmAuth", CredentialCache.DefaultNetworkCredentials);
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}

		private void Authenticate(string authType)
		{
			using (var task = _httpClient.GetAsync('/' + authType + '/'))
			{
				task.Wait();
				task.Result.EnsureSuccessStatusCode();
			}
		
            var info = RestGet<ServerVersion>("server");
            if (String.IsNullOrWhiteSpace(info.Version))
				throw new NotSupportedException("Connection to TeamCity established but API version isn't recognized.");
        }

		public Build FindLastGoodBuild(string configurationName)
		{
			var locatorBuilder = new LocatorBuilder();
			locatorBuilder
				.Append("buildType", configurationName)
				.Append("status", "SUCCESS")
				.Append("state", "finished")
				.Append("count", "1");

			var builds = RestTryGet<BuildsList>("builds/" + locatorBuilder.ToQuery());

			return builds != null && builds.Items.Count > 0
				? builds.Items[0]
				: null;
		}

		public Build FindLastTaggedBuild(string configurationName, Tag[] tags)
		{
			var joinedTagNames = String.Join(",", tags.Select(x => x.Name));

			var locatorBuilder = new LocatorBuilder();
			locatorBuilder
				.Append("buildType", configurationName)
				.Append("state", "finished")
				.Append("tags", joinedTagNames)
				.Append("count", "1");

			var builds = RestTryGet<BuildsList>("builds/" + locatorBuilder.ToQuery());

			return builds != null && builds.Items.Count > 0 
				? builds.Items[0]
				: null;
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

		public void AddBuildTag(Uri buildHref, Tag tag)
		{
			RestPost(buildHref + "/tags/", new TagsList(tag));
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

		private T RestTryGet<T>(string subUri)
			where T : class, new()
		{
			using (var task = _httpClient.GetAsync(subUri))
			{
				task.Wait();

				if (task.Result.StatusCode == HttpStatusCode.NotFound)
					return null;

				task.Result.EnsureSuccessStatusCode();

				using (var subTask = task.Result.Content.ReadAsStringAsync())
				{
					subTask.Wait();

					return Json.ReadAs<T>(subTask.Result);
				}
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

		private void RestPost<T>(string subUri, T entity)
		{
			var content = new StringContent(Json.Write(entity), Encoding.UTF8, "application/json");

			using (var task = _httpClient.PostAsync(subUri, content))
			{
				task.Wait();
				task.Result.EnsureSuccessStatusCode();
			}
		}
	}
}
