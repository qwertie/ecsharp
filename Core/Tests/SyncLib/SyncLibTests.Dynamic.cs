using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Threading;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	#region Model types (no serialization concerns — tags live on the synchronizers)

	public abstract class DynShape
	{
		public int Id;
		public override bool Equals(object? obj) => obj is DynShape s && s.Id == Id && s.GetType() == GetType();
		public override int GetHashCode() => Id;
	}
	public class DynEllipse : DynShape
	{
		public double Width, Height;
		public override bool Equals(object? obj)
			=> base.Equals(obj) && obj is DynEllipse e && e.Width == Width && e.Height == Height;
		public override int GetHashCode() => Id;
	}
	public class DynPolygon : DynShape
	{
		public List<(double, double)>? Points;
		public override bool Equals(object? obj)
			=> base.Equals(obj) && obj is DynPolygon p
			&& (p.Points ?? new List<(double, double)>()).SequenceEqual(Points ?? new List<(double, double)>());
		public override int GetHashCode() => Id;
	}
	/// <summary>A shape that refers to another shape — for cyclic-graph tests.</summary>
	public class DynDuo : DynShape
	{
		public DynShape? Partner;
	}
	/// <summary>Registered via the "easy mode" delegate overload of Add.</summary>
	public class DynLabel
	{
		public string? Text;
		public override bool Equals(object? obj) => obj is DynLabel l && l.Text == Text;
		public override int GetHashCode() => Text?.GetHashCode() ?? 0;
	}
	/// <summary>The root object of most tests. A container is used so that the root
	///   and the polymorphic field are different instances: when a fixture
	///   deduplicates the root, an inner field that is the same instance as the root
	///   would be written as a back-reference that a reader cannot resolve (the
	///   classic reason synchronizers must set <see cref="ISyncManager.CurrentObject"/>).</summary>
	public class DynBox
	{
		public DynShape? Shape;
		public DynLabel? Label;
		public override bool Equals(object? obj)
			=> obj is DynBox b && Equals(b.Shape, Shape) && Equals(b.Label, Label);
		public override int GetHashCode() => (Shape?.GetHashCode() ?? 0) ^ (Label?.GetHashCode() ?? 0);
	}

	#endregion

	#region Synchronizers ([TypeTag] on each method is the ONE place tags are specified)

	public static class DynShapeSync<SM> where SM : ISyncManager
	{
		[TypeTag("Ellipse")]
		public static DynEllipse Sync(SM sm, DynEllipse? e)
		{
			sm.CurrentObject = e ??= new DynEllipse();
			e.Id     = sm.Sync("Id", e.Id);
			e.Width  = sm.Sync("Width", e.Width);
			e.Height = sm.Sync("Height", e.Height);
			return e;
		}

		[TypeTag("Polygon")]
		public static DynPolygon Sync(SM sm, DynPolygon? p)
		{
			sm.CurrentObject = p ??= new DynPolygon();
			p.Id     = sm.Sync("Id", p.Id);
			// List<(double, double)> has a default synchronizer (list of tuples)
			p.Points = sm.SyncAny("Points", p.Points);
			return p;
		}

		[TypeTag("Duo")]
		public static DynDuo Sync(SM sm, DynDuo? d)
		{
			sm.CurrentObject = d ??= new DynDuo();
			d.Id      = sm.Sync("Id", d.Id);
			d.Partner = sm.SyncDyn("Partner", d.Partner);
			return d;
		}
	}

	/// <summary>A struct synchronizer for the static tier (fully static dispatch).
	///   The struct-level [TypeTag] applies because the struct synchronizes one type.</summary>
	[TypeTag("Ellipse")]
	public struct DynEllipseSync<SM> : ISyncObject<SM, DynEllipse> where SM : ISyncManager
	{
		public DynEllipse Sync(SM sm, DynEllipse? e) => DynShapeSync<SM>.Sync(sm, e);
	}

	/// <summary>An alternative ellipse synchronizer with a different tag but the same
	///   field layout — used to test the pluggable TagMismatch policy. It is never
	///   registered in any SyncTypeRegistry.</summary>
	public static class DynOvalSync<SM> where SM : ISyncManager
	{
		[TypeTag("Oval")]
		public static DynEllipse Sync(SM sm, DynEllipse? e) => DynShapeSync<SM>.Sync(sm, e);
	}

	/// <summary>A TypeTagRegistry whose TagMismatch policy counts mismatches instead
	///   of throwing, causing statically-typed reads to proceed anyway.</summary>
	public class LenientTagRegistry : TypeTagRegistry
	{
		public int Mismatches;
		public override void TagMismatchError(string expectedTag, string tagInStream, Type expectedType, FieldId field)
			=> Mismatches++;
	}

	/// <summary>A TypeTagRegistry (with an empty dictionary) whose UnknownTag policy
	///   substitutes DynEllipse for every unknown tag.</summary>
	public class SubstituteEllipseTagRegistry : TypeTagRegistry
	{
		public override Type? UnknownTagError(string tag, Type expectedType, FieldId field)
			=> typeof(DynEllipse);
	}

	/// <summary>Swaps in both ambient registries and restores both on Dispose.</summary>
	public struct AmbientRegistries : IDisposable
	{
		AmbientService<TypeTagRegistry>.Saved _savedTags;
		AmbientService<TypeSyncRegistry>.Saved _savedSync;
		public AmbientRegistries(TypeTagRegistry tags, TypeSyncRegistry synchronizers)
		{
			_savedTags = TypeTagRegistry.SetDefault(tags);
			_savedSync = TypeSyncRegistry.SetDefault(synchronizers);
		}
		public void Dispose()
		{
			_savedSync.Dispose();
			_savedTags.Dispose();
		}
	}

	public static class DynTestData
	{
		/// <summary>Creates a synchronizer registry with all the shape synchronizers,
		///   and a paired tag registry that received their [TypeTag] tags.</summary>
		public static (TypeSyncRegistry synchronizers, TypeTagRegistry tags) NewShapeRegistries()
		{
			var tags = new TypeTagRegistry();
			var synchronizers = new TypeSyncRegistry();
			using (TypeTagRegistry.SetDefault(tags))
				synchronizers.Add(typeof(DynShapeSync<>));
			return (synchronizers, tags);
		}

		public static AmbientRegistries UseShapeRegistries()
		{
			var (synchronizers, tags) = NewShapeRegistries();
			return new AmbientRegistries(tags, synchronizers);
		}

		public static DynEllipse NewEllipse(int id = 1) => new DynEllipse { Id = id, Width = 10.5, Height = 5.25 };
		public static DynPolygon NewPolygon(int id = 2) => new DynPolygon {
			Id = id, Points = new List<(double, double)> { (0, 0), (1, 0), (0.5, 1.25) }
		};

		/// <summary>The root synchronizer used by most dynamic-typing tests: the Shape
		///   field is synchronized dynamically (by runtime type / type tag).</summary>
		public static DynBox SyncBoxDynamic<SM>(SM sm, DynBox? box) where SM : ISyncManager
		{
			sm.CurrentObject = box ??= new DynBox();
			box.Shape = sm.SyncDyn("Shape", box.Shape);
			return box;
		}

		/// <summary>A statically-typed counterpart: the Shape field must be a
		///   DynEllipse and is synchronized with an explicit synchronizer.</summary>
		public static DynBox SyncBoxStaticEllipse<SM>(SM sm, DynBox? box) where SM : ISyncManager
		{
			sm.CurrentObject = box ??= new DynBox();
			box.Shape = sm.Sync("Shape", (DynEllipse?)box.Shape, DynShapeSync<SM>.Sync);
			return box;
		}
	}

	#endregion

	public abstract partial class SyncLibTests<Reader, Writer>
		where Writer : ISyncManager
		where Reader : ISyncManager
	{
		[Test]
		public void DynamicShapeRoundTrip()
		{
			using (DynTestData.UseShapeRegistries()) {
				// The static type of Shape is DynShape; the runtime type round-trips via the tag.
				RoundTripTest(new DynBox { Shape = DynTestData.NewPolygon() },
					DynTestData.SyncBoxDynamic<Writer>, DynTestData.SyncBoxDynamic<Reader>);
				RoundTripTest(new DynBox { Shape = DynTestData.NewEllipse() },
					DynTestData.SyncBoxDynamic<Writer>, DynTestData.SyncBoxDynamic<Reader>);
			}
		}

		[Test]
		public void DynamicListOfShapes()
		{
			using (DynTestData.UseShapeRegistries()) {
				var shapes = new List<DynShape> {
					DynTestData.NewEllipse(1), DynTestData.NewPolygon(2), DynTestData.NewEllipse(3) };
				RoundTripTest<List<DynShape>, DynShape>(shapes,
					(sm, v) => sm.SyncDynList("shapes", v)!,
					(sm, v) => sm.SyncDynList("shapes", v)!);
			}
		}

		[Test]
		public void StaticallyWrittenShapeIsReadableDynamically()
		{
			using (DynTestData.UseShapeRegistries()) {
				// Write with an explicit synchronizer (static tier). The [TypeTag] on the
				// method means the tag is written anyway...
				var box = new DynBox { Shape = DynTestData.NewEllipse() };
				var data = Write(box, DynTestData.SyncBoxStaticEllipse<Writer>, 0);
				// ...so it can be read back dynamically, as the base type:
				Assert.AreEqual(box, Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));

				// The same works when the writer uses a struct synchronizer, whose
				// struct-level [TypeTag] is resolved once per closed generic type:
				data = Write(box, (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Shape = sm.Sync("Shape", (DynEllipse?)v.Shape, default(DynEllipseSync<Writer>));
					return v;
				}, 0);
				Assert.AreEqual(box, Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));
			}
		}

		[Test]
		public void DynamicallyWrittenShapeIsReadableStatically()
		{
			using (DynTestData.UseShapeRegistries()) {
				var box = new DynBox { Shape = DynTestData.NewEllipse() };
				var data = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);
				// The static-tier reader consumes the tag and verifies it matches.
				Assert.AreEqual(box, Read<DynBox>(data, DynTestData.SyncBoxStaticEllipse<Reader>));
			}
		}

		[Test]
		public void StaticReadOfMismatchedTagThrows()
		{
			using (DynTestData.UseShapeRegistries()) {
				var box = new DynBox { Shape = DynTestData.NewPolygon() };
				var data = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);
				// The data says "Polygon" but this reader statically expects an Ellipse;
				// the default TagMismatch policy throws.
				Assert.ThrowsAny<FormatException>(() =>
					Read<DynBox>(data, DynTestData.SyncBoxStaticEllipse<Reader>));
			}
		}

		[Test]
		public void TagMismatchHandlerCanIgnore()
		{
			byte[] data;
			var box = new DynBox { Shape = DynTestData.NewEllipse() };
			using (DynTestData.UseShapeRegistries())
				data = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);  // tagged "Ellipse"

			// DynOvalSync expects the tag "Oval". With a lenient TagMismatch policy,
			// the read proceeds with the expected synchronizer anyway (the field
			// layouts are the same), and the mismatch is merely counted.
			var lenient = new LenientTagRegistry();
			using (TypeTagRegistry.SetDefault(lenient)) {
				var box2 = Read<DynBox>(data, (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Shape = sm.Sync("Shape", (DynEllipse?)v.Shape, DynOvalSync<Reader>.Sync);
					return v;
				});
				Assert.AreEqual(1, lenient.Mismatches);
				Assert.AreEqual(box, box2);
			}
		}

		[Test]
		public void UnknownTagThrows()
		{
			byte[] data;
			using (DynTestData.UseShapeRegistries())
				data = Write(new DynBox { Shape = DynTestData.NewPolygon() }, DynTestData.SyncBoxDynamic<Writer>, 0);

			// These registries know nothing tagged "Polygon"
			var partialTags = new TypeTagRegistry();
			var partialSync = new TypeSyncRegistry();
			using (TypeTagRegistry.SetDefault(partialTags))
				partialSync.Add<DynEllipse>("Ellipse", typeof(DynShapeSync<>));
			using (new AmbientRegistries(partialTags, partialSync))
				Assert.ThrowsAny<FormatException>(() =>
					Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));
		}

		[Test]
		public void UnknownTagHandlerCanSubstitute()
		{
			var (synchronizers, tags) = DynTestData.NewShapeRegistries();
			var box = new DynBox { Shape = DynTestData.NewEllipse() };
			byte[] data;
			using (new AmbientRegistries(tags, synchronizers))
				data = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);  // tagged "Ellipse"

			// This tag registry has an EMPTY dictionary, so "Ellipse" is unknown — but
			// its UnknownTag policy substitutes typeof(DynEllipse) instead of throwing.
			using (new AmbientRegistries(new SubstituteEllipseTagRegistry(), synchronizers))
				Assert.AreEqual(box, Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));
		}

		[Test]
		public void UnregisteredTypeWriteThrows()
		{
			using (new AmbientRegistries(new TypeTagRegistry(), new TypeSyncRegistry()))
				Assert.ThrowsAny<NotSupportedException>(() =>
					Write(new DynBox { Shape = DynTestData.NewEllipse() }, DynTestData.SyncBoxDynamic<Writer>, 0));
		}

		[Test]
		public void NoTypeTagModeRoundTrip()
		{
			using (DynTestData.UseShapeRegistries()) {
				// Statically typed, tag suppressed at the call site (both directions).
				const ObjectMode mode = ObjectMode.Deduplicate | ObjectMode.NoTypeTag;
				var box = new DynBox { Shape = DynTestData.NewEllipse() };
				var data = Write(box, (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Shape = sm.Sync("Shape", (DynEllipse?)v.Shape, DynShapeSync<Writer>.Sync, mode);
					return v;
				}, 0);
				var box2 = Read<DynBox>(data, (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Shape = sm.Sync("Shape", (DynEllipse?)v.Shape, DynShapeSync<Reader>.Sync, mode);
					return v;
				});
				Assert.AreEqual(box, box2);
			}
		}

		[Test]
		public void CyclicPolymorphicGraphWithDeduplication()
		{
			using (DynTestData.UseShapeRegistries()) {
				var a = new DynDuo { Id = 1 };
				var b = new DynDuo { Id = 2, Partner = a };
				a.Partner = b;

				var data = Write(new DynBox { Shape = a }, DynTestData.SyncBoxDynamic<Writer>, 0);
				var box2 = Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>)!;

				var a2 = (DynDuo) box2.Shape!;
				Assert.AreEqual(1, a2.Id);
				var b2 = (DynDuo) a2.Partner!;
				Assert.AreEqual(2, b2.Id);
				Assert.AreSame(a2, b2.Partner); // the cycle was reconstructed, not duplicated
			}
		}

		[Test]
		public void LateRegistrationIsPickedUp()
		{
			var registry = new TypeSyncRegistry();
			using (new AmbientRegistries(new TypeTagRegistry(), registry)) {
				var box = new DynBox { Label = new DynLabel { Text = "hi" } };
				SyncObjectFunc<Writer, DynBox> syncW = (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Label = sm.SyncAny("Label", v.Label);  // no synchronizer given: registry-driven
					return v;
				};
				SyncObjectFunc<Reader, DynBox> syncR = (sm, v) => {
					sm.CurrentObject = v ??= new DynBox();
					v.Label = sm.SyncAny("Label", v.Label);
					return v;
				};

				// DefaultSynchronizer has no synchronizer for DynLabel yet...
				Assert.ThrowsAny<NotSupportedException>(() => Write(box, syncW, 0));

				// ...but a registration made after that failure is found by the next call.
				registry.Add<DynLabel>("Label", (ISyncManager sm, DynLabel? v) => {
					sm.CurrentObject = v ??= new DynLabel();
					v.Text = sm.Sync("Text", v.Text);
					return v;
				});
				var data = Write(box, syncW, 0);
				Assert.AreEqual(box, Read<DynBox>(data, syncR));
			}
		}

		[Test]
		public void RegistrySwapChangesTags()
		{
			var (syncA, tagsA) = DynTestData.NewShapeRegistries();
			var tagsB = new TypeTagRegistry();
			var syncB = new TypeSyncRegistry();
			using (TypeTagRegistry.SetDefault(tagsB))
				syncB.Add<DynEllipse>("E2", typeof(DynShapeSync<>));

			var box = new DynBox { Shape = DynTestData.NewEllipse() };
			byte[] data;
			using (new AmbientRegistries(tagsA, syncA))
				data = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);

			// Registry pair B uses a different tag for the same type, so the data is foreign to it
			using (new AmbientRegistries(tagsB, syncB)) {
				Assert.ThrowsAny<FormatException>(() =>
					Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));

				byte[] dataB = Write(box, DynTestData.SyncBoxDynamic<Writer>, 0);
				Assert.AreEqual(box, Read<DynBox>(dataB, DynTestData.SyncBoxDynamic<Reader>));
			}

			// SetDefault restored the previous ambient registries when disposed
			using (new AmbientRegistries(tagsA, syncA))
				Assert.AreEqual(box, Read<DynBox>(data, DynTestData.SyncBoxDynamic<Reader>));
		}

		[Test]
		public void ExplicitRegistriesBypassAmbient()
		{
			// The registries are passed explicitly to SyncDynamic; the ambient
			// defaults are never swapped and never consulted.
			var (synchronizers, tags) = DynTestData.NewShapeRegistries();
			var box = new DynBox { Shape = DynTestData.NewPolygon() };
			var data = Write(box, (sm, v) => {
				sm.CurrentObject = v ??= new DynBox();
				v.Shape = sm.SyncDyn("Shape", v.Shape, synchronizers, tags);
				return v;
			}, 0);
			var box2 = Read<DynBox?>(data, (sm, v) => {
				sm.CurrentObject = v ??= new DynBox();
				v.Shape = sm.SyncDyn("Shape", v.Shape, synchronizers, tags);
				return v;
			});
			Assert.AreEqual(box, box2);
		}

		#region Built-in default synchronizers (DefaultSynchronizer for collections etc.)

		[Test]
		public void DefaultSyncOfArraysAndLists()
		{
			RoundTripTest<int[], int>(new[] { 3, -1, int.MaxValue },
				(sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v));
			RoundTripTest<List<string>, string>(new List<string> { "one", "", "three" },
				(sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v));
			RoundTripTest<byte[], byte>(new byte[] { 1, 2, 255, 0 },
				(sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v));
			RoundTripTest<List<int[]>>(new List<int[]> { new[] { 1 }, new int[0] },
				(sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v),
				0, (a, b) => {
					Assert.AreEqual(a!.Count, b.Count);
					for (int i = 0; i < b.Count; i++)
						ExpectList(a[i], b[i]);
				});
		}

		[Test]
		public void DefaultSyncOfDictionaryAndHashSet()
		{
			var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };
			RoundTripTest(dict, (sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v),
				0, (a, b) => {
					Assert.AreEqual(a!.Count, b.Count);
					foreach (var pair in b)
						Assert.AreEqual(pair.Value, a[pair.Key]);
				});

			var set = new HashSet<int> { 5, 7, 11 };
			RoundTripTest(set, (sm, v) => sm.SyncAny("x", v), (sm, v) => sm.SyncAny("x", v),
				0, (a, b) => Assert.IsTrue(a!.SetEquals(b)));
		}

		[Test]
		public void DefaultSyncOfDatesEnumsAndPairs()
		{
			var syncW = (SyncObjectFunc<Writer, (DateTime, DateTime, TimeSpan, DayOfWeek, KeyValuePair<string, int>)>)
				((sm, v) => sm.SyncAny("x", v));
			var syncR = (SyncObjectFunc<Reader, (DateTime, DateTime, TimeSpan, DayOfWeek, KeyValuePair<string, int>)>)
				((sm, v) => sm.SyncAny("x", v));
			var value = (
				new DateTime(2026, 7, 11, 13, 45, 0),
				new DateTime(2026, 7, 11, 13, 45, 0, 123).AddTicks(4567),
				new TimeSpan(1, 2, 3, 4, 5),
				DayOfWeek.Friday,
				new KeyValuePair<string, int>("price", 42));
			RoundTripTest(value, syncW, syncR);
		}

		#endregion
	}

	/// <summary>Dynamic typing tests specific to the JSON format (tag representation
	///   in the output, and reading foreign JSON that lacks type tags).</summary>
	public class SyncDynamicJsonTests : TestHelpers
	{
		static string WriteShapeJson(DynShape shape, SyncJson.Options? options = null)
			=> Encoding.UTF8.GetString(SyncJson.Write<DynBox>(new DynBox { Shape = shape },
				DynTestData.SyncBoxDynamic<SyncJson.Writer>, options).ToArray());

		[Test]
		public void TypeTagAppearsAsTabProperty()
		{
			using (DynTestData.UseShapeRegistries()) {
				// In non-Newtonsoft mode the tag property is named "\t"
				var options = new SyncJson.Options { NewtonsoftCompatibility = false };
				var json = WriteShapeJson(DynTestData.NewEllipse(), options);
				Assert.IsTrue(json.Contains("\"\\t\""), "expected a \\t property in: " + json);
				Assert.IsTrue(json.Contains("\"Ellipse\""), json);
			}
		}

		[Test]
		public void TypeTagUsesDollarTypeInNewtonsoftMode()
		{
			using (DynTestData.UseShapeRegistries()) {
				// NewtonsoftCompatibility is on by default, and names the tag "$type"
				var options = new SyncJson.Options();
				var json = WriteShapeJson(DynTestData.NewEllipse(), options);
				Assert.IsTrue(json.Contains("\"$type\""), "expected a $type property in: " + json);

				var box = SyncJson.Read<DynBox>(Encoding.UTF8.GetBytes(json),
					DynTestData.SyncBoxDynamic<SyncJson.Reader>, options);
				Assert.AreEqual(DynTestData.NewEllipse(), box!.Shape);
			}
		}

		[Test]
		public void NoTypeTagModeOmitsTheTag()
		{
			using (DynTestData.UseShapeRegistries()) {
				var json = Encoding.UTF8.GetString(SyncJson.Write<DynBox>(
					new DynBox { Shape = DynTestData.NewEllipse() },
					(sm, v) => {
						sm.CurrentObject = v ??= new DynBox();
						v.Shape = sm.Sync("Shape", (DynEllipse?)v.Shape, DynShapeSync<SyncJson.Writer>.Sync,
							ObjectMode.Deduplicate | ObjectMode.NoTypeTag);
						return v;
					}).ToArray());
				Assert.IsFalse(json.Contains("\\t"), "expected no \\t property in: " + json);
				Assert.IsFalse(json.Contains("$type"), "expected no $type property in: " + json);
				Assert.IsFalse(json.Contains("Ellipse"), json);
			}
		}

		[Test]
		public void ForeignJsonWithoutTagFallsBackToStaticType()
		{
			using (DynTestData.UseShapeRegistries()) {
				// Hand-written JSON with no "\t" property: when the expected type itself
				// is registered, SyncDynamic falls back to it.
				var json = @"{ ""Shape"": { ""Id"": 1, ""Width"": 10.5, ""Height"": 5.25 } }";
				var box = SyncJson.Read<DynBox>(Encoding.UTF8.GetBytes(json), (sm, v) => {
					v ??= new DynBox();
					v.Shape = sm.SyncDyn<SyncJson.Reader, DynEllipse>("Shape", null);
					return v;
				});
				Assert.AreEqual(DynTestData.NewEllipse(), box!.Shape);
			}
		}
	}
}
