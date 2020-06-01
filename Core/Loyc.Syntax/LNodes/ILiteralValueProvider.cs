namespace Loyc.Syntax
{
	/// <summary>The intention of this interface is that a <c>struct</c> implementing 
	/// it can be embedded inside a <see cref="LiteralNode"/> in order to provide not
	/// just a Value property, but also a TextValue and a TypeMarker.</summary>
	/// <remarks>Each method here is given the value of <see cref="LNode.Range"/> so that 
	/// the provider has access to the original source code range. This makes it possible
	/// for the literal value provider to save memory, e.g. by not storing a reference to
	/// the original source text when it is known that the <see cref="SourceRange"/> 
	/// already has the needed reference.
	/// <para/>
	/// Also to save memory, implementations of this interface are usually structs.
	/// </remarks>
	public interface ILiteralValueProvider
	{
		/// <summary><see cref="LNode.Value"/> returns whatever this returns.</param>
		object GetValue(SourceRange range);
		/// <summary><see cref="LNode.TextValue"/> returns whatever this returns.</param>
		UString GetTextValue(SourceRange range);
		/// <summary><see cref="LNode.TypeMarker"/> returns whatever this returns.</param>
		Symbol GetTypeMarker(SourceRange range);
	}
}
