// Generated from CharCategory.ecs by LeMP custom tool. LeMP version: 2.8.4.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//string propList_txt = includeFileText("PropList.txt");
namespace Loyc.Syntax.Lexing
{
	class CharCategory
	{
		//precompute(categoryTable.Select(c => quote(( $(LNode.Literal(c.From)), $(LNode.Literal(c.To)), UnicodeCategory.$(LNode.Id(c.Category.ToString())) ))));
		void f() { }
	}
}
/*
	base2    base5 base8 base10 base12 base16 base36 base100
	0        0     0     0      0      0      0      0
	1        1     1     1      1      1      1      1
	10       2     2     2      2      2      2      2
	11       3     3     3      3      3      3      3
	100      4     4     4      4      4      4      4
	101      10    5     5      5      5      5      5
	110      11    6     6      6      6      6      6
	111      12    7     7      7      7      7      7
	1000     13    10    8      8      8      8      8
	1001     14    11    9      9      9      9      9
	1010     20    12    10     ͻ      ͻ      A      A
	1011     21    13    11     ͼ      ͼ      B      B
	1100     22    14    12     10     გ      C      C
	1101     23    15    13     11     დ      D      D
	1110     24    16    14     12     ე      E      E
	1111     30    17    15     13     ვ      F      F
	10000    31    20    16     14     10     G      G
	10001    32    21    17     15     11     H      H
	10010    33    22    18     16     12     I      I

	100011   120   43    35     2ͼ     19     Z      Z
	100100   121   44    36     30     1A     10     
	                     1234          64            10

1/3 = 0.3333333333333333333333333333333333333333333333333333333333333333333333333333333333333
1/3 = 0.4(base12)
1/4 = 0.3(base12)
1/2 = 0.6(base12)
2/3 = 0.8(base12)
3/4 = 0.9(base12)
5/6 = 0.ͻ(base12)
1/6 = 0.2(base12)
1/5 = 0.24972497249724972497249724972497249724972497249724972497249724972497249724972497249724972497249724972497249724972497249724972497(base12)



5-digit number in base X: EDCBA

X=8
EDCBA=12345
E*X^4 + D*X^3 + C*X^2 + B*X^1 + A*X^0
1*8*8*8*8 + 2*8*8*8 + 3*8*8 + 4*8 + 5









































*/