using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace AnFake.Plugins.Tfs2012
{
	/// <summary>
	///		Represents tools related to Team Foundation work items.
	/// </summary>
	public static class TfsWorkItem
	{
		private static TfsPlugin _impl;

		private static TfsPlugin Impl
		{
			get
			{
				if (_impl != null)
					return _impl;

				_impl = Plugin.Get<TfsPlugin>();
				_impl.Disposed += () => _impl = null;

				return _impl;
			}
		}

		/// <summary>
		///		Represents work item provided by Tfs.
		/// </summary>		
		public sealed class WorkItem : Core.Integration.Tracking.ITicket
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private readonly Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem _impl;

			public WorkItem(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem impl)
			{
				_impl = impl;
			}

			public Uri Uri
			{
				get { return new Uri(Impl.Linking.GetArtifactUrl(_impl.Uri.ToString())); }
			}

			public Uri NativeUri
			{
				get { return _impl.Uri; }
			}

			public string Id
			{
				get { return _impl.Id.ToString(CultureInfo.InvariantCulture); }
			}

			public int NativeId
			{
				get { return _impl.Id; }
			}

			public string Type
			{
				get { return _impl.Type.Name; }
			}

			public string Summary
			{
				get { return _impl.Title; }
			}

			public string State
			{
				get { return _impl.State; }
			}

			public string Reason
			{
				get { return _impl.Reason; }
			}

			public object GetField(string name)
			{
				if (!_impl.Fields.Contains(name))
					throw new InvalidConfigurationException(String.Format("WorkItem doesn't have '{0}' field.", name));

				var val = _impl.Fields[name].Value;
				if (val == null)
					throw new InvalidConfigurationException(String.Format("WorkItem field '{0}' is null.", name));

				return val;
			}

			public object GetField(string name, object defaultValue)
			{
				if (!_impl.Fields.Contains(name))
					return defaultValue;

				var val = _impl.Fields[name].Value;
				return val ?? defaultValue;
			}

			public void SetField(string name, object value)
			{
				if (!_impl.Fields.Contains(name))
					throw new InvalidConfigurationException(String.Format("WorkItem doesn't have '{0}' field.", name));

				_impl.Fields[name].Value = value;
			}

			public void PartialOpen()
			{
				_impl.PartialOpen();
			}

			public void Save()
			{
				_impl.Save();
			}

			public void Close()
			{
				_impl.Close();
			}
		}

		public static IEnumerable<WorkItem> ExecQuery(string wiql, params object[] args)
		{
			var store = Impl.TeamProjectCollection.GetService<WorkItemStore>();
			
			var variables = new Dictionary<string, object>();
			ApplyPredefinedVariable(variables);

			for (var i = 0; i+1 < args.Length; i += 2)
			{
				variables[args[i].ToString()] = args[i + 1];
			}
			
			return 
				from Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem item in store.Query(wiql, variables) 
				select new WorkItem(item);
		}

		public static IEnumerable<WorkItem> ExecNamedQuery(string queryPath, params object[] args)
		{
			var store = Impl.TeamProjectCollection.GetService<WorkItemStore>();
			var project = store.Projects[Impl.TeamProject];

			var steps = queryPath.Split('/');
			var queryItem = (QueryItem) project.QueryHierarchy;
			foreach (var step in steps)
			{
				var queryFolder = queryItem as QueryFolder;
				if (queryFolder == null || !queryFolder.Contains(step))
					throw new InvalidConfigurationException(
						String.Format("Query path '{0}' is eighter incorrect or inaccessible for user '{1}'.", queryPath, User.Current));

				queryItem = queryFolder[step];
			}

			var queryDef = queryItem as QueryDefinition;
			if (queryDef == null)
				throw new InvalidConfigurationException(String.Format("The '{0}' is not a query definition.", queryPath));

			return ExecQuery(queryDef.QueryText, args);
		}		

		private static void ApplyPredefinedVariable(IDictionary<string, object> variables)
		{			
			variables["project"] = Impl.TeamProject;
		}
	}
}