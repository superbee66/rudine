

using System;
using System.Linq;

namespace Rudine.Interpreters.Pdf.util
{
	public static class TypeParser
	{
		public static Type LcdType(params string[] samples)
		{

		        bool bool_test;
			if (samples.All(s => bool.TryParse(s, out bool_test)))
			return typeof(bool);

		        sbyte sbyte_test;
			if (samples.All(s => sbyte.TryParse(s, out sbyte_test)))
			return typeof(sbyte);

		        byte byte_test;
			if (samples.All(s => byte.TryParse(s, out byte_test)))
			return typeof(byte);

		        short short_test;
			if (samples.All(s => short.TryParse(s, out short_test)))
			return typeof(short);

		        ushort ushort_test;
			if (samples.All(s => ushort.TryParse(s, out ushort_test)))
			return typeof(ushort);

		        int int_test;
			if (samples.All(s => int.TryParse(s, out int_test)))
			return typeof(int);

		        uint uint_test;
			if (samples.All(s => uint.TryParse(s, out uint_test)))
			return typeof(uint);

		        long long_test;
			if (samples.All(s => long.TryParse(s, out long_test)))
			return typeof(long);

		        ulong ulong_test;
			if (samples.All(s => ulong.TryParse(s, out ulong_test)))
			return typeof(ulong);

		        char char_test;
			if (samples.All(s => char.TryParse(s, out char_test)))
			return typeof(char);

		        float float_test;
			if (samples.All(s => float.TryParse(s, out float_test)))
			return typeof(float);

		        double double_test;
			if (samples.All(s => double.TryParse(s, out double_test)))
			return typeof(double);

	
			return typeof(string);

		}
	}
}