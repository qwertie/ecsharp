using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;

namespace Loyc.Tests
{
	public class LogTest3
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(LogTest3));
	
		static LogTest3()
		{
			XmlConfigurator.Configure();
		}

		public static void Main(string[] args)
		{
			LogTest3 log3 = new LogTest3();
			log3.DoLogging();
			LogTest4 log4 = new LogTest4();
			log4.DoLogging();
		}
	
		public void DoLogging()
		{
			logger.Debug("Here is a debug log from LogTest3.");
			logger.Info("... and an Info log from LogTest3.");
		}
	}

	public class LogTest4
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(LogTest4));
	
		static LogTest4()
		{
			DOMConfigurator.Configure();
		}

		public void DoLogging()
		{
			logger.Debug("Here is a debug log from LogTest4.");
			logger.Info("... and an Info log from LogTest4.");
		}

	}
}
