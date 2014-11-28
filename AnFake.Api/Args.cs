using System;
using System.Text;

namespace AnFake.Api
{
	public sealed class Args
	{
		private readonly StringBuilder _args = new StringBuilder();
		private readonly string _optionMarker;
		private readonly string _nameValueMarker;

		public Args(string optionMarker, string nameValueMarker)
		{
			_optionMarker = optionMarker;
			_nameValueMarker = nameValueMarker;
		}

		public Args NonQuotedValue(string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			_args.Append(value);

			return this;
		}

		public Args QuotedValue(string value)
		{
			if (String.IsNullOrEmpty(value))
				return this;

			_args.Append("\"");

			var start = 0;
			for (var i = 0; i < value.Length; i++)
			{
				if (i < start)
					continue;
				
				switch (value[i])
				{
					case '"':
						_args.Append(value, start, i - start).Append("\\\"");
						start = i + 1;
						break;

					case '\\':
						if (i + 1 < value.Length)
						{
							if (value[i + 1] == '"')
							{
								_args.Append(value, start, i - start).Append("\\\\\\\"");
								start = i + 2;
							}
						}
						else
						{
							_args.Append(value, start, i - start).Append("\\\\");
							start = i + 1;
						}
						break;
				}
			}

			if (start < value.Length)
			{
				_args.Append(value, start, value.Length - start);
			}

			_args.Append("\"");
			
			return this;
		}

		public Args Space()
		{
			if (_args.Length > 0 && !Char.IsWhiteSpace(_args[_args.Length - 1]))
			{
				_args.Append(" ");
			}

			return this;
		}

		public Args ValuedOption(string name)
		{
			_args.Append(_optionMarker).Append(name).Append(_nameValueMarker);			

			return this;
		}

		public Args Option(string name)
		{
			Space();

			_args.Append(_optionMarker).Append(name);

			return this;
		}		

		public override string ToString()
		{
			return _args.ToString();
		}
	}
}