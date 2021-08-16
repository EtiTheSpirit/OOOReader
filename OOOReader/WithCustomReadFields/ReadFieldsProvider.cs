#nullable enable
using OOOReader.Clyde;
using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.WithCustomReadFields {
	public static class ReadFieldsProvider {

		private static readonly IReadOnlyDictionary<string, Action<int, ShadowClass, ClydeFile>> READ_FIELDS = new Dictionary<string, Action<int, ShadowClass, ClydeFile>>() {
			["com.threerings.config.ConfigGroup"] = (numFields, cls, clydeFile) => {
				Array? mgConfigs = clydeFile.Read<Array>("configs", null);
				string? clsName = clydeFile.Read<string>("class");
				if (clsName != null) {
					cls.SetField("clazz", ShadowClass.GetOrCreate(clsName));
				} else {
					cls.SetField("clazz", ShadowClass.GetOrCreate("com.threerings.config.ManagedConfig"));
				}
			},
			["com.threerings.config.ConfigManager"] = (numFields, cls, clydeFile) => {
				Array? groups = clydeFile.Read<Array>("groups", Array.Empty<ShadowClass>());

				if (groups != null) {
					for (int idx = 0; idx < groups.Length; idx++) {
						if (cls.TryGetField("_groups", out ShadowClass? shd)) {
							
						}
					}
				}
			},
		};

		public static Action<int, ShadowClass, ClydeFile>? GetReadFieldsMethod(ShadowClass? forClass) {
			do {
				if (READ_FIELDS.TryGetValue(forClass!.Signature, out var val)) {
					return val;
				}
				forClass = forClass.BaseClass;
			} while (forClass != null);
			return null;
		}

	}
}
