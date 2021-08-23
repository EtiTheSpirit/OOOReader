using OOODumper.LazySRGReimpl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OOODumper {
	class Program {

		public const string BASE = @"F:\Users\Xan\source\repos\OOOReader\OOODumper\bin\Debug\net5.0";

		static void Main(string[] args) {
			StringBuilder sb = new StringBuilder();
			
			StringBuilder rf = new StringBuilder();
			var OOOLibAndDeps = Assembly.LoadFile(@$"{BASE}\OOOLibAndDeps.dll");
			var projectx_pcode = Assembly.LoadFile(@$"{BASE}\projectx-pcode.dll");
			var projectx_pcode_2 = Assembly.LoadFile(@$"{BASE}\projectx-pcode-newer.dll");
			IEnumerable<Type> types1 = OOOLibAndDeps.GetTypes().Where(type => {
				return type.FullName.Contains("com.threerings");
			});
			IEnumerable<Type> types2 = projectx_pcode.GetTypes().Where(type => {
				return type.FullName.Contains("com.threerings");
			});
			IEnumerable<Type> types3 = projectx_pcode_2.GetTypes().Where(type => {
				return type.FullName.Contains("com.threerings");
			});
			IEnumerable<Type> types = types1.Concat(types2).Concat(types3);
			Console.WriteLine("Found " + types.Count() + " types.");
			List<string> withReadFields = new List<string>();
			foreach (Type type in types) {
				ExportableShadowClass.Populate(type);
				MethodInfo mtd = type.GetMethod("readFields");
				if (mtd != null) {
					if (mtd.DeclaringType == type) {
						// Strictly declared only.
						string name = type.ToString();
						if (!withReadFields.Contains(name)) {
							withReadFields.Add(name);
							rf.AppendLine(name);
						}
					} else {
						// rf.AppendLine(type.ToString() + " <= " + mtd.DeclaringType.ToString());
					}
				}
			}
			ExportableShadowClass.AppendAllTo(sb);

			File.WriteAllText("./OOOClassDump.txt", sb.ToString());
			File.WriteAllText("./WithCustomReadFields.txt", rf.ToString());
		}
	}
}
