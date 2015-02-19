using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Context;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

namespace AnFake.Plugins.NHibernate
{
	internal sealed class NhPlugin : PluginBase
	{
		private readonly Configuration _configuration;
		private readonly NhAutoMapper _autoMapper;

		private ISessionFactory _sessionFactory;

		public NhPlugin(IBuildServer buildServer)
		{
			var connectionString = buildServer.IsLocal
				? MyBuild.GetProp("Nh.LocalConnectionString", null)
				: MyBuild.GetProp("Nh.ConnectionString", null);

			_configuration = new Configuration();
			_configuration.DataBaseIntegration(
				dbi =>
				{
					dbi.ConnectionProvider<DriverConnectionProvider>();
					dbi.Dialect<MsSql2005Dialect>();
					dbi.Driver<SqlClientDriver>();
					dbi.LogSqlInConsole = false;
					dbi.SchemaAction = SchemaAutoAction.Update;
					dbi.ConnectionString = connectionString;
				})
				.CurrentSessionContext<ThreadStaticSessionContext>();

			_autoMapper = new NhAutoMapper();
		}

		public override void Dispose()
		{
			if (_sessionFactory != null)
			{
				_sessionFactory.Dispose();
				_sessionFactory = null;
			}

			base.Dispose();
		}

		public Configuration Configuration
		{
			get { return _configuration; }
		}

		public NhAutoMapper AutoMapper
		{
			get { return _autoMapper; }
		}

		public ISessionFactory SessionFactory
		{
			get
			{
				if (_sessionFactory != null)
					return _sessionFactory;

				Trace.Info("NHibernate: Initiating...");

				var connectionString = _configuration.GetProperty("connection.connection_string");
				if (String.IsNullOrEmpty(connectionString))
					throw new InvalidConfigurationException(
						"NHibernate: connection string isn't specified.\n" +
						"Hint. You might provide connection string with the following ways:\n" +
						" * using 'Nh.ConnectionString' command line parameter for server-side build;\n" +
						" * using 'Nh.LocalConnectionString' command line parameter for local build;\n" +
						" * using 'Nh.ConnectionString' infrastructure config parameter;\n" +
						" * using 'Nh.LocalConnectionString' user config  parameter.");

				Trace.InfoFormat("NHibernate: Using connection string '{0}'", connectionString);

				var mapping = (HbmMapping) null;
				while ((mapping = _autoMapper.Pull()) != null)
				{
					_configuration.AddDeserializedMapping(mapping, mapping.assembly);
				}

				Trace.InfoFormat("NHibernate: Updating schema...\n  {0}", String.Join("\n  ", _autoMapper.MappedTypes.Select(x => x.Name)));

				var showSql = "true".Equals(_configuration.GetProperty("show_sql"), StringComparison.OrdinalIgnoreCase);
				new SchemaUpdate(_configuration).Execute(showSql, true);

				Trace.Info("NHibernate: Schema successfuly updated.");

				_sessionFactory = _configuration.BuildSessionFactory();

				return _sessionFactory;
			}
		}

		public void ReconfigureNeeded()
		{
			if (_sessionFactory == null)
				return;

			_sessionFactory.Dispose();
			_sessionFactory = null;
		}
	}
}