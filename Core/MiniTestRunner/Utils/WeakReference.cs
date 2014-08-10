using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniTestRunner
{
    // Adds strong typing to WeakReference.Target using generics. Also,
    // the Create factory method is used in place of a constructor
    // to handle the case where target is null, but we want the 
    // reference to still appear to be alive.
    public class WeakReference<T> : WeakReference where T : class
    {
        public static WeakReference<T> Create(T target)
        {
            if (target == null)
                return WeakNullReference<T>.Singleton;

            return new WeakReference<T>(target);
        }

        protected WeakReference(T target)
            : base(target, false) { }

        public new T Target
        {
            get { return (T)base.Target; }
        }
    }

	// Provides a weak reference to a null target object, which, unlike
	// other weak references, is always considered to be alive. This 
	// facilitates handling null dictionary values, which are perfectly
	// legal.
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
