using System;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	public static class NameGen
	{
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

		public static Params Defaults { get; private set; }

		static NameGen()
		{
			Defaults = new Params();
		}

		public static string Generate(string basicName, Predicate<string> validator)
		{
			return Generate(basicName, validator, p => { });
		}

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