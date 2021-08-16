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
			using ClassDumper writer = new ClassDumper(swriter);

			StringBuilder rf = new StringBuilder();
			Type cl = typeof(com.threerings.ClydeLog);
			IEnumerable<Type> types = cl.Assembly.GetTypes().Where(type => {
				return type.FullName.Contains("com.threerings");
			});
			Console.WriteLine("Found " + types.Count() + " types.");
			foreach (Type type in types) {
				writer.WriteTypeAndMembers(type);
				MethodInfo mtd = type.GetMethod("readFields");
				if (mtd != null) {
					if (mtd.DeclaringType == type) {
						// Strictly declared only.
						rf.AppendLine(type.ToString());
					} else {
						// rf.AppendLine(type.ToString() + " <= " + mtd.DeclaringType.ToString());
					}
				}
			}

			File.WriteAllText("./OOOClassDump.txt", sb.ToString());
			File.WriteAllText("./WithCustomReadFields.txt", rf.ToString());
		}
	}
}
