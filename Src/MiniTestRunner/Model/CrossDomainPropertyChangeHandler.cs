using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using MiniTestRunner.TestDomain;
using System.Diagnostics;
using System.Reflection;

namespace MiniTestRunner
{
	/// <summary>Allows a non-MarshalByRefObject to subscribe to PropertyChanged on 
	/// an object in another AppDomain. The CLR may pretend to allow us to subscribe 
	/// to the event directly, but when the event fires, it goes to a useless COPY 
	/// of the subscriber that was silently created in the other AppDomain.</summary>
	public class CrossDomainPropertyChangeHelper : MarshalByRefObject//, ISponsor
	{
		PropertyChangedDelegate _handler;
		IPropertyChanged _target;

		public CrossDomainPropertyChangeHelper(IPropertyChanged target, PropertyChangedDelegate handler)
		{
			Debug.Assert(RemotingServices.IsTransparentProxy(target));
			_handler = handler;
			target.PropertyChanged += Intermediary;
			
			//((ILease)RemotingServices.GetLifetimeService(this)).Register(this); // register self as sponsor
		}

		public void Intermediary(object sender, string prop)
		{
			_handler(sender, prop);
		}

		//public TimeSpan Renewal(ILease lease)
		//{
		//    return TimeSpan.FromSeconds(60); //_handler.IsAlive ? TimeSpan.FromSeconds(60) : TimeSpan.Zero;
		//}
	}

	//class Helper<Class, T>
	//{
	//    Action<Class, T> openDelegate;
	//    WeakReference<Class>
	//}
	//class WeakAction<T>
	//{
	//    // Want to create a dynamic method of the form:
	//    R Forwarder(Class self, T1 arg1, T2 arg2, ...)
	//    {
	//        [return] self.UserMethod(arg1, arg2, ...);
	//    }
	//}

	//class WeakEventHandler<EventArgs>
	//{
	//}

	//class WeakDelegate<D> : WeakReference where D : class // where D : Delegate
	//{
	//    public WeakDelegate(D d) : base(((Delegate)d).Target)
	//    {
	//    }

	//    public static operator+ (WeakDelegate<D> one, WeakDelegate<D> two)
	//    {
	//        return new WeakDelegate<D>(one, two);
	//    }
	//}
}
