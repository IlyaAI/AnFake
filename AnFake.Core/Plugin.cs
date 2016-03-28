using System;
using System.Collections.Generic;
using AnFake.Api;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using AnFake.Core.Integration.Builds;
using AnFake.Core.Integration.Tests;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Registration;

namespace AnFake.Core
{
	/// <summary>
	///     Represents "entry-point" for accessing pluged-in functionality.
	/// </summary>
	public static class Plugin
	{
		// ReSharper disable once InconsistentNaming
		private static readonly Dictionary<Type, Object> _registrators = new Dictionary<Type, object>();
		private static ContainerBuilder _builder = new ContainerBuilder();
		private static IContainer _container;

		public sealed class Registrator<T>
		{
			private readonly IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> _registrator;

			internal Registrator(IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> registrator)
			{
				_registrator = registrator;
			}

			/// <summary>
			///     Defines interface provided by registered plugin.
			/// </summary>
			/// <typeparam name="TInterface">interface type provided by plugin</typeparam>
			/// <returns>this</returns>
			public Registrator<T> As<TInterface>()
			{
				_registrator.As<TInterface>();

				return this;
			}

			/// <summary>
			///     Defines plugin as self-service.
			/// </summary>			
			/// <returns>this</returns>
			public Registrator<T> AsSelf()
			{
				_registrator.AsSelf();

				return this;
			}

			internal Registrator<T> AutoActivate()
			{
				_registrator.AutoActivate();

				return this;
			}
		}

		/// <summary>
		///     Registers plugin.
		/// </summary>
		/// <remarks>
		///     <para>Plugins should be registered in 'build configuration phase' i.e. before any target starts.</para>
		///     <para>By default registered plugins instantiated just before invoking of first target.</para>
		/// </remarks>
		/// <typeparam name="TPlugin">class representing plugin</typeparam>
		public static Registrator<TPlugin> Register<TPlugin>()
			where TPlugin : class
		{
			EnsureBuilder();

			Object registrator;
			if (_registrators.TryGetValue(typeof (TPlugin), out registrator))			
				return ((Registrator<TPlugin>) registrator).AutoActivate();

			var pluginType = typeof (TPlugin);
			Trace.InfoFormat("Plugged-in: {0}, {1}", pluginType.FullName, pluginType.Assembly.FullName);

			var typedRegistrator = new Registrator<TPlugin>(
				_builder.RegisterType<TPlugin>()
					.SingleInstance()
					.AutoActivate()
				);

			_registrators.Add(typeof(TPlugin), typedRegistrator);

			return typedRegistrator;
		}

		/// <summary>
		///     Registers plugin with on-demand activation.
		/// </summary>
		/// <remarks>
		///     <para>Plugin with on-demand activation will be instantiated on first call.</para>
		///     <para>
		///         On-demand activation should be used carefully because plugin might miss some events due to later
		///         instantiation.
		///     </para>
		/// </remarks>
		/// <typeparam name="TPlugin">class representing plugin</typeparam>
		public static Registrator<TPlugin> RegisterOnDemand<TPlugin>()
			where TPlugin : class
		{
			EnsureBuilder();

			Object registrator;
			if (_registrators.TryGetValue(typeof(TPlugin), out registrator))
				return (Registrator<TPlugin>)registrator;

			var pluginType = typeof (TPlugin);
			Trace.InfoFormat("Plugged-in (on-demand): {0}, {1}", pluginType.FullName, pluginType.Assembly.FullName);

			var typedRegistrator = new Registrator<TPlugin>(
				_builder.RegisterType<TPlugin>()
					.SingleInstance()
				);

			_registrators.Add(typeof(TPlugin), typedRegistrator);

			return typedRegistrator;
		}

		/// <summary>
		///     Registers default implementation.
		/// </summary>
		/// <typeparam name="TImpl">class providing default implementation</typeparam>
		public static Registrator<TImpl> RegisterDefault<TImpl>()
			where TImpl : class
		{
			EnsureBuilder();

			return new Registrator<TImpl>(
				_builder.RegisterType<TImpl>()
					.PreserveExistingDefaults()
					.SingleInstance()
					.AutoActivate()
				);
		}

		/// <summary>
		///     Gets implementation of specified interface or throws an exception if no one registered.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <returns></returns>
		public static TInterface Get<TInterface>()
		{
			EnsureContainer();

			try
			{
				return _container.Resolve<TInterface>();
			}
			catch (ComponentNotRegisteredException)
			{
				throw new InvalidConfigurationException(
					String.Format(
						"There is no registered plugin which provides '{0}' interface. Hint: probably, you forgot to call PlugIn() for some plugins.",
						typeof(TInterface).Name));
			}
			catch (DependencyResolutionException e)
			{
				var anfake = FindAnFakeException(e);
				if (anfake != null)
					throw anfake;

				throw;
			}			
		}

		/// <summary>
		///     Finds implementation of specified interface or returns null if no one registered.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <returns></returns>
		public static TInterface Find<TInterface>()
		{
			EnsureContainer();

			TInterface iface;
			return _container.TryResolve(out iface)
				? iface
				: default(TInterface);
		}

		/// <summary>
		///     Resets all registrations. Normally used for test purpose only.
		/// </summary>
		internal static void Finalise()
		{
			if (_container != null)
			{
				_container.Dispose();
				_container = null;
			}

			_registrators.Clear();
			_builder = new ContainerBuilder();			
		}		

		/// <summary>
		///     Configures underlying IoC container.
		/// </summary>
		/// <remarks>
		///     This method should be called when all plugins have been registered but before first call to <c>Plugin.Get</c> or
		///     <c>Plugin.Find</c>. Normally called by runner. Also might be used to reconfigure plugins for test purpose.
		/// </remarks>
		internal static void Configure()
		{
			EnsureBuilder();

			if (_container != null)
			{
				_container.Dispose();
				_container = null;
			}

			RegisterDefault<MsTrxPostProcessor>()
				.As<IMsTrxPostProcessor>();
			RegisterDefault<LocalBuildServer>()
				.As<IBuildServer>()
				.As<IBuildServer2>();

			try
			{
				_container = _builder.Build();
				_builder = null;

				_registrators.Clear();
			}
			catch (DependencyResolutionException e)
			{
				var anfake = FindAnFakeException(e);
				if (anfake != null)
					throw anfake;

				throw;
			}			
		}

		private static void EnsureBuilder()
		{
			if (_builder == null)
				throw new InvalidOperationException("Plugin: Internal IoC container already configured. Hint: probably, you are trying to activate plugin inside of some target.");
		}

		private static void EnsureContainer()
		{
			if (_container == null)
				throw new InvalidOperationException("Plugin: Internal IoC container isn't configured. Hint: probably, you are trying to use plugin outside of any target.");
		}

		private static AnFakeException FindAnFakeException(Exception exception)
		{
			var inner = exception.InnerException;
			while (inner != null && !(inner is AnFakeException))
			{
				inner = inner.InnerException;
			}

			return (AnFakeException) inner;
		}
	}
}