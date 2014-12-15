{-
    Assignment 4: "BPCF" Simply typed lambda calculus with type inference.
    by David Piepgrass (227845)
    
    For easy testing, call main :: IO (). Press Enter for a REPL or try
    running examples.txt.

    This program is my own implementation of a large subset of Algorithm W, 
    the famous type inference algorithm; basically all that's missing is 
    let-expressions like "let id = \x -> x in expr". The difference between
    a let-expression that defines a function, as opposed to a conventional
    lambda function, is that the the identifier can be used repeatedly 
    rather than only once, and therefore supports "universal quantification".
    This means you can write (id 7) in one place and id has type Int->Int,
    while you can write (id True) in another place, so id is type Bool->Bool.
    
    In this mini-compiler you can define terms and re-use them, like this:
    
      id = \x. x
      if id true then id "Good!" else id "NOOOOO!!!"
    
    But this actually just expands "id" to "(\x.x)" each time it appears, so
    it's not very efficient. Now, although I didn't implement let-expressions,
    I have implemented a couple of universally quantified functions as 
    built-ins, e.g. the Y combinator has type (f->f)->f, for all f, and the 
    pair-joining comma operator has type a->b->(a,b) for all a and b.

    I believe that this type inference algorithm runs in slightly more than
    O(N log N) time for typical programs of size N, which is better than 
    naive type inference implementations that run in O(N^2) time. I believe 
    if I were implementing this in an imperative language that I'd be able 
    to reduce that to O(N) straightforwardly by taking advantage of 
    hashtables and mutable state.
    
    The space complexity is O(N) and I didn't figure out how to do "garbage 
    collection" for the type equations. More precisely, I'm not sure at
    what point I can be sure that an equation will no longer be needed and
    can be discarded, so the equation table ends up roughly as long as the
    program.
    
    Since I already have a runtime engine for a "dynamically typed lambda
    calculus" from Assignment 3, type inference is implemented as a 
    preprocessing stage before compilation to the dynamically-typed machine. 
    So you get the safety of static typing with the speed of dynamic typing.
    It could be worse...actually, given that this is implemented in Haskell 
    instead of C or D or Rust, I don't know if the interpreter could avoid 
    dynamic type checking anyway.
    
    In order to allow the same examples from Assignment 3 to remain 
    compatible with Assignment 4, my interpreter will print a warning but
    still run code that fails type-checking, e.g. ("Aloha":123:true:nil),
    which is supported by the runtime engine but not by the type checker.
-}
import ParserA4
import qualified Data.Map.Strict as Map
import Data.Bits  -- for .&., .|., shiftR, shiftL, etc.
import Data.Char  -- for ord and chr
import Data.Fixed -- for mod' (floating point mod)
import Text.Printf
import Text.Parsec.Error (ParseError)
import System.IO
import Control.Monad
import Control.Monad.State
import Control.Monad.Error
import System.Environment (getArgs)
import System.IO.Unsafe

unwrap :: (Show error) => Either error result -> result
unwrap (Left err)     = error (show err)
unwrap (Right result) = result

-- Can't believe this isn't standard. 
foldl' f z []     = z
foldl' f z (x:xs) = let z' = f z x 
                    in z' `seq` foldl' f z' xs

-- Turns out that foldl is basically the foreach loop of Haskell. Who knew?
-- Note: there is a standard forM, but it doesn't have a state variable.
foreach list initialState f = foldl' f initialState list
foreachM list initialState f = foldM f initialState list

-- e.g. ellipsize 10 "Hello, world!" == "Hello, w.."
ellipsize max str = if length str > max then take (max-2) str ++ ".." else str

-- for debugging only
--trace x r = unsafePerformIO (putStrLn (show x)) `seq` r

-----------------------------------------------------------------------------
-- Type inference - the main part of assignment 4 ---------------------------
-----------------------------------------------------------------------------

-- This is the type of the names of type variables. String is not very 
-- efficient; an industry-grade solution would change this type and find a 
-- way to avoid comparing strings all the time.
type TVarSym = String

data Type = TPair Type Type
          | TFun  Type Type
          | TList Type
          | TInt | TFloat | TBool | TChar | TUnit
          | TVar TVarSym
          deriving (Eq)
data ForallType = ForallType [TVarSym] Type
type TypeEnv = Map.Map String ForallType

-- The parser doesn't preserve source locations. Instead, use the term itself.
type Location = LTerm

data TypeEqn loc = TypeEqn TVarSym Type loc
type TypeEqn' = TypeEqn Location

