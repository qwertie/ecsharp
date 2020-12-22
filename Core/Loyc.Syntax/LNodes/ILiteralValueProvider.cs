namespace Loyc.Syntax
{
	/// <summary>The intention of this interface is that a <c>struct</c> implementing 
	/// it can be embedded inside a <see cref="LiteralNode"/> in order to lazily obtain
	/// the text of a literal, or even parse it lazily.</summary>
	/// <remarks>Each method here is given the value of <see cref="LNode.Range"/> so that 
	/// the provider has access to the original source code text.</remarks>
	public interface ILiteralValueProvider
	{
		/// <summary><see cref="LNode.Value"/> returns whatever this returns.</summary>
		object GetValue(SourceRange range);
		/// <summary><see cref="LNode.TextValue"/> returns whatever this returns.</summary>
		UString GetTextValue(SourceRange range);
		/// <summary><see cref="LNode.TypeMarker"/> returns whatever this returns.</summary>
		Symbol GetTypeMarker(SourceRange range);
	}
}
