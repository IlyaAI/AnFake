using System;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents unique name generation tool.
	/// </summary>
	public static class NameGen
	{
		/// <summary>
		///		Name generation parameters.
		/// </summary>
		public sealed class Params
		{
			public string NameGenerationFormat;
			public int MaxGeneratedNames;

			internal Params()
			{
				NameGenerationFormat = "{0}@{1}";
				MaxGeneratedNames = 10;
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Defauls name generation parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static NameGen()
		{
			Defaults = new Params();
		}

		/// <summary>
		///		Equals to <c>Generate(basicName, validator, () => {})</c>.
		/// </summary>
		/// <param name="basicName">basic name (not null or empty)</param>
		/// <param name="validator">predicate which should return true if name is unique</param>
		/// <returns>unique name</returns>
		public static string Generate(string basicName, Predicate<string> validator)
		{
			return Generate(basicName, validator, p => { });
		}

		/// <summary>
		///		Generates unique name by adding numeric suffix to 'basicName'.
		/// </summary>
		/// <param name="basicName">basic name (not null or empty)</param>
		/// <param name="validator">predicate which should return true if name is unique</param>
		/// <param name="setParams">action to override default parameters</param>
		/// <returns>unique name</returns>
		public static string Generate(string basicName, Predicate<string> validator, Action<Params> setParams)
		{
			if (String.IsNullOrEmpty(basicName))
				throw new ArgumentException("NameGen.Generate(basicName, validator[, setParams]): basicName must not be null or empty");

			if (validator == null)
				throw new ArgumentException("NameGen.Generate(basicName, validator[, setParams]): validator must not be null");

			if (setParams == null)
				throw new ArgumentException("NameGen.Generate(basicName, validator, setParams): setParams must not be null");

			if (validator(basicName))
				return basicName;

			var parameters = Defaults.Clone();
			setParams(parameters);

			var index = 2;
			while (index <= parameters.MaxGeneratedNames)
			{
				var name = String.Format(parameters.NameGenerationFormat, basicName, index);
				if (validator(name))
					return name;

				index++;
			}

			throw new InvalidConfigurationException(String.Format("NameGen.Generate: too many ({0}+) generated names on the same basis '{1}'", index, basicName));
		}
	}
}