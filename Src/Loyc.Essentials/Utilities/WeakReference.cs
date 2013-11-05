using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Loyc
{
	/// <summary>
	/// Adds strong typing to WeakReference.Target using generics.
	/// </summary>
	[Serializable]
	public class WeakReference<T> : WeakReference where T : class
	{
		/// <summary>Returns a new WeakReference except that if target is null,
		/// the WeakNullReference Singleton is returned instead.</summary>
		public static WeakReference<T> NewOrNullSingleton(T target)
		{
			if (target == null)
				return WeakNullReference<T>.Singleton;
			return new WeakReference<T>(target);
		}

		public WeakReference(T target) : base(target) { }
		public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection) { }
		#if !CompactFramework && !WindowsCE
		protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context) {}
		#endif

		public new T Target
		{
			get { return (T)base.Target; }
			set {
				if (this != WeakNullReference<T>.Singleton)
					base.Target = value;
				else if (value != null)
					throw new InvalidOperationException("Cannot change target of WeakNullReference<T>.Singleton");
			}
		}
	}

	/// <summary>Provides a weak reference to a null target object, which, unlike
	/// other weak references, is always considered to be alive. This facilitates,
	/// for instance, handling null dictionary values in WeakValueDictionary.</summary>
	public class WeakNullReference<T> : WeakReference<T> where T : class
	{
		public static readonly WeakNullReference<T> Singleton = new WeakNullReference<T>();

		private WeakNullReference() : base(null) { }

		public override bool IsAlive
		{
			get { return true; }
		}
	}
}
