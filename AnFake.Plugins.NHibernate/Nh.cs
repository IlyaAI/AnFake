using System;
using AnFake.Core;
using AnFake.Core.Exceptions;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;
using NHibernate.Mapping.ByCode;

namespace AnFake.Plugins.NHibernate
{
	/// <summary>
	///		Persistence related tools based on HNibernate.
	/// </summary>
	/// <remarks>
	///		<para>Provides facility to persist simple data contract objects with convention based mapping.</para>
	/// 
	///		<para>IMPORTANT! Only simple properties, one-to-many and many-to-one relations are supported.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// [&lt;DataContract>]
	/// type PerformanceReport () =
	///	    [&lt;DataMember>] member val Name: string = null with get, set
	///     [&lt;DataMember>] member val ElapsedTime: double = 0.0 with get, set
	///     [&lt;DataMember>] member val BytesProcessed: int64 = 0L with get, set
	/// 
	/// Nh.MapClass&lt;PerformanceReport>()
	/// 
	/// let report = Json.Read&lt;PerformanceReport>("report.json".AsFile())
	///
    /// Nh.DoWork(fun uow ->
    ///     uow.Save(report)
    ///     uow.Commit()
    /// )
	/// </code>
	/// </example>
	public static class Nh
	{
		/// <summary>
		///		Unit of work.
		/// </summary>
		public sealed class UnitOfWork : IDisposable
		{
			private readonly ISession _hsx;
			private readonly ITransaction _tx;
			private bool _disposed;

			internal UnitOfWork()
			{
				_hsx = Impl.SessionFactory.OpenSession();
				try
				{
					_tx = _hsx.BeginTransaction();
				}
				catch (Exception)
				{
					_hsx.Dispose();
					throw;
				}
			}

			/// <summary>
			///		Creates HQL-based query.
			/// </summary>
			/// <param name="hql">query string (not null or empty)</param>
			/// <returns><c>IQuery</c> object</returns>
			/// <example>
			/// <code>
			/// Nh.DoWork(fun uow ->
			///     let prevReports = 
			///         uow.Query("from PerformanceReport order by id desc")
            ///             .SetMaxResults(5)
            ///             .List&lt;PerformanceReport>()
            ///     
            ///     // do something with prevReports
            /// )
			/// </code>
			/// </example>
			public IQuery Query(string hql)
			{
				if (String.IsNullOrEmpty(hql))
					throw new ArgumentException("Nh.UnitOfWork.Query(hql): hql must not be null or empty");

				return _hsx.CreateQuery(hql);
			}

			/// <summary>
			///		Saves (inserts or updates) entity to persistent store.
			/// </summary>
			/// <remarks>
			///		Entity class must be mapped via one of the <c>MapClass</c> methods.
			/// </remarks>
			/// <param name="entity">entity to be saved (not null)</param>
			public void Save(object entity)
			{
				if (entity == null)
					throw new ArgumentException("Nh.UnitOfWork.Save(entity): entity must not be null");

				_hsx.Save(entity);
			}

			/// <summary>
			///		Deletes entity from persistent store.
			/// </summary>
			/// <param name="entity">entity to be deleted (not null)</param>
			public void Delete(object entity)
			{
				if (entity == null)
					throw new ArgumentException("Nh.UnitOfWork.Delete(entity): entity must not be null");

				_hsx.Delete(entity);
			}

			/// <summary>
			///		Flushes all pending operations.
			/// </summary>
			/// <remarks>
			///		Normally you should not call <c>Flush</c> explicitly.
			/// </remarks>
			public void Flush()
			{
				_hsx.Flush();
			}

			/// <summary>
			///		Commits all changes to persistent store.
			/// </summary>
			public void Commit()
			{
				_tx.Commit();
			}

			/// <summary>
			///		Disposes current unit-of-work. If <c>Commit</c> wasn't called before disposing then transaction is rolled back.
			/// </summary>
			/// <remarks>
			///		Normally it isn't neccessary to call <c>Dispose</c> explicitly. It will be automatically called after build run.
			/// </remarks>
			public void Dispose()
			{
				if (_disposed)
					return;

				if (!_tx.WasCommitted && !_tx.WasRolledBack)
					_tx.Rollback();

				_tx.Dispose();
				_hsx.Dispose();

				_disposed = true;
			}
		}

		private static NhPlugin _impl;

		private static NhPlugin Impl
		{
			get
			{
				if (_impl != null)
					return _impl;

				_impl = Plugin.Get<NhPlugin>();
				_impl.Disposed += () => _impl = null;

				return _impl;
			}
		}

		/// <summary>
		///		NHibernate configuration.
		/// </summary>
		public static IFluentSessionFactoryConfiguration Configuration
		{
			get
			{
				var cfg = Impl.Configuration.SessionFactory();
				Impl.ReconfigureNeeded();

				return cfg;
			}
		}

		/// <summary>
		///		Convention mapper.
		/// </summary>
		public static ConventionModelMapper ConventionMapper
		{
			get
			{
				var mapper = Impl.Mapper;
				Impl.ReconfigureNeeded();

				return mapper;
			}
		}

		/// <summary>
		///		Activates NHibernate plugin.
		/// </summary>
		public static void PlugIn()
		{
			Plugin.Register<NhPlugin>().AsSelf();
		}

		/// <summary>
		///		Applies convention based mapping to specified class.
		/// </summary>
		/// <remarks>
		///		IMPORTANT! Only simple properties, one-to-many and many-to-one relations are supported.
		/// </remarks>		
		/// <typeparam name="T">class to be mapped</typeparam>
		public static void MapClass<T>()
			where T : class
		{
			EnsureNonSystem<T>();

			Impl.RequestedEntityTypes.Add(typeof (T));
			Impl.ReconfigureNeeded();
		}

		/// <summary>
		///		Applies convention based mapping to specified class with customizations.
		/// </summary>
		/// <remarks>
		///		IMPORTANT! Only simple properties, one-to-many and many-to-one relations are supported.
		/// </remarks>		
		/// <param name="customize">action which provides customization</param>
		/// <typeparam name="T">class to be mapped</typeparam>
		/// <example>
		/// <code>
		/// Nh.MapClass&lt;PerformanceReport>(
        ///     fun map ->
        ///         map.Property(
        ///            "Name",                
        ///            fun p -> p.Index("IDX_Name")
        ///     )
		/// )
		/// </code>
		/// </example>
		public static void MapClass<T>(Action<IClassMapper<T>> customize)
			where T : class
		{
			EnsureNonSystem<T>();

			Impl.RequestedEntityTypes.Add(typeof (T));
			Impl.Mapper.Class(customize);
			Impl.ReconfigureNeeded();
		}		

		/// <summary>
		///		Runs given action in transaction scope.
		/// </summary>
		/// <param name="body">action body (not null)</param>		
		public static void DoWork(Action<UnitOfWork> body)
		{
			if (body == null)
				throw new ArgumentException("Nh.DoWork(body): body must not be null");

			using (var uow = new UnitOfWork())
			{
				body.Invoke(uow);
			}
		}

		private static void EnsureNonSystem<T>()
		{
			if (typeof(T).FullName.StartsWith("System."))
				throw new InvalidConfigurationException(
					String.Format(
					"System types aren't allowed to be persistent entities: '{0}'.", 
					typeof(T).FullName));
		}
	}
}