-- Our type inference algorithm is unable to support all the operators
-- that the virtual machine supports because it only allows each name
-- to have a single type. For example the VM supports 'x'+1=='y' and
-- 'y'-'x'==1. Even if we added support for "type classes" that work
-- like Haskell, the type inference system still wouldn't support this 
-- kind of operator overloading. Instead the type system will pretend
-- (for example) that (+) :: Int -> Int -> Int; it will NOT support all 
-- features of the VM.
predefinedEnv :: TypeEnv
predefinedEnv = Map.fromList predefinedList where 
  tBinFn x y r = TFun x (TFun y r)
  tTriFn x y z r = TFun x (TFun y (TFun z r))
  tcons = ForallType ["item"] (tBinFn (TVar "item") (TList (TVar "item")) (TList (TVar "item")))
  predefinedList = 
    [-- Basic requirements of Assignment 4: succ, fix a.k.a. Y, pair construction (,)
     ("succ"   , ForallType [] (TFun TInt TInt)), 
     ("Y"      , ForallType ["f"] (TFun (TFun (TVar "f") (TVar "f")) (TVar "f"))),
     (","      , ForallType ["p","q"] (tBinFn (TVar "p") (TVar "q") (TPair (TVar "p") (TVar "q")))),
     -- Additional unary operators
     ("neg"    , ForallType [] (TFun TInt TInt)),
     ("~"      , ForallType [] (TFun TInt TInt)),
     ("!"      , ForallType [] (TFun TBool TBool)),
     ("toInt"  , ForallType ["a"] (TFun (TVar "a") TInt)),
     ("toFloat", ForallType ["a"] (TFun (TVar "a") TFloat)),
     ("toChar" , ForallType ["a"] (TFun (TVar "a") TChar)),
     ("isList" , ForallType ["a"] (TFun (TVar "a") TBool)),
     ("isInt"  , ForallType ["a"] (TFun (TVar "a") TBool)),
     ("isBool" , ForallType ["a"] (TFun (TVar "a") TBool)),
     ("isChar" , ForallType ["a"] (TFun (TVar "a") TBool)),
     ("isFloat", ForallType ["a"] (TFun (TVar "a") TBool)),
     -- Additional binary operators
     ("+"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("*"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("/"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("%"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("-"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("<<"  , ForallType [] (tBinFn TInt TInt TInt)),
     (">>"  , ForallType [] (tBinFn TInt TInt TInt)),
     ("&"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("|"   , ForallType [] (tBinFn TInt TInt TInt)),
     ("&&"  , ForallType [] (tBinFn TBool TBool TBool)),
     ("||"  , ForallType [] (tBinFn TBool TBool TBool)),
     (":"   , tcons),
     ("cons", tcons),
     ("pow" , ForallType [] (tBinFn TFloat TFloat TFloat)),
     ("<"   , ForallType [] (tBinFn TInt TInt TBool)),
     ("<="  , ForallType [] (tBinFn TInt TInt TBool)),
     ("=="  , ForallType [] (tBinFn TInt TInt TBool)),
     ("!="  , ForallType [] (tBinFn TInt TInt TBool)),
     ("/="  , ForallType [] (tBinFn TInt TInt TBool)),
     (">"   , ForallType [] (tBinFn TInt TInt TBool)),
     (">="  , ForallType [] (tBinFn TInt TInt TBool))]

----------------------------------------------------
-- Type inference, part 1 of 3: Collecting equations
----------------------------------------------------

-- State & error monad used by collect'. The state monad in C holds 
-- 1. a counter used for creating unique variable names,
-- 2. A list of type equations found by collect', and
-- 3. A list of type symbols that should NOT be eliminated
--    (TODO: REMOVE #3 - IT APPEARS NOT TO BE NECESSARY AFTER ALL)
type C a = ErrorT String (State (Int, [TypeEqn'], [TVarSym])) a

instance Show loc => Show (TypeEqn loc) where
    show = showTypeEqn
showTypeEqn (TypeEqn name typ loc) = 
  printf "%-40s@ %s" (name++" = "++show typ) (ellipsize 38 (show loc)) 

testCollect :: String -> IO ()
testCollect exprStr = do
  let e = unwrap (parseExpr exprStr) 
  let (outTN, eqns) = unwrap $ collect predefinedEnv e
  putStrLn $ "Result variable: "++outTN
  let  printEqns eqn = putStrLn $ showTypeEqn eqn
  mapM printEqns (eqnsToList eqns) 
  return ()
  --return (outTN, eqns)

-- This function collects type equations which are used afterward as the basis
-- for type inference.
collect :: TypeEnv -> LTerm -> Either String (TVarSym, [TypeEqn'])
collect env expr = 
  let m = collect' env expr False
      (either, (counter, typeEqns, _)) = runState (runErrorT m) (0, emptyEqnList, []) in
  case either of Left err -> Left err
                 Right resultVar -> Right (resultVar, typeEqns)
-- Within this function, variables of the form "xT" mean "type of x" while 
-- "xTN" means "name of type variable for x", such that xT = TVar xTN.
-- This function also demonstrates how to translate the obscure type system
-- notation used by comp-sci wonks (where env is called Γ) into actual code.
-- Here are some tips for interpreting this notation.
--   - The turnstile Ⱶ doesn't do anything. It's just a divider, much like a
--     comma. It merely divides the "environment" information (left side) 
--     from everything else (right side). On the left side you will usually
--     see Γ, which refers to the "environment", a map from names to types; it
--     contains bound variables and predefined names such as "+" or "succ".
--     The notation for this map looks like Γ = (succ::Nat->Nat, zero::Nat, x:T)
--     Each name only appears once in the map; adding a new bound variable 
--     can replace the entry for an existing variable (variable shadowing).
--   - The colon (e:T) or double colon (e::T) means "expression e has type T".
--   - The horizontal-bar notation is used in different ways, and this guide
--     will only help you make sense of it if your horizontal-bar equations
--     match the format assumed here. In general, the horizontal bar is sort
--     of an "if-then" statement, with a condition ("premises") on top, and
--     a conclusion on the bottom. On the left or right side is a name for 
--     the rule. There can be multiple premises, separated by blank space.
-- The conversion rules include:
--   - When you see "x::xT, Γ" on the top line, create a new type variable xT
--     for x and insert a new pair (x, xT) into the environment. The modified
--     environment is used only within that clause.
--   - When you see "x::xT, Γ" on the bottom line, look up the type of x in
--     the environment (x will be a string, while xT is a Type).
--   - When you see "Ⱶ expr::exprT" on the top line, recursively collect type
--     equations for expr. exprT will be a simple type variable (TVar exprTN)
--     and in this system I decided to allow the subfunction (the recursively
--     invoked collect') to choose the name of the type variable, which I 
--     believe allows me to pick better names. (The alternative is to have
--     the caller choose a name and pass it into itself recursively.)
--   - When you see a type equation <xT=yT->zT>, add a new type equation. The 
--     left-hand side xT needs to be a simple type variable, i.e. (TVar xTN).
collectE env expr = collect' env expr True
collect' :: TypeEnv -> LTerm -> Bool -> C TVarSym
collect' env expr eliminatable =
  case expr of
    --           var ϵ env                                         neg ϵ env
    -- ----------------------------------proj   e.g. ---------------------------------------------
    --  var::tT, env Ⱶ var::varT <varT=tT>           neg::Int->Int,env Ⱶ neg::neg2 <neg2=Int->Int>
    (Var var) -> 
      case Map.lookup var env of
        Nothing -> throwError ("Undefined term: "++show var)
        Just tT_forall -> do tT <- instantiate tT_forall
                             (TVar varTN) <- newTypeVar var eliminatable
                             addTypeEqn varTN tT expr
                             return varTN
    --        x::xT, env Ⱶ body::bT <...>
    -- -----------------------------------------abst
    --  env Ⱶ (\x.body)::fn_x <fn_x=xT->bT, ...>
    (Abs x body) -> do
       -- Above the line
       xT <- newTypeVar x True
       let env' = (Map.insert x (ForallType [] xT) env)
       bTN <- collectE env' body
       -- Below the line
       (TVar fn_xTN) <- newTypeVar ("fn_"++x) eliminatable
       addTypeEqn fn_xTN (TFun xT (TVar bTN)) expr
       return fn_xTN
    -- env Ⱶ f:fT <...>      env Ⱶ x:xT <...>
    -- --------------------------------------app
    -- env Ⱶ (f x):outT <fT=xT->outT, ...>
    (App f x)    -> do
      fTN <- collectE env f                      -- Above line
      xTN <- collectE env x                      -- Above line
      outT <- newTypeVar "out" eliminatable      -- Below line
      addTypeEqn fTN (TFun (TVar xTN) outT) f    -- Below line
      let (TVar outTN) = outT
      return outTN
    -- 
    -- ---------------------------literal      e.g. ------------------------
    --   env Ⱶ lit:litT <litT=t>                    env Ⱶ 42:lit7 <lit7=Int>
    --   where lit is some literal and t is the type of that literal.
    --   (literalType lit) handles the fact that nil is polymorphic.
    -- What if a literal is repeated, do we really need a new type var each 
    -- time? I'm think we don't, unless it's the nil literal.
    (Literal lit) -> do
      --litT <- newTypeVar (show lit ++ "_")
      --let litTN = show lit
      (typ, litTN) <- literalTypeAndVar lit
      addTypeEqn litTN typ expr
      return litTN
    -- env Ⱶ c::cT      env Ⱶ x::xT      env Ⱶ y::yT
    -- ------------------------------------------------if
    -- env Ⱶ (if c then x else y)::xT  <cT=Bool, xT=yT>
    (If c x y) -> do
      cTN <- collectE env c
      addTypeEqn cTN TBool      c   
      xTN <- collectE env x
      yTN <- collectE env y
      addTypeEqn xTN (TVar yTN) expr
      return xTN
    (Case e casePattern) -> case casePattern of
      -- env Ⱶ e::eT <...>     x::xT, y::yT, env Ⱶ s::sT <...>
      -- -----------------------------------------------------pcase
      -- env Ⱶ (case e of (x,y) -> s)::sT <eT=(xT,yT), ...>
      PCase (x, y, s) -> do
        eTN <- collectE env e
        xT <- newTypeVar x True
        yT <- newTypeVar y True
        let env'  = (Map.insert x (ForallType [] xT) env)
            env'' = (Map.insert y (ForallType [] yT) env')
        sTN <- collect' env'' s eliminatable
        addTypeEqn eTN (TPair xT yT) e
        return sTN
      --     env Ⱶ e::eT <...>     env Ⱶ n::nT <...>     p::pT, env Ⱶ c::cT <...>
      -- ----------------------------------------------------------------------------------Lcase
      -- env Ⱶ (case e of nil->n; cons p->c)::cT <∃elT. eT=L(elT), nT=cT, pT=(elT,L(elT)), ...>
      LCase n (p, c) -> do
        eTN <- collectE env e  -- case expr
        nTN <- collectE env n  --  nil expr
        pT <- newTypeVar p True
        let env' = (Map.insert p (ForallType [] pT) env)
        cTN <- collect' env' c eliminatable -- cons expr
        elT <- newTypeVar "listElem" True
        let (TVar pTN) = pT
        addTypeEqn eTN (TList elT) e
        addTypeEqn nTN (TVar cTN)  expr
        addTypeEqn pTN (TPair elT (TList elT)) c
        return cTN
      -- env Ⱶ e::eT <...>     env Ⱶ n::nT <...>     h::elT, t::L(elT), env Ⱶ c::cT <...>
      -- --------------------------------------------------------------------------------Lcase'
      -- env Ⱶ (case e of nil->n; cons h:T->c)::cT <eT=L(elT), nT=cT, ...>
      LCase' n (h, t, c) -> do
        eTN <- collectE env e  -- case expr
        nTN <- collectE env n  --  nil expr
        elT <- newTypeVar "elem" True
        let env'  = (Map.insert h (ForallType [] elT) env)
        let env'' = (Map.insert t (ForallType [] (TList elT)) env')
        cTN <- collect' env'' c eliminatable -- cons expr
        addTypeEqn eTN (TList elT) e
        addTypeEqn nTN (TVar cTN)  expr
        return cTN
      -- env Ⱶ e::eT <...>     env Ⱶ z::zT <...>     p::pT, env Ⱶ s::sT <...>
      -- --------------------------------------------------------------------Ncase
      -- env Ⱶ (case e of 0->z; succ p->s)::sT  <eT=Nat, pT=Nat, zT=sT, ...>
      NCase z (p, s) -> do
        eTN <- collectE env e  -- case expr
        zTN <- collectE env z  -- zero expr
        pT <- newTypeVar p True
        let env' = (Map.insert p (ForallType [] pT) env)
        sTN <- collect' env' s eliminatable -- succ expr
        let (TVar pTN) = pT
        addTypeEqn eTN TInt e
        addTypeEqn pTN TInt s
        addTypeEqn zTN (TVar sTN) s
        return sTN
      -- env Ⱶ e::eT <...>     env Ⱶ u::uT <...>
      -- ---------------------------------------ucase
      --   env Ⱶ (case e of ()->u)::uT  <eT=1>
      UCase u -> do
        eTN <- collectE env e  -- case expr
        uTN <- collectE env u  -- unit expr
        addTypeEqn eTN TUnit e
        return uTN

-- Given a polymorphic function, returns an instantiation of its type. The purpose
-- of this is to allow the same function to be used more than once with different
-- types each time. For example, consider the pair operator (,)::a->b->(a,b). 
-- Without instantiation, if you construct a pair with (7, True) then it would be 
-- a type error to construct a different pair later with different types, such as 
-- (7.0, 777). (Note that the language defined by this assignment does not allow 
-- you to define polymorphic functions, only to use predefined ones).
instantiate :: ForallType -> C Type
instantiate (ForallType vars t) = case vars of
  [] -> return t
  _  -> do -- Make an equation table that maps each TVarSym in vars to a new TVarSym
           let instantiate1 prefix = do tvar <- newTypeVar prefix True; 
                                        return (prefix, TypeEqn prefix tvar (Literal UnitVal))
           eqns <- mapM instantiate1 vars
           -- Apply that equation table to t
           return $ applySubstitutions t (Map.fromList eqns)

literalTypeAndVar :: Value -> C (Type, TVarSym)
literalTypeAndVar v = 
  let sym = show v in
  case v of
    (IntVal _)   -> return (TInt, sym)
    (FloatVal _) -> return (TFloat, sym)
    (CharVal _)  -> return (TChar, sym)
    (BoolVal _)  -> return (TBool, sym)
    (UnitVal)    -> return (TUnit, sym)
    -- A list literal must be a string literal or a nil literal (no others exist). 
    (ListVal (v:vs)) -> do (vT, _) <- literalTypeAndVar v -- vT is expected to be TChar
                           tvar <- newTypeVar "str" True
                           let (TVar tn) = tvar
                           return (TList vT, tn)
    (ListVal [])     -> do nilv <- newTypeVar "nil" True
                           elemv <- newTypeVar "elem" True
                           let (TVar niln) = nilv
                           return (TList elemv, niln)
    (PairVal (x, y)) -> throwError "unreachable"

newTypeVar :: String -> Bool -> C Type
newTypeVar prefix eliminatable =
    do  (counter, eqns, preserveList) <- get
        let name = prefix ++ show counter
        let preserveList' = if eliminatable then preserveList
            else name:preserveList
        put (counter + 1, eqns, preserveList')
        return (TVar name)

addTypeEqn :: TVarSym -> Type -> Location -> C ()
addTypeEqn name typ location = do
  (c, eqns, p) <- get
  let eqns' = (TypeEqn name typ location):eqns
  put (c, eqns', p)
eqnsToList list = list
emptyEqnList = []

--------------------------------------
-- Type inference, part 2: Unification
--------------------------------------

-- Gets a list of the names of type variables in t::Type.
typeVarsIn :: Type -> [TVarSym]
typeVarsIn t = tvi t [] where
  tvi (TVar n)     list = n:list
  tvi (TFun t1 t2) list = tvi t1 (tvi t2 list)
  tvi (TPair x y)  list = tvi x  (tvi y  list)
  tvi (TList item) list = tvi item list
  tvi _            list = list

-- Creates a single subsitution and performs the "occurs check". When given a 
-- trivial request of the form (check name (TVar name)), the result is empty.
check :: TVarSym -> Type -> Either String [(TVarSym, Type)]
check x t | t == TVar x = return []
          | elem x (typeVarsIn t) = 
            throwError ("occurs check failed: " ++ x ++ " = " ++ show t)
          | otherwise = return [(x, t)]

-- Core of unification algorithm: given two types, produces a "unified type" 
-- and a list of substitutions which, if applied to either of the input types, 
-- produces the unified type. The first type should be the "expected" type
-- and the second one should be the "actual" type.
unify :: Type -> Type -> Either String [(TVarSym, Type)]
unify  tA tB = unify' tA tB []
unify' tA tB output =
    case (tA, tB) of
    (TVar v, t)      -> do sub <- check v t; return $ sub++output
    (t, TVar v)      -> do sub <- check v t; return $ sub++output
    (TInt, TInt)     -> return output
    (TBool, TBool)   -> return output
    (TUnit, TUnit)   -> return output
    (TFloat, TFloat) -> return output
    (TChar, TChar)   -> return output
    (TList t, TList s) -> unify' t s output
    (TFun fT xT, TFun fT' xT') ->
       unify' fT fT' output >>= unify' xT xT'
    (TPair xT yT, TPair xT' yT') ->
       unify' xT xT' output >>= unify' yT yT'
    _ -> throwError ("type mismatch: "++show tA++" vs. "++show tB)

----------------------------------------------------
-- Type inference, part 3: Solving the equation list
----------------------------------------------------

testInfer :: String -> IO ()
testInfer exprStr = do
  testCollect exprStr
  let e = unwrap (parseExpr exprStr) 
  let t = unwrap (inferType e)
  putStrLn $ "*** Inferred type: " ++ show t
  return ()

-- Infers the type of an LTerm or produces an error message
inferType :: LTerm -> Either String Type
inferType lterm = 
  case collect predefinedEnv lterm of
    Left err -> Left err
    Right (sym, eqnList) -> do
      solveEqnsFor sym eqnList

type TypeEqnTable = Map.Map TVarSym (TypeEqn')

solveEqnsFor :: TVarSym -> [TypeEqn'] -> Either String Type
solveEqnsFor sym eqnList = do
  eqnTable <- processTypeEqns eqnList
  case Map.lookup sym eqnTable of
    Nothing -> throwError ("Symbol missing in equation table: "++show sym++" in "++show eqnTable)
    Just (TypeEqn lhs rhs loc) -> do
      let typ = applySubstitutions rhs eqnTable
      doOccursCheck (TypeEqn lhs typ loc)
      return typ

-- Process equations in the same order they were produced by collect, 
-- i.e. starting at the end of the list.
processTypeEqns :: [TypeEqn'] -> Either String TypeEqnTable
processTypeEqns [] = return Map.empty
processTypeEqns (eqn:rest) = 
  do eqns <- processTypeEqns rest
     processTypeEqn eqn eqns

processTypeEqn :: TypeEqn' -> TypeEqnTable -> Either String TypeEqnTable
processTypeEqn (TypeEqn name typ' loc) eqns =
  let findOrInsert = Map.insertLookupWithKey (\key new old -> old) in
  do
    -- Step 1: apply substitutions in the equation table to typ'
    let typ = applySubstitutions typ' eqns
    let typEqn = TypeEqn name typ loc
    trivial <- doOccursCheck typEqn
    if trivial then 
      return eqns
    else do
    -- Step 2: insert new equation. In case of collision, do unification
      case findOrInsert name typEqn eqns of
        (Nothing, eqns') -> -- new equation "name = typ" inserted
          return eqns'
        (Just (TypeEqn _ oldTyp _), _) -> -- already defined -> unify
          case unify typ oldTyp of
            Left errMsg -> 
              throwError (errMsg ++ " (location: " ++ show loc ++ ")")
            Right substitutions -> do
              -- unification produces a list of substitutions. Process them.
              foreach substitutions (Right eqns) (\eqns sub -> do {
                  eqns <- eqns; 
                  let (tvarSym, typ) = sub in
                  processTypeEqn (TypeEqn tvarSym typ loc) eqns;
                })

doOccursCheck :: TypeEqn' -> Either String Bool
doOccursCheck (TypeEqn lhs rhs loc) = do
  if not (elem lhs (typeVarsIn rhs)) then
    return False
  else if rhs == TVar lhs then
    return True -- very rare
  else
    throwError ("occurs check failed: " ++ lhs ++ " = " ++ show rhs ++ " (location: " ++ show loc ++ ")")

applySubstitutions :: Type -> TypeEqnTable -> Type
applySubstitutions tterm eqns = apply tterm 
  where
    apply tterm = case tterm of {
      TVar name -> case Map.lookup name eqns of {
        Nothing -> tterm;
        Just (TypeEqn _ replacement _) -> 
          applySubstitutions replacement eqns };
      TFun t1 t2 -> do {
        let t1' = apply t1;
            t2' = apply t2;
        in  (TFun t1' t2') };
      TPair x y  -> do {
        let x' = apply x;
            y' = apply y;
        in  (TPair x' y') };
      TList item -> do {
        let item' = apply item;
        in  (TList item') };
      _ -> tterm
    }

-----------------------------------------------------------------------------
-- Mostly assignment 3 stuff after this point! ------------------------------
-----------------------------------------------------------------------------
-- Pre-conversion to De Bruijn form -----------------------------------------
-----------------------------------------------------------------------------

-- LTerms are pre-compiled to this form (De Bruijn form)
data DbLTerm = Local Int
             | FreeVar String
             | Literal' Value
             | Abs' DbLTerm
             | App' DbLTerm DbLTerm
             | If' DbLTerm DbLTerm DbLTerm
             | Case' DbLTerm (CasePattern DbLTerm)
             deriving (Eq, Show)

-- Gets the index of an item in a list
indexOf :: (Eq a) => a -> [a] -> Maybe Int
indexOf x list = indexOf' 0 x list
indexOf' n x [] = Nothing
indexOf' n x (s:ss) = if x == s then Just n
                      else           indexOf' (n+1) x ss

-- A function to convert LTerm to DbLTerm. This function also translates 
-- the two "useless" case patterns to "sensible" forms understood by compile.
-- Second parameter is a stack of strings.
convertToDeBruijn lterm = convert lterm [] 
  where
    convert :: LTerm -> [String] -> DbLTerm
    convert (Abs local term) locals = 
      Abs' (convert term (local:locals))
    convert (App left right) locals = 
      App' (convert left locals) (convert right locals)
    convert (Var name) locals = case indexOf name locals of
                                Just i  -> Local i
                                Nothing -> FreeVar name
    convert (Literal val) locals = Literal' val
    convert (If c a b) l = If' (convert c l) (convert a l) (convert b l)
    convert (Case e pattern) locals = 
      case pattern of
      -- The case of (case e of () -> t0) is weird; it has no apparent purpose.
      -- Let's translate it as (\(). t0) input
      UCase t0 -> convert (App (Abs "()" t0) e) locals
      -- Since we have normal arithmetic operators, NCase is pointless too.
      -- Lets translate it as (\?. if ? < 0 then t0 else (\p. t1) ?) (e - 1)
      NCase t0 (p, t1) -> convert (App (Abs q (If cond t0 t1')) e_m1) locals where
        cond = App (App (Var "<") (Var q)) (Literal (IntVal 0))
        e_m1 = App (App (Var "-") e) (Literal (IntVal 1))
        t1' = App (Abs p t1) (Var q) 
        q = "?"
      _ -> Case' (convert e locals) (convertCP pattern) where
        convertCP (PCase (x, y, e0)) = PCase (x, y, convert e0 (y:x:locals))
        convertCP (NCase e0 (p, e1)) = NCase (convert e0 locals) (p, convert e1 (p:locals))
        convertCP (LCase' e0 (x, xs, e1)) = LCase' (convert e0 locals) (x, xs, convert e1 (xs:x:locals))
        convertCP (LCase e0 (p, e1)) = convertCP (convertLC e0 (p, e1)) where
          -- The compiler doesn't actually support LCase, so perform this transformation first:
          -- LCase e0 (p, e1) -> LCase' e0 (x, xs, (\p. e1) (x, xs))
          convertLC e0 (p, e1) = LCase' e0 (x, xs, App (Abs p e1) (App (App (Var ",") (Var x)) (Var xs))) 
            where x = ">x<"; xs = ">xs<"

-----------------------------------------------------------------------------
-- CES data types -----------------------------------------------------------
-----------------------------------------------------------------------------

-- We must support closures as well as normal Values
data Value' = V Value
            | Closure [CesInstr] [Value']
            | YClosure [CesInstr] [Value']
            deriving (Eq, Show)
type Environment = [Value']

data BinOp = Add | Sub | Mul | Div | Mod | Pow | Shr | Shl | AndBits | OrBits | Cons | Lt | Leq | Eq | MakePair
             deriving(Eq, Show)
data UnaryOp = Neg | Not | ToInt | ToFloat | ToChar | IsInt | IsBool | IsChar | IsFloat | IsList | TypeOf
               deriving(Eq, Show)

-- Note: I changed the names of certain things to make them (in my opinion) a bit more clear and logical.
data CesInstr = Lambda [CesInstr]
              | YLambda [CesInstr]
              | Apply
              | Access Int  -- read a local variable
              | Return
              -- Built-in constants & operations
              | Const Value
              | BinOp BinOp
              | UnaryOp UnaryOp
              | ThenElse [CesInstr] [CesInstr] -- thenBranch elseBranch
              | ListCase [CesInstr] [CesInstr] -- consCase nilCase
              | UnpackPair [CesInstr]
              deriving (Eq, Show)

-----------------------------------------------------------------------------
-- Compilation from DbLTerm to CES code [CesInstr] --------------------------
-----------------------------------------------------------------------------

-- For testing
testCompile str = compile (convertToDeBruijn (unwrap (parseExpr str)))

expand :: LTerm -> Map.Map String LTerm -> LTerm
expand term syms = expand' term [] where
  expand' term locals = case term of
    Var str -> 
      case Map.lookup str syms of
      Nothing    -> term
      Just term' -> if elem str locals then term else expand' term' locals
    Abs n t   -> Abs n (expand' t (n:locals))
    App f x   -> App (expand' f locals) (expand' x locals)
    If c a b  -> If (expand' c locals) (expand' a locals) (expand' b locals)
    Case t cp -> Case (expand' t locals) (expandCP cp) where
      expandCP (PCase (x, y, term))       = PCase (x, y, expand' term locals)
      expandCP (LCase' nil (x, xs, term)) = LCase' (expand' nil locals) (x,xs,expand' term locals)
      expandCP (UCase term)               = UCase (expand' term locals)
      expandCP (NCase zero (n, term))     = NCase (expand' zero locals)  (n, expand' term locals)
      expandCP (LCase nil (v, term))      = LCase (expand' nil locals)   (v, expand' term locals)
    _ -> term
{-
-- Replaces all free variables with their expansions
expand :: DbLTerm -> Map.Map String DbLTerm -> DbLTerm
expand term syms = 
  case term of
    FreeVar str -> 
      case Map.lookup str syms of
      Nothing    -> term
      Just term' -> expand term' syms
    Abs' t     -> Abs' (expand t syms)
    App' f x   -> App' (expand f syms) (expand x syms)
    If' c a b  -> If' (expand c syms) (expand a syms) (expand b syms)
    Case' t cp -> Case' (expand t syms) (expandCP cp) where
      expandCP (PCase (x, y, term))       = PCase (x, y, expand term syms)
      expandCP (LCase' nil (x, xs, term)) = LCase' (expand nil syms) (x,xs,expand term syms)
      expandCP (UCase term)               = UCase (expand term syms)
      expandCP (NCase zero (n, term))     = NCase (expand zero syms)  (n, expand term syms)
      expandCP (LCase nil (v, term))      = LCase (expand nil syms)   (v, expand term syms)
    _ -> term

compile :: DbLTerm -> Map.Map String DbLTerm -> [CesInstr]
compile term syms = compile' term' where
  term' = expand term syms
-}

-- Compile (pre-expanded) DeBruijn terms to CES code
compile term = cpl term [] where
  cpl (Local n)      rest = (Access n):rest
  cpl (FreeVar str)  rest = error ("Undefined term: " ++ str)
  cpl (Literal' val) rest = (Const val):rest
  cpl (Abs' body)    rest = (Lambda (cpl body [Return])):rest
  cpl (If' cond t f) rest =
    cpl cond (ThenElse (cpl t [Return]) (cpl f [Return]) : rest)
  cpl (App' fn arg)  rest = 
    -- Oddly, the spec says to reverse the order of the terms in the CES.
    -- This means evaluation will proceed from right to left.
    let defaultResult = cpl arg (cpl fn (Apply:rest)) in
    case fn of
    (App' (FreeVar op) arg0) ->
        case op of
        "+"    -> cpl arg (cpl arg0 (BinOp Add : rest))
        "-"    -> cpl arg (cpl arg0 (BinOp Sub : rest))
        "*"    -> cpl arg (cpl arg0 (BinOp Mul : rest))
        "/"    -> cpl arg (cpl arg0 (BinOp Div : rest))
        "%"    -> cpl arg (cpl arg0 (BinOp Mod : rest))
        "<<"   -> cpl arg (cpl arg0 (BinOp Shl : rest))
        ">>"   -> cpl arg (cpl arg0 (BinOp Shr : rest))
        "&"    -> cpl arg (cpl arg0 (BinOp AndBits : rest))
        "&&"   -> cpl arg (cpl arg0 (BinOp AndBits : rest))
        "|"    -> cpl arg (cpl arg0 (BinOp OrBits : rest))
        "||"   -> cpl arg (cpl arg0 (BinOp OrBits : rest))
        ":"    -> cpl arg (cpl arg0 (BinOp Cons : rest))
        "cons" -> cpl arg (cpl arg0 (BinOp Cons : rest))
        "pow"  -> cpl arg (cpl arg0 (BinOp Pow : rest))
        "<"    -> cpl arg (cpl arg0 (BinOp Lt : rest))
        "<="   -> cpl arg (cpl arg0 (BinOp Leq : rest))
        "=="   -> cpl arg (cpl arg0 (BinOp Eq : rest))
        "!="   -> cpl arg (cpl arg0 (BinOp Eq : UnaryOp Not : rest))
        "/="   -> cpl arg (cpl arg0 (BinOp Eq : UnaryOp Not : rest))
        ">"    -> cpl arg0 (cpl arg (BinOp Lt : rest))
        ">="   -> cpl arg0 (cpl arg (BinOp Leq : rest))
        ","    -> cpl arg (cpl arg0 (BinOp MakePair : rest))
        _    -> defaultResult
    (FreeVar op) ->
        case op of
        "neg"     -> cpl arg (UnaryOp Neg : rest)
        "succ"    -> cpl arg (Const (IntVal 1) : BinOp Add : rest)
        "-"       -> cpl arg (UnaryOp Neg : rest)
        "~"       -> cpl arg (UnaryOp Not : rest)
        "!"       -> cpl arg (UnaryOp Not : rest)
        "toInt"   -> cpl arg (UnaryOp ToInt : rest)
        "toFloat" -> cpl arg (UnaryOp ToFloat : rest)
        "toChar"  -> cpl arg (UnaryOp ToChar : rest)
        "isList"  -> cpl arg (UnaryOp IsList : rest)
        "isInt"   -> cpl arg (UnaryOp IsInt : rest)
        "isBool"  -> cpl arg (UnaryOp IsBool : rest)
        "isChar"  -> cpl arg (UnaryOp IsChar : rest)
        "isFloat" -> cpl arg (UnaryOp IsFloat : rest)
        "Y"       -> 
            case arg of
            (Abs' (Abs' body)) -> (YLambda (cpl body [Return])):rest
            _ -> error ("The Y combinator must have a function of two parameters as its argument: " ++ show arg)
        _         -> defaultResult
    -- old version of case statement for lists (no support from parser required)
    (App' (App' (FreeVar "case") list) nil) ->
        let body = case arg of { -- why do I need braces here??
            (Abs' (Abs' body')) -> body';
            -- Else, pretend the user wrote (\h T. arg h T) instead of arg
            _ -> App' (App' arg (Local 1)) (Local 0);
        } in
            cpl list (ListCase (cpl body [Return]) (cpl nil [Return]) : rest)
    _ -> defaultResult
  cpl (Case' input casePattern) rest =
       cpl input $ case casePattern of
          PCase (_, _, body) -> UnpackPair (cpl body [Return]) : rest
          LCase' nil (_, _, body) -> 
            ListCase (cpl body [Return]) (cpl nil [Return]) : rest
          _ -> error "unreachable case"

-----------------------------------------------------------------------------
-- SEC simple virtual machine -----------------------------------------------
-----------------------------------------------------------------------------

-- Code: a list of instructions
-- "Environment": a list of local variables
-- Stack: a stack of values and continuations
transition :: ([CesInstr], Environment, [Value']) 
           -> ([CesInstr], Environment, [Value'])
transition ((instr:cs), locals, stack) = 
  case instr of
    Const value  -> (cs, locals, (V value):stack)
    Access offs  -> (cs, locals, (locals!!offs):stack)
    Lambda body  -> (cs, locals, (Closure body locals):stack)
    YLambda body -> (cs, locals, (YClosure body locals):stack)
    Apply        -> 
      case stack of
      ((Closure c' e'):v:ss)  -> (c', v:e', (Closure cs locals):ss)
      ((YClosure c' e'):v:ss) -> (c', v:(YClosure c' e'):e', (Closure cs locals):ss)
      (x:v:ss)                -> error ("Attempt to call non-function value " ++ showValue x)
      _                       -> error ("Apply: Unexpected stack state: " ++ show stack)
    Return       ->
      case stack of
      (val:(Closure code' e'):ss) -> (code', e', val:ss)
      _                           -> error ("Return: Unexpected stack state: " ++ show stack ++ " ... " ++ (show (cs, locals)))
    BinOp op     ->
      case stack of
      ((V a):(V b):ss) -> (cs, locals, V (doBinOp op a b) : ss)
      (a:b:ss)         -> error ("Binary "++show op++": cannot take closures as parameters: " ++ show a ++ ", " ++ show b)
    UnaryOp op   ->
      case stack of
      ((V x):ss) -> (cs, locals, V (doUnaryOp op x) : ss)
      (v:ss) -> error ("Unary "++show op++": cannot take closures as parameters: " ++ show v)
      []     -> error ("Unary "++show op++": stack empty!")
    ThenElse cT cF ->
      case stack of
      ((V (BoolVal c)):ss) -> let cBranch = if c then cT else cF 
                                      in  (cBranch, locals, (Closure cs locals):ss)
      (v:ss) -> error ("if: condition is not boolean: " ++ showValue v)
      _ -> error "if: stack empty!"
    UnpackPair cP ->
      case stack of
      ((V (PairVal (x, y))):ss) -> 
        (cP, (V y):(V x):locals, (Closure cs locals):ss)
      (v:ss) -> error ("cannot unpack non-pair: " ++ showValue v)
      [] -> error "unpack pair: stack empty!"
    ListCase cC cN ->
      case stack of
      ((V (ListVal lst)):ss) -> 
        case lst of
          []     -> (cN, locals,                        (Closure cs locals):ss)
          (v:vs) -> (cC, (V (ListVal vs)):(V v):locals, (Closure cs locals):ss)
      (v:ss) -> error ("case applied to non-list: " ++ showValue v)
      [] -> error "case: stack empty!"

-----------------------
-- Primitive operations
-----------------------

doBinOp Add (IntVal a) (IntVal b) = IntVal (a + b)
doBinOp Sub (IntVal a) (IntVal b) = IntVal (a - b)
doBinOp Mul (IntVal a) (IntVal b) = IntVal (a * b)
doBinOp Div (IntVal a) (IntVal b) = IntVal (a `div` b)
doBinOp Mod (IntVal a) (IntVal b) = IntVal (a `mod` b)
doBinOp Lt  (IntVal a) (IntVal b) = BoolVal (a < b)
doBinOp Leq (IntVal a) (IntVal b) = BoolVal (a <= b)
doBinOp Eq  (IntVal a) (IntVal b) = BoolVal (a == b)
doBinOp Shr (IntVal a) (IntVal b) = IntVal (a `shiftR` b)
doBinOp Shl (IntVal a) (IntVal b) = IntVal (a `shiftL` b)
doBinOp AndBits (IntVal a) (IntVal b) = IntVal (a .&. b)
doBinOp OrBits  (IntVal a) (IntVal b) = IntVal (a .|. b)

doBinOp Add (FloatVal a) (FloatVal b) = FloatVal (a + b)
doBinOp Sub (FloatVal a) (FloatVal b) = FloatVal (a - b)
doBinOp Mul (FloatVal a) (FloatVal b) = FloatVal (a * b)
doBinOp Div (FloatVal a) (FloatVal b) = FloatVal (a / b)
doBinOp Mod (FloatVal a) (FloatVal b) = FloatVal (a `mod'` b)
doBinOp Pow (FloatVal a) (FloatVal b) = FloatVal (a ** b)
doBinOp Lt  (FloatVal a) (FloatVal b) = BoolVal (a < b)
doBinOp Leq (FloatVal a) (FloatVal b) = BoolVal (a <= b)
doBinOp Eq  (FloatVal a) (FloatVal b) = BoolVal (a == b)
doBinOp Shr (FloatVal a) (IntVal b) = FloatVal (a / (2.0 ** fromIntegral b))
doBinOp Shl (FloatVal a) (IntVal b) = FloatVal (a / (2.0 ** fromIntegral b))
doBinOp Pow (FloatVal a) (IntVal b) = FloatVal (a ** fromIntegral b)

doBinOp Add (CharVal a) (IntVal b) = CharVal (chr (ord a + b))
doBinOp Sub (CharVal a) (IntVal b) = CharVal (chr (ord a - b))
doBinOp Sub (CharVal a) (CharVal b) = IntVal (ord a - ord b)
doBinOp Lt  (CharVal a) (CharVal b) = BoolVal (a < b)
doBinOp Leq (CharVal a) (CharVal b) = BoolVal (a <= b)
doBinOp Eq  (CharVal a) (CharVal b) = BoolVal (a == b)
doBinOp AndBits (CharVal a) (IntVal b) = CharVal (chr (ord a .&. b))
doBinOp OrBits  (CharVal a) (IntVal b) = CharVal (chr (ord a .|. b))

doBinOp AndBits (BoolVal a) (BoolVal b) = BoolVal (a && b)
doBinOp OrBits  (BoolVal a) (BoolVal b) = BoolVal (a || b)

doBinOp Cons a (ListVal b) = ListVal (a:b)

doBinOp MakePair a b = PairVal (a, b)

doBinOp op a b = error ("Type mismatch: operator " ++ show op ++ " does not support parameters " ++ valueToStr a ++ ", " ++ valueToStr b)

doUnaryOp Neg (IntVal x)     = IntVal (-x)
doUnaryOp Not (IntVal x)     = IntVal (complement x)
doUnaryOp Neg (FloatVal x)   = FloatVal (-x)
doUnaryOp Not (BoolVal x)    = BoolVal (not x)
doUnaryOp ToInt (BoolVal x)  = if x then IntVal 1 else IntVal 0
doUnaryOp ToInt (CharVal x)  = IntVal (ord x)
doUnaryOp ToInt (FloatVal x) = IntVal (floor x)
doUnaryOp ToInt (IntVal x)   = IntVal x
doUnaryOp ToFloat (BoolVal x)  = if x then FloatVal 1 else FloatVal 0
doUnaryOp ToFloat (CharVal x)  = FloatVal (fromIntegral (ord x) :: Float)
doUnaryOp ToFloat (FloatVal x) = FloatVal x
doUnaryOp ToFloat (IntVal x)   = FloatVal (fromIntegral x :: Float)
doUnaryOp ToChar (BoolVal x)  = if x then CharVal '\x01' else CharVal '\0'
doUnaryOp ToChar (CharVal x)  = CharVal x
doUnaryOp ToChar (FloatVal x) = CharVal (chr (floor x))
doUnaryOp ToChar (IntVal x)   = CharVal (chr x)
doUnaryOp IsInt v = case v of { IntVal _ -> BoolVal True; _ -> BoolVal False }
doUnaryOp IsBool v = case v of { BoolVal _ -> BoolVal True; _ -> BoolVal False }
doUnaryOp IsChar v = case v of { CharVal _ -> BoolVal True; _ -> BoolVal False }
doUnaryOp IsFloat v = case v of { FloatVal _ -> BoolVal True; _ -> BoolVal False }
doUnaryOp IsList v = case v of { ListVal _ -> BoolVal True; _ -> BoolVal False }
doUnaryOp TypeOf v = wrapString (typeOf v)

typeOf (IntVal _) = "Int"
typeOf (FloatVal _) = "Float"
typeOf (CharVal _) = "Char"
typeOf (BoolVal _) = "Bool"
typeOf (ListVal _) = "List"

run :: [CesInstr] -> [Value']
run [] = []
run code = run' (code, [], []) where
  run' state =
    let (code', env', stack') = transition state
    in  case code' of
        [] -> stack'
        _  -> run' (code', env', stack')

-----------------------------------------------------------------------------
-- Putting it all together --------------------------------------------------
-----------------------------------------------------------------------------

compileAll :: [(String, LTerm)] -> [[CesInstr]]
compileAll list = r where (r, _) = compileStmts list Map.empty

compileStmts :: [(String, LTerm)] -> Map.Map String LTerm 
            -> ([[CesInstr]], Map.Map String LTerm)
compileStmts [] syms = ([], syms)
compileStmts list syms = 
  let (cs, s) = process list syms [] 
  in  (reverse cs, s) where
    process []          syms results = (results, syms)
    process (line:rest) syms results =
      let (ces, syms') = compileStmt line syms
          results' = case ces of 
            Nothing  -> results
            Just ces -> ces:results
      in  process rest syms' results'

compileStmt :: (String, LTerm) -> Map.Map String LTerm 
            -> (Maybe [CesInstr], Map.Map String LTerm)
compileStmt (name, expr) symbols = 
  let expr' = expand expr symbols 
      expr'' = convertToDeBruijn expr'
  in  case name of
      "" -> (Just (compile expr''), symbols)
      _  -> (Nothing, Map.insert name expr' symbols)

example = "λx. x≤5" 

main :: IO ()
main = do
  args <- getArgs
  fileName <- case args of 
    [] -> do 
       putStrLn "Input name of file with lambda calculus code in it (leave blank for interactive): "
       getLine
    [fN] -> return fN
  if fileName == "" then repl
  else do runFile fileName; return ()

-- read-eval-print loop
repl :: IO ()
repl = repl' (Map.fromList [("pi", Literal (FloatVal pi))])
  where
    repl' syms = do
      putStr ">>> "
      hFlush stdout
      line <- getLine
      if line == ":q" || line == ":quit" || line == "exit" then
        return ()
      else do
        syms' <- compileAndRun "" line syms True
        repl' syms'

runFile fileName = do
  file <- openFile fileName ReadMode
  text <- hGetContents file
  compileAndRun fileName text Map.empty False

compileAndRun :: String -> String -> Map.Map String LTerm -> Bool -> IO (Map.Map String LTerm)
compileAndRun fileName text syms replMode =
  case parseFile fileName text of
    Left err -> do
      putStrLn "-- PARSE FAILED --"
      putStrLn (show err)
      return Map.empty
    Right code -> do -- code :: [(String, LTerm)]
      --putStrLn "-- PARSE RESULTS --"
      --forM code (\r -> putStrLn (show r))
      when (not replMode) $
        putStrLn "-- TYPING RESULTS --"
      foreachM code syms (\syms (name, expr) -> do
        let expr' = expand expr syms
        
        putStr $ if name == "" then show expr else name
        putStr " :: "
        case inferType expr' of 
          Left errMsg -> putStrLn $ "*** "++errMsg
          Right typ   -> putStrLn $ show typ
        return (Map.insert name expr' syms))
      --when (not replMode) $
      --  putStrLn "-- COMPILE RESULTS --"
      let (compiled, syms') = compileStmts code syms
      --forM compiled (\ces -> putStrLn (show ces))
      when (not replMode) $
        putStrLn "-- EXECUTION RESULTS --"
      forM compiled (\ces -> showResult (run ces))
      return syms'

showResult :: [Value'] -> IO ()
showResult (r:[]) = showResult' r
showResult stack = do
  putStrLn (show (length stack) ++ " results left on stack!")
  forM stack showResult'
  return ()
showResult' r = putStrLn (showValue r)

-----------------------------------------------------------------------------
-- Pretty printing ----------------------------------------------------------
-----------------------------------------------------------------------------

showValue (V v) = valueToStr v
showValue other = "{"++show other++"}"

instance Show Type where
    show = showType
showType             ::  Type -> String
showType (TVar n)    =   n
showType TInt        =   "Int"
showType TChar       =   "Char"
showType TFloat      =   "Float"
showType TBool       =   "Bool"
showType TUnit       =   "()"
showType (TList t)   =   "["++showType t++"]"
showType (TPair x y) =   "("++showType x++","++showType y++")"
showType (TFun i o)  =   "("++showType i++" -> "++showType o++")"

isIdStart c = c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_'
instance Show LTerm where
    show expr = showExpr expr (0::Int)

showExpr expr prec =
    let (prec', s) = show' expr in
    if prec > prec' then "("++s++")" else s
  where
    show' (App (App (Var f) x) y) =
      let (c:_) = f 
          p = precOf f 
          q = p .|. 1 in
      if isIdStart c then
        (20, f++" "++showExpr x 21++" "++showExpr y 21)
      else
        (p, showExpr x p++" "++f++" "++showExpr y q)
    show' (App f x) = (20, showExpr f 20++" "++showExpr x 20)
    show' (Var s) = (100, s)
    show' (Abs x body) = (1, "\\"++x++". "++showExpr body 0)
    show' (Literal v) = (100, valueToStr v)
    show' (If c t f) = (2, "if "++showExpr c 0++" then "++showExpr t 0++" else "++showExpr f 2)
    show' (Case e casePattern) =
      (2, "case "++showExpr e 0++" of "++(
        case casePattern of
          PCase (s0, s1, t)   -> s0++","++s1++" -> "++showExpr t 2
          UCase t             -> "() -> "++showExpr t 2
          NCase t0 (p, tN)    -> "0 -> "++showExpr t0 0++"; "++p++" -> "++showExpr tN 2
          LCase tN (p, tC)    -> "nil -> "++showExpr tN 0++"; "++p++" -> "++showExpr tC 2
          LCase' tN (x,xs,tC) -> "nil -> "++showExpr tN 0++"; "++x++":"++xs++" -> "++showExpr tC 2
      ))

precOf "!" = 18
precOf "~" = 18
precOf "*" = 16
precOf "/" = 16
precOf "%" = 16
precOf ">>" = 16
precOf "<<" = 16
precOf "+" = 14
precOf "-" = 14
precOf ":" = 13
precOf "==" = 8
precOf "/=" = 8
precOf ">=" = 8
precOf "<=" = 8
precOf "<" = 8
precOf ">" = 8
precOf "&" = 6
precOf "|" = 6
precOf "^" = 6
precOf "&&" = 4
precOf "||" = 4
precOf "," = 2
precOf _ = 10
