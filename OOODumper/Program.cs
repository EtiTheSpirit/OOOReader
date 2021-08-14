using OOODumper.LazySRGReimpl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OOODumper {
	class Program {

		static void Main(string[] args) {
			StringBuilder sb = new StringBuilder();
			using StringWriter swriter = new StringWriter(sb);
			using SRGStyleWriter writer = new SRGStyleWriter(swriter);

			Type cl = typeof(com.threerings.ClydeLog);
			IEnumerable<Type> types = cl.Assembly.GetTypes().Where(type => {
				return type.FullName.Contains("com.threerings");
			});
			Console.WriteLine("Found " + types.Count() + " types.");
			foreach (Type type in types) {
				writer.WriteTypeAndMembers(type);
			}

			File.WriteAllText("./classdump.txt", sb.ToString());
		}
	}
}
