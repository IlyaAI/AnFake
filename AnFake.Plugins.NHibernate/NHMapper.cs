using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;

namespace AnFake.Plugins.NHibernate
{
	public static class NhMapper
	{
		private static readonly ISet<Type> PersistentTypes = new HashSet<Type>();
		private static readonly ConventionModelMapper Impl;
		private static HbmMapping _mapping;

		static NhMapper()
		{
			Impl = new ConventionModelMapper();

			Impl.IsEntity(
				(type, declared) => PersistentTypes.Contains(type));

			Impl.BeforeMapClass += (inspector, type, customizer) =>
			{
				customizer.Lazy(false);
				customizer.Table(type.Name);

				/*if (type.GetMember("Id", MemberTypes.Property | MemberTypes.Field, BindingFlags.Public).Length == 0)
				{
					customizer.Id(mapper =>
					{
						mapper.Column("Id");
						mapper.Type(new Int64Type());
						mapper.Generator(new NativeGeneratorDef());
					});
				}*/
			};			
		}

		internal static HbmMapping Mapping
		{
			get { return _mapping ?? (_mapping = Impl.CompileMappingFor(PersistentTypes)); }
		}

		public static void Class<T>()
			where T : class
		{
			PersistentTypes.Add(typeof (T));
		}

		public static void Class<T>(Action<IClassMapper<T>> customize)
			where T : class
		{
			PersistentTypes.Add(typeof (T));

			Impl.Class(customize);
		}

		public static void FromAssembly(string name)
		{
			
		}

		public static void Reset()
		{			
			_mapping = null;
		}
	}
}