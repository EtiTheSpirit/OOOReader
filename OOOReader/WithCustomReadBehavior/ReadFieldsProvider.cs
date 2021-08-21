#nullable enable
using OOOReader.Clyde;
using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.WithCustomReadBehavior {
	public static class ReadFieldsProvider {

		private static readonly IReadOnlyDictionary<string, Action<int, ShadowClass, ClydeFile>> READ_FIELDS = new Dictionary<string, Action<int, ShadowClass, ClydeFile>>() {
			["com.threerings.config.ConfigGroup"] = (numFields, cls, clydeFile) => {
				Array? mgConfigs = clydeFile.Read<Array>("configs", null);
				string? clsName = clydeFile.Read<string>("class");
				if (clsName != null) {
					cls.SetField("clazz", ShadowClass.CreateInstanceOf(clsName));
				} else {
					cls.SetField("clazz", ShadowClass.CreateInstanceOf("com.threerings.config.ManagedConfig"));
				}
			},
			["com.threerings.config.ConfigManager"] = (numFields, cls, clydeFile) => {
				Array? groups = clydeFile.Read<Array>("groups", Array.Empty<ShadowClass>());

				if (groups != null) {
					for (int idx = 0; idx < groups.Length; idx++) {
						if (cls.TryGetField("_groups", out object? shd)) {
							if (shd is Dictionary<object, object?> dict) {
								cls.TryGetField("_cclass", out object? cfgClass);
								dict[groups.GetValue(idx)!] = cfgClass;
							} else {
								throw new InvalidCastException("Unexpected type " + shd!.GetType().ToString());
							}
						}
					}
				}
			},
			["com.threerings.config.ManagedConfig"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);
				cls.SetField("comment", clydeFile.Read("comment", string.Empty));
			},
			["com.threerings.math.Transform2D"] = (numFields, cls, clydeFile) => {
				ShadowClass? translation = clydeFile.Read<ShadowClass?>("translation");
				cls.SetField("_translation", translation);

				float rotation = clydeFile.Read("rotation", 0f);
				cls.SetField("_rotation", rotation);

				float scale = clydeFile.Read("scale", 1f);
				cls.SetField("_scale", scale);

				ShadowClass? matrix = clydeFile.Read<ShadowClass?>("matrix");
				cls.SetField("_matrix", matrix);

				if (matrix != null) {
					bool isAffine = (float)matrix.GetField("m02")! == 0f && (float)matrix.GetField("m12")! == 0f && (float)matrix.GetField("m22")! == 1f;
					cls.SetField("_type", isAffine ? 3 : 4);
				} else if (translation == null && rotation == 0 && scale == 1) {
					cls.SetField("_type", 0);
				} else {
					cls.SetField("_translation", translation ?? ShadowClass.CreateInstanceOf("com.threerings.math.Vector2f"));
					cls.SetField("_type", scale == 1 ? 1 : 2);
				}
			},
			["com.threerings.math.Transform3D"] = (numFields, cls, clydeFile) => {
				ShadowClass? translation = clydeFile.Read<ShadowClass?>("translation");
				cls.SetField("_translation", translation);

				ShadowClass? rotation = clydeFile.Read<ShadowClass?>("rotation");
				cls.SetField("_rotation", rotation);

				float scale = clydeFile.Read("scale", 1f);
				cls.SetField("_scale", scale);

				ShadowClass? matrix = clydeFile.Read<ShadowClass?>("matrix");
				cls.SetField("_matrix", matrix);

				if (matrix != null) {
					bool isAffine = (float)matrix.GetField("m03")! == 0f && (float)matrix.GetField("m13")! == 0f && (float)matrix.GetField("m23")! == 0f && (float)matrix.GetField("m33")! == 1f;
					cls.SetField("_type", isAffine ? 3 : 4);
				} else if (translation == null && rotation == null && scale == 1) {
					cls.SetField("_type", 0);
				} else {
					cls.SetField("_translation", translation ?? ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f"));
					cls.SetField("_rotation", rotation ?? ShadowClass.CreateInstanceOf("com.threerings.math.Quaternion"));
					cls.SetField("_type", scale == 1 ? 1 : 2);
				}
			},
			["com.threerings.opengl.effect.config.BaseParticleSystemConfig$Layer"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);
				cls.TryGetField("moveParticlesWithEmitter", out bool moveParticlesWithEmitter);
				clydeFile.ReadInto(cls, "rotateOrientationsWithEmitter", moveParticlesWithEmitter);
			},
			["com.threerings.opengl.model.config.ArticulatedConfig"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);

				// calls "initTransientFields", which calls updateRefTransforms on the root node.
				/*
				cls.TryGetField("root", out ShadowClass? root);
				if (root != null) {
					// Param was a blank Transform3D
					ShadowClass blankTransform3D = ShadowClass.CreateInstanceOf("com.threerings.math.Transform3D");
					// TODO: Compose method
					// TODO: Invert method
					// root.TryGetField("children", out List<ShadowClass>? children);
					// TODO: Call updateRefTransforms on each child too.
				}
				*/
			},
			
			["com.threerings.tudey.data.TudeySceneModel"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);
				clydeFile.ReadInto(cls, "sceneId", 0);
				clydeFile.ReadInto(cls, "name", string.Empty);
				clydeFile.ReadInto(cls, "version", 1);
				clydeFile.ReadInto(cls, "auxModels", new List<ShadowClass>());

				// TODO: tile garbage

				clydeFile.Read("entries", new List<ShadowClass>());

				// TODO: more tile garbage

				string[]? layers = clydeFile.Read("layers", Array.Empty<string>());
				cls.SetField("layers", layers);
				if (layers != null) {
					for (int idx = 0; idx < layers.Length; idx++) {
						clydeFile.Read("layer" + idx, Array.Empty<string>());
					}
				}
				
			},
			["com.threerings.tudey.util.CoordIntMap"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);
				// And nothing else?
			},
			["com.threerings.tudey.util.CoordIntMap$Cell"] = (numFields, cls, clydeFile) => {
				//clydeFile.ReadFieldsDefault(cls);
				// And nothing else?
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
