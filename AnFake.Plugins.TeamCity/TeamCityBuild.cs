using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration.Builds;

namespace AnFake.Plugins.TeamCity
{
	internal sealed class TeamCityBuild : IBuild
	{
		private readonly Rest.TeamCityClient _apiClient;
		private Rest.Build _build;
		private bool _detailed;
		private List<Rest.Tag> _tags;
		private Dictionary<string, string> _props;

		public TeamCityBuild(Rest.TeamCityClient apiClient, Rest.Build build)
		{
			_apiClient = apiClient;
			_build = build;
			_detailed = false;
		}

		public Uri Uri
		{
			get { return _build.WebUrl; }
		}

		public Uri NativeUri
		{
			get { return _build.Href; }
		}

		public string ConfigurationName
		{
			get { return _build.BuildTypeId; }
		}

		public int ChangesetId
		{
			get
			{
				int id;
				if (!Int32.TryParse(ChangesetHash, out id))
					throw new InvalidConfigurationException("Integer changeset id not supported by current VCS.");

				return id;
			}
		}

		public string ChangesetHash
		{
			get
			{
				LoadDetails();

				if (_build.Revisions.Items.Count == 0)
					throw new InvalidOperationException("Inconsistency: build doesn't have associated VCS roots.");

				return _build.Revisions.Items[0].Version;
			}			
		}

		public int Counter
		{
			get
			{
				LoadProps();

				string value;
				if (!_props.TryGetValue("build.counter", out value))
					throw new InvalidOperationException("Inconsistency: build doesn't have 'build.counter' property.");

				return Int32.Parse(value);
			}
		}

		public DateTime Started
		{
			get
			{
				LoadDetails();

				return DateTime.ParseExact(_build.StartDate, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture);
			}
		}

		public DateTime Finished
		{
			get
			{
				LoadDetails();

				return DateTime.ParseExact(_build.FinishDate, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture);
			}
		}

		public string[] Tags
		{
			get
			{
				LoadTags();

				return _tags.Select(t => t.Name).ToArray();
			}
		}
		
		public void AddTag(string tag)
		{
			if (String.IsNullOrEmpty(tag))
				throw new ArgumentException("Build.AddTag(tag): tag must not be null or empty");

			LoadTags();

			_tags.Add(new Rest.Tag(tag));

			SaveTags();
		}

		public void RemoveTag(string tag)
		{
			if (String.IsNullOrEmpty(tag))
				throw new ArgumentException("Build.RemoveTag(tag): tag must not be null or empty");

			LoadTags();

			if (_tags.RemoveAll(t => t.Name == tag) > 0)
			{
				SaveTags();
			}			
		}

		public bool HasProp(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Build.HasProp(name): name must not be null or empty");

			LoadProps();

			return _props.ContainsKey(name);
		}

		public string GetProp(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("Build.GetProp(name): name must not be null or empty");

			string value;
			if (!_props.TryGetValue(name, out value) || String.IsNullOrEmpty(value))
				throw new InvalidConfigurationException(String.Format("Build '{0}' doesn't contain '{1}' property.", _build.WebUrl, name));

			return value;			
		}

		public void DownloadArtifacts(string artifactsPath, FileSystemPath dstPath, string pattern, bool overwrite)
		{
			if (artifactsPath == null)
				throw new ArgumentException("Build.DownloadArtifacts(artifactsPath, dstPath[, pattern[, overwrite]]): artifactsPath must not be null");

			if (dstPath == null)
				throw new ArgumentException("Build.DownloadArtifacts(artifactsPath, dstPath[, pattern[, overwrite]]): dstPath must not be null");

			var zip = (MyBuild.Current.LocalTemp/"artifacts".MakeUnique(".zip")).AsFile();
			zip.EnsurePath();

			Trace.InfoFormat("Downloading zipped build #{0} artifacts '{1}/{2}'...", _build.Number, artifactsPath, String.IsNullOrEmpty(pattern) ? "**" : pattern);
			_apiClient.GetArchivedArtifacts(_build.Href, artifactsPath, pattern, zip.Path.Full);

			Trace.Info("Unpacking artifacts...");
			Zip.Unpack(zip.Path, dstPath, p => p.OverwriteMode = overwrite ? Zip.OverwriteMode.Overwrite : Zip.OverwriteMode.None);
		}

		private void LoadDetails()
		{
			if (_detailed)
				return;

			_build = _apiClient.GetBuildDetails(_build.Href);
			_detailed = true;
		}

		private void LoadTags()
		{
			if (_tags != null)
				return;

			_tags = _apiClient.GetBuildTags(_build.Href);
		}

		private void SaveTags()
		{
			if (_tags == null)
				return;

			_apiClient.SetBuildTags(_build.Href, _tags);
		}

		private void LoadProps()
		{
			if (_props != null)
				return;

			_props = new Dictionary<string, string>();
			
			foreach (var prop in _apiClient.GetBuildProps(_build.Href))
			{
				_props.Add(prop.Name, prop.Value);
			}			
		}
	}
}