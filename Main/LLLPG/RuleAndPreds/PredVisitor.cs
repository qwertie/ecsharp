using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.LLParserGenerator
{
	/// <summary>Base class for implementing a visitor that examines a tree of LLLPG <see cref="Pred"/>icates.</summary>
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

	/// <summary>Base class for implementing a visitor that examines a tree of LLLPG 
	/// <see cref="Pred"/>icates. The default implementation of Visit(P) for each
	/// predicate type P recursively visits the children of the P.</summary>
	public abstract class RecursivePredVisitor : PredVisitor
	{
		public override void Visit(Seq pred)     { VisitOther(pred); VisitChildrenOf(pred); }
		public override void Visit(Alts pred)    { VisitOther(pred); VisitChildrenOf(pred, true); }
		public override void Visit(AndPred pred) { VisitOther(pred); VisitChildrenOf(pred); }
		public override void Visit(Gate pred)    { VisitOther(pred); VisitChildrenOf(pred); }
	}

	/// <summary>Base class for visitors that can replace predicates entirely.</summary>
	/// <remarks>Only used by <see cref="LLParserGenerator.ApplyInlines"/></remarks>
	public abstract class RecursiveReplacementPredVisitor : PredVisitor
	{
		public override void Visit(Seq pred)     { VisitOther(pred); ReplaceChildrenOf(pred); }
		public override void Visit(Alts pred)    { VisitOther(pred); ReplaceChildrenOf(pred, true); }
		public override void Visit(AndPred pred) { VisitOther(pred); ReplaceChildrenOf(pred); }
		public override void Visit(Gate pred)    { VisitOther(pred); ReplaceChildrenOf(pred); }
			
		protected Pred Replacement { get; set; }

		public virtual void ReplaceChildrenOf(Seq pred)
		{
			VisitAndReplace(pred.List);
		}
		public virtual void ReplaceChildrenOf(AndPred pred)
		{
			if (pred.Pred is Pred) {
				var child = (pred.Pred as Pred);
				if (VisitAndReplace(ref child))
					pred.Pred = child;
			}
		}
		public virtual void ReplaceChildrenOf(Gate pred) 
		{
			VisitAndReplace(ref pred.Predictor);
			VisitAndReplace(ref pred.Match);
		}
		public virtual void ReplaceChildrenOf(Alts pred, bool includeError) 
		{
			VisitAndReplace(pred.Arms);
			if (includeError && pred.ErrorBranch != null && pred.ErrorBranch != DefaultErrorBranch.Value)
				VisitAndReplace(ref pred.ErrorBranch);
		}
		private void VisitAndReplace(IList<Pred> list)
		{
			for (int i = 0; i < list.Count; i++) {
				var child = list[i];
				if (VisitAndReplace(ref child))
					list[i] = child;
			}
		}
		private bool VisitAndReplace(ref Pred p)
		{
			Debug.Assert(Replacement == null);
			p.Call(this);
			if (Replacement != null) {
				p = Replacement;
				Replacement = null;
				return true;
			}
			return false;
		}
	}
}
