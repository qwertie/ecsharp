using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecs.Parser
{
	using LS = EcsTokenTypes;

	public partial class EcsParser
	{
	}

	// +-------------------------------------------------------------+
	// | (allowed as attrs.) | (not allowed as keyword attributes)   |
	// | Modifier keywords   | Statement names | Types** | Other***  |
	// |:-------------------:|:---------------:|:-------:|:---------:|
	// | abstract            | break           | bool    | operator  |
	// | const               | case            | byte    | sizeof    |
	// | explicit            | checked         | char    | typeof    |
	// | extern              | class           | decimal |:---------:|
	// | implicit            | continue        | double  | else      |
	// | internal            | default         | float   | catch     |
	// | new*                | delegate        | int     | finally   |
	// | override            | do              | long    |:---------:|
	// | params              | enum            | object  | in        |
	// | private             | event           | sbyte   | as        |
	// | protected           | fixed           | short   | is        |
	// | public              | for             | string  |:---------:|
	// | readonly            | foreach         | uint    | base      |
	// | ref                 | goto            | ulong   | false     |
	// | sealed              | if              | ushort  | null      |
	// | static              | interface       | void    | true      |
	// | unsafe              | lock            |         | this      |
	// | virtual             | namespace       |         |:---------:|
	// | volatile            | return          |         | stackalloc|
	// | out*                | struct          |         |           |
	// |                     | switch          |         |           |
	// |                     | throw           |         |           |
	// |                     | try             |         |           |
	// |                     | unchecked       |         |           |
	// |                     | using           |         |           |
	// |                     | while           |         |           |
	// +-------------------------------------------------------------+
}
