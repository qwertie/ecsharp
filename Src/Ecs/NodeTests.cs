using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	using S = CodeSymbols;
	using Loyc.Utilities;
	using ecs;
	using System.Diagnostics;

	[TestFixture]
	public class NodeTests : Assert
	{
		[Test]
		public void Positions()
		{
			ISourceFile source = new EmptySourceFile("{source}.cs");
			ISourceFile source2 = new EmptySourceFile("{source2}.cs");
			var F = new GreenFactory(source);
			// Simulate: 0       8         17        27
			//           Console.WriteLine("Hello!");
			var Console = F.Symbol("Console", 7);
			var WriteLine = F.Symbol("WriteLine", 9);
			var Hello = F.Literal("Hello!", 8);
			var dot = F.Call(new GreenAtOffs(F.Symbol(S.Dot), 7), new GreenAtOffs(Console, 0), new GreenAtOffs(WriteLine, 8));
			var stmt = F.Call(new GreenAtOffs(dot, 0), new GreenAtOffs(Hello, 17), 27);
			// 0    5   9
			// void f() {
			// 11  Console.WriteLine("Hello!");
			// 41  Console.WriteLine("Hello!");
			// }72
			var gbody = F.List(new[] { new GreenAtOffs(stmt, 12-9), new GreenAtOffs(stmt, 42-9) }, 72-9);
			var def = F.Def(new GreenAtOffs(F.Symbol("f"), 5), 
			                new GreenAtOffs(F.EmptyList, 6), 
			                new GreenAtOffs(F.Void, 0),
			                new GreenAtOffs(gbody, 9), 72);

			var root = Node.NewFromGreen(def, 0);
			AreEqual(root.SourceRange.Source, source);
			AreEqual(root.SourceRange.BeginIndex, 0);
			AreEqual(root.SourceRange.Length, 72);
			var body = root.Args[3];
			AreEqual(body.SourceRange.Source, source);
			AreEqual(body.SourceRange.BeginIndex, 9);
			AreEqual(body.SourceRange.Length, 72-9);
			var stmt0 = body.Args[0];
			var stmt1 = body.Args[1];
			AreNotSame(stmt0, stmt1);
			AreSame(stmt0, body.Args[0]); // same when not frozen
			AreEqual(stmt0.SourceRange.Source, source);
			AreEqual(stmt0.SourceRange.BeginIndex, 12);
			AreEqual(stmt1.SourceRange.BeginIndex, 42);
			AreEqual(stmt0.SourceRange.Length, 27);
			AreEqual(stmt1.SourceRange.Length, 27);
			AreEqual(stmt1.Head.Args[0].SourceRange, new SourceRange(source, 42, 7));
			AreEqual(stmt1.Head.Head.SourceRange, new SourceRange(source, 42 + 7, 1));
			
			AreSame(stmt1, body.Args.Detach(1));
			AreEqual(stmt1.SourceRange.BeginIndex, 42);
			AreEqual(stmt1.SourceRange.Length, 27);
			body.Args.Add(stmt1);
			AreSame(stmt1, body.Args[1]);
			AreSame(stmt1.Parent, body);
			
			stmt1.Detach();
			AreEqual(body.ArgCount, 1);
			AreSame(stmt1.Parent, null);
			body.Args.Insert(0, stmt1);
			AreEqual(stmt0.SourceRange.BeginIndex, 12);
			AreEqual(stmt1.SourceRange.BeginIndex, 42);
			AreEqual(stmt0.SourceRange.Length, 27);
			AreEqual(stmt1.SourceRange.Length, 27);
			AreEqual(stmt0.IndexInParent, 1);

			// Detach/attach is also possible while frozen.
			stmt0.Freeze();
			stmt0.Detach();
			body.Args.Insert(0, stmt0);
			AreEqual(stmt0.IndexInParent, 0);
			AreEqual(stmt0.SourceRange.BeginIndex, 12);
			AreEqual(stmt0.SourceRange.Length, 27);
			IsTrue(stmt0.Args[0].IsLiteral);
			AreEqual(stmt0.Args[0].SourceRange, new SourceRange(source, 12+17, 8));

			// We can detach a head, and the parent will retain the Name.
			var fullName = stmt1.Head;
			AreEqual(fullName.ToString(), "Console.WriteLine");
			fullName.Detach();
			AreEqual(fullName.ToString(), "Console.WriteLine");
			AreSame(stmt1.Head, null);
			AreSame(stmt1.Name, S.Dot);
			AreSame(fullName.Parent, null);
			// Clone is unaffected.
			AreEqual(stmt0.HeadOrThis.Name, S.Dot);
			AreEqual(stmt0.HeadOrThis.ArgCount, 2);
			AreEqual(stmt0.HeadOrThis.ToString(), "Console.WriteLine");

			// You can attach a node from another file, and it remembers its file.
			var @return = Node.NewFromGreen(F.Symbol(S.Return, 7), 1);
			body.Args.Add(@return);
			AreSame(@return.Parent, body);
			AreNotSame(@return.SourceRange.Source, body.SourceRange.Source);
			AreEqual(@return.SourceRange.Source, source2);
			AreEqual(@return.SourceRange.BeginIndex, 1);
			AreEqual(@return.SourceRange.Length, 7);
		}
	}
}
