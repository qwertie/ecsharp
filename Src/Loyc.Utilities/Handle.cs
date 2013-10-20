namespace Loyc.Utilities
{
	struct Handle
	{
		Handle(long id) { Id = id; }
		public long Id;

		public bool IsNull { get { return Id == 0; } }
		public static readonly Handle Null = new Handle();

		public override string ToString() { return Id.ToString(); }
		public override bool Equals(object b) { return base.Equals(b); }
		public override int GetHashCode() { return (int)Id; }

		public static bool operator ==(Handle a, Handle b) { return a.Id == b.Id; }
		public static bool operator !=(Handle a, Handle b) { return a.Id != b.Id; }
	}
}
