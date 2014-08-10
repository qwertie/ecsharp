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
	public class CrossDomainPropertyChangeHelper : MarshalByRefObject, ISponsor, IDisposable
	{
		PropertyChangedDelegate _handler;

		/// <summary>Performs the logical operation "target.PropertyChanged += handler",
		/// where 'target' is located in another AppDomain, using this helper as an
		/// intermediary so that the subscription works correctly.</summary>
		public CrossDomainPropertyChangeHelper(IPropertyChanged target, PropertyChangedDelegate handler)
		{
			Debug.Assert(RemotingServices.IsTransparentProxy(target));
			_handler = handler;
			target.PropertyChanged += Intermediary;
			
			// Honestly I don't know what I am doing. Tried to figure it out, failed.
			((ILease)RemotingServices.GetLifetimeService(this)).Register(this); // register self as sponsor
		}

		public void Intermediary(object sender, string prop)
		{
			if (_handler != null)
				_handler(sender, prop);
		}
		public TimeSpan Renewal(ILease lease)
		{
			return _handler != null ? TimeSpan.FromSeconds(60) : TimeSpan.Zero;
		}
		public void Dispose()
		{
			_handler = null;
		}
	}
}
