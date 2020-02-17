using Loyc.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;

namespace Loyc
{
	/// <summary>Adds extension methods to modernize .NET's simple built-in service locator.</summary>
	/// <remarks>
	/// .NET has had a litte-known built-in simple service locator since .NET 1.1 called
	/// <see cref="System.ComponentModel.Design.ServiceContainer"/>, which implements both 
	/// <see cref="IServiceContainer"/> and <see cref="IServiceProvider"/>. 
	/// IServiceContainer lets you add services (associating a Type object with a factory or 
	/// a singleton):
	/// <pre>
	/// interface IServiceContainer {
	///     void AddService(Type serviceType, object serviceInstance);
	///     void AddService(Type serviceType, ServiceCreatorCallback callback);
	///     ...
	/// }
	/// delegate object ServiceCreatorCallback(IServiceContainer container, Type serviceType);
	/// </pre>
	/// 
	/// Meanwhile, IServiceProvider is a simple service locator:
	/// <pre>
	///	public interface IServiceProvider
	///	{
	///		object GetService(Type serviceType);
	///	}
	/// </pre>
	/// 
	/// This class adds three extension methods to make IServiceContainer and IServiceProvider 
	/// generic, so you can write code like
	/// <pre>
	///    var services = new ServiceContainer();
	///    services.AddService&lt;IFoo>(p => new Foo());
	///    
	///    // elsewhere in your code...
	///    IFoo newFoo = services.GetService&lt;IFoo>();
	/// </pre>
	/// 
	/// Remember that service locators are generally an antipattern! However, if you have one
	/// of those rare, legitimate needs to use one, you should prefer the IServiceProvider 
	/// interface built into .NET (if it is enough for your needs) so that your lower-level 
	/// code avoids taking an unnecessary dependency on an IoC framework.
	/// <para/>
	/// See also: http://core.loyc.net/essentials/ambient-service-pattern.html
	/// <para/>
	/// Thanks to <a href="http://blog.differentpla.net/blog/2011/12/20/did-you-know-that-net-already-had-an-ioc-container">
	/// Roger's blog</a> for the idea of these extension methods.
	/// </remarks>
	[Obsolete("I have a hypothesis that no one is using this and that deprecation won't hurt (am I wrong?)")]
	public static class ServiceProvider
	{
		public static TInterface GetService<TInterface>(this IServiceProvider provider)
		{
			return (TInterface)provider.GetService(typeof(TInterface));
		}

		public static void AddService<TInterface>(this IServiceContainer container, Func<IServiceProvider, TInterface> factory)
		{
			container.AddService(typeof(TInterface), (c, type) => factory(c));
		}
		public static void RemoveService<TInterface>(this IServiceContainer container)
		{
			container.RemoveService(typeof(TInterface));
		}
	}
}
