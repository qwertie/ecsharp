using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.LLParserGenerator
{
	public abstract class PredVisitor
	{
		public void Visit(Pred pred) { pred.Call(this); }
		public virtual void Visit(TerminalPred term) { VisitOther(term); }
		public virtual void Visit(RuleRef rref)     { VisitOther(rref); }
		public virtual void Visit(Seq seq)          { VisitOther(seq); }
		public virtual void Visit(Alts alts)        { VisitOther(alts); }
		public virtual void Visit(AndPred and)      { VisitOther(and); }
		public virtual void Visit(Gate gate)        { VisitOther(gate); }
		public virtual void Visit(EndOfRule end)    { VisitOther(end); }
		public virtual void VisitOther(Pred pred) { }
		public void VisitChildrenOf(Seq pred) { foreach (var p in pred.List) p.Call(this); }
		public void VisitChildrenOf(AndPred pred) { if (pred.Pred is Pred) (pred.Pred as Pred).Call(this); }
		public void VisitChildrenOf(Gate pred) { pred.Predictor.Call(this); pred.Match.Call(this); }
		public void VisitChildrenOf(Alts pred, bool includeError) 
		{
			foreach (var p in pred.Arms) p.Call(this);
			if (includeError && pred.ErrorBranch != null && pred.ErrorBranch != DefaultErrorBranch.Value)
				pred.ErrorBranch.Call(this);
		}
	}
	public abstract class RecursivePredVisitor : PredVisitor
	{
		public override void Visit(Seq pred)     { VisitChildrenOf(pred); }
		public override void Visit(Alts pred)    { VisitChildrenOf(pred, true); }
		public override void Visit(AndPred pred) { VisitChildrenOf(pred); }
		public override void Visit(Gate pred)    { VisitChildrenOf(pred); }
	}
}
