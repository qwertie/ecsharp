{-
    This is an "Enhanced" parser for lambda terms by David Piepgrass.
    It supports everything you need for assignment 4, plus many things you don't.
    
    The definition of a lambda term is given in this file as follows: 
    
    data LTerm = Var String       -- bound variable OR operator name
               | Abs String LTerm -- anonymous function (\string. lterm)
               | App LTerm LTerm  -- application (fn arg)
               | Literal Value    -- any literal (2, 2.5, True, '2', "two")
               | If LTerm LTerm LTerm  -- if c then t else f
               | Case LTerm CasePattern -- case expr of casePattern
               deriving (Eq, Show)
    
    *** For assignment 4, I have added parsing for case expressions like:
        "case P of (x,y) -> expr1"
        "case P of  x,y  -> expr1"
        "case N of    0 -> expr1; succ S -> expr2"
        "case N of zero -> expr1; succ S -> expr2"
        "case L of  nil -> expr1; x:xs -> expr2"
        "case L of  nil -> expr1; cons v vs -> expr2"
        "case L of  nil -> expr1; cons v -> expr2"
        However, please note that the nil or zero case MUST be written first.
        Go ahead, try (parseExpr "case p of (x,y) -> z") in ghci. Right now.
        Also the comma operator has been added (for pairs), and is treated the 
        same as the binary operators like * + etc. You can also parse "()".
    
    The parser produces output of type LTerm. It recognizes:
    
    - Most of the C/C++ binary operators (except . -> ::)
    - Some unary operators (~ - !)
    - The cons operator ":"
    - The if-then expression: if c then t else f
    - Case expressions for lists, integers, (), and pairs
    
    It also can parse various literals:
    
    - Ints (IntVal i)
    - Floats (FloatVal f)
    - Chars (CharVal c)
    - Boolean "true" and "false" (BoolVal b)
    - Strings (ListVal s) where s consists of CharVals
    - nil and "" both represent empty lists.
    
    DON'T WORRY: For Assignment 4, obviously you don't have to support
    everything that the parser supports. In fact, not even my OWN submission
    will support everything! - DP
    
    Escape sequences are not supported in char and string literals.
    
    Other features:
    - instead of writing "fn x y", the parser allows you to write 
      "x `fn` y". The output is the same either way:
        App (App (Var "fn") (Var "x")) (Var "y")
    - instead of writing "\x. (\y. x y)", you can write "\x y. x y".
      You are also allowed to write "\x y -> x y". Again, the syntax
      tree is the same in any case:
        Abs "x" (Abs "y" (App (Var "x") (Var "y")))
    - this can be parsed: λx. x ≤ 5
        (doesn't seem to work when reading from a file??)
        Abs "x" (App (App (Var "<=") (Var "x")) (Literal (IntVal 5)))
    - Single-line Haskell or C-style comments: // ex1, -- ex2
    
    Unary operators like -5 are mapped to a syntax tree like this:
        App (Var "-") (Literal (IntVal 5))
        (oops, I just noticed this is sort of partially ambiguous with x-y)
    Binary operators like 7+3 are mapped to a syntax tree like this:
        App (App (Var "+") (Literal (IntVal 7))) (Literal (IntVal 3))
    After parsing, you can use pattern matching to detect binary operators:
    
    case expr of
    (App fn arg) -> 
        let defaultResult = ... in
        case fn of
        (App (Var op) arg0) ->
            case op of
            "+" -> -- add      arg0 + arg
            "-" -> -- subtract arg0 - arg
            "*" -> -- multiply arg0 * arg
            ...
            _   -> defaultResult -- no operator detected (bound variable?)
        _ -> defaultResult -- normal application
    
    *****************
    ***** USAGE *****
    *****************
    - Import ParserA4.hs into your program with "import ParserA4"
    - Call (parseExpr "\\x. x+1") to parse the specified expression into
        Either ParseError LTerm
      You can unpack the "Either" using code like
        case (parseExpr str) of 
        Right term -> -- do something with the lambda term
        Left err   -> error (show err)
    - Call (parseWithFN expr "filename.txt" "\x. x+1") to parse using
      the specified file name in error messages. The return type is
        Either ParseError LTerm
    - The parser also supports a sequence of statements like
      "Pi = 3.14; AddOne = \x -> x+1; AddOne Pi". You can call 
      (parseFile "filename.txt" fileContents) to parse a file like this
      into a list of pairs; the return type is
        Either ParseError [(String, LTerm)]
      Note that parseFile doesn't actually read the file; for that you need
      an IO function like
        main = do
          putStrLn "Input name of file with lambda calculus code in it"
          fileName <- getLine
          file <- openFile fileName ReadMode
          text <- hGetContents file
          putStrLn (show (parseFile fileName text))
    
    It is possible to use this parser without modifications in your assignment
    (but you have my permission to do whatever you want: modify it or not.)
-}
{-# LANGUAGE FlexibleContexts #-}
{-# LANGUAGE NoMonomorphismRestriction #-}
module ParserA4 where

import Text.Parsec hiding (token, tokens, satisfy)
import Text.Parsec.Prim (ParsecT, Stream)
import Text.Parsec.Pos (SourcePos)
import Text.Parsec.Expr (buildExpressionParser, Operator(..), Assoc(..))
import qualified Text.Parsec.Token as Token
import Control.Applicative ((<*), (*>), (<$>), (<*>))
import Data.Functor.Identity (Identity)
import Data.Maybe
import Control.Monad.Identity

-- Prelude

contains (x:xs) item = x == item || contains xs item
contains []     item = False

-----------------------------------------------------------------------------
-- LTerm (Syntax tree) ------------------------------------------------------
-----------------------------------------------------------------------------

-- Initial syntax trees (one per statement): a lambda expression (aka λ-term)
data LTerm = Var String       -- bound variable OR operator name
           | Abs String LTerm -- anonymous function (\string. term)
           | App LTerm LTerm  -- application (fn arg); also used for built-in operators
           | Literal Value    -- any literal (2, 2.5, True, '2', "two")
           | If  LTerm LTerm LTerm  -- if c then t else f
           | Case LTerm (CasePattern LTerm) -- case expr of * -> *
           deriving (Eq)
data CasePattern lterm = 
             PCase (String, String, lterm)        -- x,y -> t0
           | UCase lterm                          -- () -> t0
           | NCase lterm (String, lterm)          -- Zero -> t0; succ n -> t1
           -- The assignment wants us to "deconstruct" a list into a pair, 
           -- which is bizarre since no one actually wants a pair. So this
           -- Parser allows deconstruction into either a pair or head:tail,
           -- and you can support only one case or both, your choice.
           | LCase lterm (String, lterm)          -- nil -> t0; pair -> t1
           | LCase' lterm (String, String, lterm) -- nil -> t0; x:xs -> t1
           deriving (Eq, Show)

-- Values supported by the parser (Assignment three only requires IntVal, BoolVal and ListVal)
data Value = IntVal Int
           | FloatVal Float
           | CharVal Char
           | BoolVal Bool
           | ListVal [Value]
           | UnitVal 
           | PairVal (Value, Value)
           deriving (Eq)

-----------------------------------------------------------------------------
-- Show Value
-----------------------------------------------------------------------------

instance Show Value where
    show v = valueToStr v

valueToStr (BoolVal b) = if b then "true" else "false"
valueToStr (IntVal x) = show x
valueToStr (FloatVal x) = show x
valueToStr (CharVal x) = show x
valueToStr (PairVal (x, y)) = "(" ++ valueToStr x ++ "," ++ valueToStr y ++ ")"
valueToStr UnitVal = "()"
valueToStr (ListVal list) = 
  case all isChar list && not (null list) of 
  True -> show (map toChar list) where
    toChar (CharVal c) = c
  False -> "[" ++ showRest list ++ "]" where
    showRest [] = ""
    showRest [v] = valueToStr v
    showRest (v:w:vs) = valueToStr v ++ (',' : showRest (w:vs))
  where isChar (CharVal _) = True
        isChar _ = False

-----------------------------------------------------------------------------
-- LEXER --------------------------------------------------------------------
-----------------------------------------------------------------------------

type Parser output = ParsecT [Token] () Identity output

data TokenT = TTId      String
            | TTKeyword String
            | TTLiteral Value
            | TTMulDiv  String
            | TTPlus
            | TTMinus
            | TTColon
            | TTNot
            | TTTilde
            | TTUserOp  String
            | TTComp    String
            | TTBitwise String
            | TTLogical String
            | TTAssign
            | TTBackslash
            | TTDot
            | TTComma
            | TTEndStmt
            | LBrack
            | RBrack
            | LBrace
            | RBrace
            | LParen
            | RParen
            | Invalid Char
    deriving (Show, Eq)

type Token = (TokenT, SourcePos)

--withPos rule = (,) <$> rule <*> getPosition
--withPos rule = fmap (\x y -> (x, y)) rule <*> getPosition
withPos rule = do p <- getPosition
                  r <- rule
                  return (r, p)

--charToken :: Stream s m Char => Char -> t -> ParsecT s u m (t, SourcePos)
charToken c t = withPos (char c  >> return t)
charToken2 (a:b:[]) t = withPos (try (char a >> char b >> return t))

keywords = ["if","then","else","true","false","nil","of"]

ident :: ParsecT String () Identity Token
ident = withPos (do {
            first <- oneOf firstChar;
            rest  <- many (oneOf (firstChar ++ "'0123456789"));
            let str = first:rest
            in  return (case keywords `contains` str of 
                        True -> TTKeyword str 
                        False -> TTId str);
        })
        where firstChar = ['A'..'Z'] ++ ['a'..'z'] ++ "_"

simpleString quote consTT = try $ withPos (do 
  char quote
  str <- many (noneOf (quote:"\n\r"))
  char quote
  return (consTT str))

userOp = simpleString '`' (\s -> TTUserOp s)
wrapString str = ListVal (map CharVal str)
wrapStringTT str = TTLiteral (wrapString str)
stringLiteral = simpleString '"' wrapStringTT
charLiteral = simpleString '\'' (\s -> 
             case s of { [c] -> TTLiteral (CharVal c); _ -> wrapStringTT s })
invalid   = withPos (do c <- anyChar; return (Invalid c))

-- These operators concatenate text read by adjacent subparsers
(<++>) a b = (++) <$> a <*> b
(<:>) a b = (:) <$> a <*> b
infixr 9 <:>
infixr 9 <++>

number :: ParsecT String () Identity Token
number = withPos (
  do str <- many1 digit
     more <- optionMaybe $ try (
         char '.'                             <:>
         many1 digit                         <++>
         option [] (try (
           (char 'e' <|> char 'E')            <:>
           option '+' (char '+' <|> char '-') <:>
           many1 digit
         )))
     return (case more of
         Nothing   -> TTLiteral (IntVal ((read str) :: Int))
         Just more -> TTLiteral (FloatVal ((read (str') :: Float)))
                      where str' = str ++ more))

normalToken :: ParsecT String () Identity Token
normalToken = choice
    [ ident
    , number
    , stringLiteral
    , charLiteral
    , userOp
    -- Longer operators must be listed before shorter prefixes!!
    , charToken  '['  LBrack
    , charToken  ']'  RBrack
    , charToken  '{'  LBrace
    , charToken  '}'  RBrace
    , charToken  '('  LParen
    , charToken  ')'  RParen
    , charToken2 "->" TTDot -- for writing lambda terms as \x -> T
    , charToken2 ">>" (TTMulDiv  "<<")
    , charToken2 "<<" (TTMulDiv  ">>")
    , charToken2 "<=" (TTComp    "<=")
    , charToken2 ">=" (TTComp    ">=")
    , charToken2 "==" (TTComp    "==")
    , charToken2 "!=" (TTComp    "!=")
    , charToken2 "/=" (TTComp    "/=")
    , charToken  '≠'  (TTComp    "/=")
    , charToken  '>'  (TTComp    ">")
    , charToken  '<'  (TTComp    "<")
    , charToken  '≥'  (TTComp    ">=")
    , charToken  '≤'  (TTComp    "<=")
    , charToken  '*'  (TTMulDiv  "*")
    , charToken  '/'  (TTMulDiv  "/")
    , charToken  '%'  (TTMulDiv  "%")
    , charToken  '+'  TTPlus
    , charToken  '-'  TTMinus
    , charToken  ':'  TTColon
    , charToken2 "&&" (TTLogical "&&")
    , charToken2 "||" (TTLogical "||")
    , charToken  '&'  (TTBitwise "&")
    , charToken  '|'  (TTBitwise "|")
    , charToken  '^'  (TTBitwise "^")
    , charToken  ';'  TTEndStmt
    , charToken  ','  TTComma
    , charToken  '='  TTAssign
    , charToken  '!'  TTNot
    , charToken  '~'  TTTilde
    , charToken  '\\' TTBackslash -- Two backslashes represents one
    , charToken  'λ'  TTBackslash
    , charToken  '.'  TTDot -- for writing lambda terms as \x. T
    , invalid
    ]

comment = try $ do 
  string "//" <|> string "--"
  skipMany (noneOf "\n\r")
  return '\0'

whiteSpace :: ParsecT String u Identity ()
whiteSpace = skipMany (comment <|> oneOf " \t" <|> newline)

tokensParser = whiteSpace
            >> many (do t <- normalToken; whiteSpace; return t)

tokenize :: String -> String -> Either ParseError [Token]
tokenize sourceName text = runParser tokensParser () sourceName text

-----------------------------------------------------------------------------
-- PARSER -------------------------------------------------------------------
-----------------------------------------------------------------------------

-- advance: Tells Parsec the position of the NEXT token in the text file
-- Parameters: advance <current_position> <current_token> <remaining_tokens> 
advance :: SourcePos -> t -> [Token] -> SourcePos
advance _ _ ((_, pos) : _) = pos
advance pos _ [] = pos

-- Creates a terminal parser (a parser that accepts or rejects a single token)
-- based on a user-defined predicate f.
-- BTW: This function is based on Text.Parsec.Prim.tokenPrim whose parameters 
-- are (1) how to show the token (in error messages?), (2) a function that
-- figures out the next source code position, (3) a function that returns 
-- Maybe t where t is a token value; Nothing indicates that the input didn't
-- match.
satisfy :: (Token -> Bool) -> Parser TokenT
satisfy f = satisfy' (\token -> if f token then Just (fst token) else Nothing)
satisfy' fMaybe = tokenPrim (\(t,pos) -> "at " ++ show pos ++ ": " ++ show t)
                      advance
                      fMaybe

-- A parser that matches token T and returns it. This only supports exact 
-- matches, so we can match a keyword with something like (tok $ TTId "while"),
-- but we can't match an arbitrary identifier with this function.
tok :: TokenT -> Parser TokenT
tok t' = satisfy (\(t,pos) -> t == t')

-- Match identifier and produce a string
tokId :: Parser String
tokId = satisfy' (\(t,pos) -> 
        case t of { TTId s -> Just s; _ -> Nothing })
-- Match literal and produce a Value
tokLiteral :: Parser Value
tokLiteral = satisfy' (\(t,pos) -> 
             case t of { TTLiteral v -> Just v; _ -> Nothing })
-- These parsers match different kinds of operators by precedence
tokMulDiv :: Parser String
tokMulDiv = satisfy' (\(t,pos) -> 
            case t of { TTMulDiv s -> Just s; _ -> Nothing })
tokUserOp :: Parser String
tokUserOp = satisfy' (\(t,pos) -> 
            case t of { TTUserOp s -> Just s; _ -> Nothing })
tokComp :: Parser String
tokComp = satisfy' (\(t,pos) -> 
            case t of { TTComp s -> Just s; _ -> Nothing })
tokBitwise :: Parser String
tokBitwise = satisfy' (\(t,pos) -> 
             case t of { TTBitwise s -> Just s; _ -> Nothing })
tokLogical :: Parser String
tokLogical = satisfy' (\(t,pos) -> 
             case t of { TTLogical s -> Just s; _ -> Nothing })
kw k = tok (TTKeyword k)
idkw k = tok (TTId k)

atom = (do kw "if"; c <- expr; kw "then"; 
           a <- expr; kw "else"; b <- expr; return (If c a b))
   <|> (do kw "false";      return (Literal (BoolVal False)))
   <|> (do kw "true";       return (Literal (BoolVal True)))
   <|> (do kw "nil";        return (Literal (ListVal [])))
   <|> (do v <- tokLiteral; return (Literal v))
   <|> try caseExpr
   <|> (do s <- tokId;      return (Var s))
   <|> (do tok LParen; e <- option (Literal UnitVal) expr; tok RParen; return e)
   <|> (do tok TTBackslash; vars <- many tokId; tok TTDot; e <- expr;
           return $ foldr (\var e -> Abs var e) e vars)
primary = do (f:args) <- many1 atom
             return (foldl (\f x -> App f x) f args)

caseExpr = do 
    idkw "case"
    in_e <- expr
    kw "of"
    makeCasePattern <- 
      (   try parsePCase -- x,y  or  (x,y)
      <|> parseLCase     -- nil->expr;x:xs  or  nil->expr;cons v
      <|> parseNCase     -- 0->expr;succ n
      <|> (do tok LParen; tok RParen; return UCase) -- ()
      )
    tok TTDot -- . or ->
    out_e <- expr
    return (Case in_e (makeCasePattern out_e))
  where
    parsePCase = do 
      lp <- optionMaybe (tok LParen);
      x <- tokId; tok TTComma; y <- tokId
      if lp == Nothing then 
        option RParen (choice []) -- no-op
      else
        tok RParen;
      return (\t -> PCase (x, y, t))
    parseLCase = do 
      -- nil -> expr;  
      kw "nil"
      tok TTDot
      nil_e <- expr
      tok TTEndStmt
      (     (do -- cons v  or  cons v vs
            idkw "cons";
            v <- tokId;
            vs <- option "" tokId;
            if vs /= "" then
              return (\cons_e -> LCase' nil_e (v, vs, cons_e))
            else
              return (\cons_e -> LCase nil_e (v, cons_e)))
        <|> (do -- cons v:vs
            v <- tokId;
            tok TTColon;
            vs <- tokId;
            return (\cons_e -> LCase' nil_e (v, vs, cons_e)))
        )
    parseNCase = do 
      -- 0 -> expr;  
      tok (TTLiteral (IntVal 0)) <|> idkw "zero"
      tok TTDot -- . or ->
      zero_e <- expr
      tok TTEndStmt
      idkw "succ";
      pred <- tokId;
      return (\succ_e -> NCase zero_e (pred, succ_e))

-- Typical way to write a Parsec expression parser
expr :: Parser LTerm
expr = buildExpressionParser opTable primary <?> "expression"
opTable = [ [Prefix (tok TTTilde >> return (unaryExpr "~"))]
          , [Prefix (tok TTNot   >> return (unaryExpr "!"))]
          , [Prefix (tok TTMinus >> return (unaryExpr "neg"))]
          , [Infix (do op <- tokMulDiv;  return (binExpr op)) AssocLeft]
          , [Infix plusMinus                                  AssocLeft]
          , [Infix (do op <- tok TTColon;return (binExpr ":")) AssocRight]
          , [Infix (do op <- tokUserOp;  return (binExpr op)) AssocLeft]
          , [Infix (do op <- tokComp;    return (binExpr op)) AssocLeft]
          , [Infix (do op <- tokBitwise; return (binExpr op)) AssocLeft]
          , [Infix (do op <- tokLogical; return (binExpr op)) AssocLeft]
          , [Infix (do tok TTComma;      return (binExpr ",")) AssocLeft]
          ]
        where
          binExpr   op x y = (App (App (Var op) x) y)
          unaryExpr op x   = (App (Var op) x)
          plusMinus = do
            op <- (tok TTPlus <|> tok TTMinus)
            return (binExpr (case op of { TTPlus -> "+"; _ -> "-" }))

stmt :: Parser (String, LTerm)
stmt = do
  var <- option "" $ try (do var <- tokId; tok TTAssign; return var)
  e <- expr
  return (var, e)
stmts = do 
  ss <- sepBy (optionMaybe stmt) (tok TTEndStmt)
  return (catMaybes ss)
stmtsAndEof = do s <- stmts; eof; return s

parseExpr :: String -> Either ParseError LTerm
parseExpr text = parseWithFN (do e <- expr; eof; return e) "" text

parseAll :: String -> Either ParseError [(String, LTerm)]
parseAll text = parseWithFN stmtsAndEof "" text

parseFile :: String -> String -> Either ParseError [(String, LTerm)]
parseFile fileName text = parseWithFN stmtsAndEof fileName text

parseWithFN :: Parsec [Token] () result -> String -> String -> Either ParseError result
parseWithFN parser fileName text = 
    case tokenize fileName text of
    Left error -> Left error
    Right toks -> parseCore parser toks

parseCore :: Stream s Data.Functor.Identity.Identity t
          => Parsec s () a -> s -> Either ParseError a
parseCore parser tokens = runParser parser () "" tokens
