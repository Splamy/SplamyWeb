using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SplamyWeb.Tests;

	class Test
	{
		static void Main(string[] args)
		{
			MyColl col = [1, 2, 3];
		}
	}

	[CollectionBuilder(typeof(MyCollBuilder), "Create")]
	abstract class MyColl : IEnumerable<int>
	{
		public List<int> list;

		public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)list).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)list).GetEnumerator();
	}

	static class MyCollBuilder
	{
		public static MyColl Create(ReadOnlySpan<int> values) => new MyCollImpl() { list = new List<int>(values.ToArray()) };

		class MyCollImpl : MyColl { }
	}
