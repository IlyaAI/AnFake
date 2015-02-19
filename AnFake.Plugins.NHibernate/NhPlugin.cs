using System;
using System.Collections.Generic;
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
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;
using NHibernate.Util;

namespace AnFake.Plugins.NHibernate
{
	internal sealed class NhPlugin : PluginBase
	{
		private readonly ISet<Type> _requestedEntityTypes = new HashSet<Type>();
		private readonly ISet<Type> _mappedEntityTypes = new HashSet<Type>();
		private readonly Configuration _configuration;
		private readonly ConventionModelMapper _mapper;

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

			_mapper = new ConventionModelMapper();

			_mapper.IsEntity(
				(type, declared) =>
				{
					if (declared || _requestedEntityTypes.Contains(type) || _mappedEntityTypes.Contains(type))
						return true;

					if (!type.IsClass || type.BaseType != typeof (Object) || type.FullName.StartsWith("System."))
						return false;

					_requestedEntityTypes.Add(type);
					return true;
				});

			_mapper.IsPersistentId((member, declared) => member.Name.Equals("Id"));

			_mapper.BeforeMapClass +=
				(inspector, type, customizer) =>
				{
					customizer.Lazy(false);
					customizer.Table(type.Name);					
					customizer.EntityName(type.Name);
				};

			_mapper.AfterMapClass +=
				(inspector, type, customizer) =>
				{
					if (type.GetMember("Id").Length == 0)
					{
						customizer.Id(
							mapper =>
							{
								mapper.Type(new Int64Type());
								mapper.Generator(new NativeGeneratorDef());
							});
					}
				};

			_mapper.BeforeMapProperty +=
				(inspector, member, customizer) =>
				{
					var type = member.LocalMember.GetPropertyOrFieldType();
					if (type.IsValueType && !type.IsNullable())
					{
						customizer.NotNullable(true);
					}
				};

			_mapper.BeforeMapOneToMany +=
				(inspector, member, customizer) =>
				{
					var elementType = member.LocalMember.DetermineRequiredCollectionElementType();
					customizer.EntityName(elementType.Name);
				};

			_mapper.BeforeMapManyToOne +=
				(inspector, member, customizer) =>
				{
					var type = member.LocalMember.GetPropertyOrFieldType();
					customizer.Class(type);
				};

			_mapper.BeforeMapSet += (inspector, member, customizer) => customizer.Cascade(Cascade.All);
			_mapper.BeforeMapList += (inspector, member, customizer) => customizer.Cascade(Cascade.All);
			_mapper.BeforeMapBag += (inspector, member, customizer) => customizer.Cascade(Cascade.All);
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

		public ISet<Type> RequestedEntityTypes
		{
			get { return _requestedEntityTypes; }
		}

		public ISet<Type> MappedEntityTypes
		{
			get { return _mappedEntityTypes; }
		}

		public Configuration Configuration
		{
			get { return _configuration; }
		}

		public ConventionModelMapper Mapper
		{
			get { return _mapper; }
		}

		public ISessionFactory SessionFactory
		{
			get
			{
				if (_sessionFactory != null)
					return _sessionFactory;

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
				
				Trace.Info("NHibernate: Mapped classes:");
				while (_requestedEntityTypes.Count > 0)
				{
					var type = _requestedEntityTypes.First();
					_requestedEntityTypes.Remove(type);

					if (!_mappedEntityTypes.Add(type))
						continue;

					Trace.InfoFormat("  {0}", type.FullName);

					var mapping = _mapper.CompileMappingFor(new[] {type});
					FixManyToOneRelations(mapping.RootClasses[0]);

					_configuration.AddDeserializedMapping(mapping, type.FullName);
				}

				Trace.Info("NHibernate: Updating schema...");

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

		private static void FixManyToOneRelations(HbmClass mapping)
		{
			foreach (var relation in mapping.Items.OfType<HbmManyToOne>())
			{
				var index = relation.@class.LastIndexOfAny(new []{'.', '+'});
				if (index >= 0)
				{
					relation.entityname = relation.@class.Substring(index + 1);
				}
			}
		}
	}
}