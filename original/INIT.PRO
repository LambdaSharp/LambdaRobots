
    (*-------------------------------------------------------INITIALIZE---*)

    PROCEDURE INITIALIZE;

    VAR I : Integer;
      C : Char;

    BEGIN

(*
    =================
     character types
    =================
 *)

      SPS['+'] := PLUS; SPS['-'] := MINUS;
      SPS['*'] := TIMES; SPS['/'] := RDIV;
      SPS['('] := LPARENT; SPS[')'] := RPARENT;
      SPS['='] := EQL; SPS[','] := COMMA;
      SPS['['] := LBRACK; SPS[']'] := RBRACK;
      SPS['"'] := NEQ; SPS['&'] := ANDSY;
      SPS[';'] := SEMICOLON;

      FOR C := Chr(ORDMINCHAR) TO Chr(ORDMAXCHAR) DO CASE C OF

        'A'..'Z' : CHARTP[C] := LETTER;
        'a'..'z' : CHARTP[C] := LOWCASE;
        '0'..'9' : CHARTP[C] := NUMBER;

        '+', '-', '*', '/', '(', ')', '$', '=', ' ', ',',
        '.', '''', '[', ']', ':', '^', '_', ';', '{', '}',
        '<', '>' : CHARTP[C] := SPECIAL;

      ELSE CHARTP[C] := ILLEGAL;

      END;

(*
    ===========
       Sets
    ===========
*)

      CONSTBEGSYS := [PLUS, MINUS, INTCON, REALCON, CHARCON, IDENT];
      TYPEBEGSYS := [IDENT, ARRAYSY, RECORDSY];
      BLOCKBEGSYS := [CONSTSY, TYPESY, VARSY, PROCSY, FUNCSY, BEGINSY];
      FACBEGSYS := [INTCON, REALCON, CHARCON, IDENT, LPARENT, NOTSY];
      STATBEGSYS := [BEGINSY, IFSY, WHILESY, REPEATSY, FORSY, CASESY];
      STANTYPS := [NOTYP, INTS, REALS, BOOLS, CHARS];

(*
   ===========
     Scalars
   ===========
*)

      LC := 0;
      LL := 0;
      CC := 0;
      CH := ' ';
      ERRPOS := 0;
      ERRS := [];
      T := -1;
      A := 0;
      B := 1;
      SX := 0;
      C2 := 0;
      DISPLAY[0] := 1;
      IFLAG := False;
      OFLAG := False;
      DFLAG := False;
      SKIPFLAG := False;
      LINECOUNT := -1;

    END;                          { INITIALIZE }

    PROCEDURE ENTERSTDFCNS;

(*--------------------------------------------------------ENTER-----
   the following procedures enter the apropriate type
   into the associated table for that type.
*)

      PROCEDURE ENTER(X0 : ALFA; X1 : xOBJECT; X2 : TYPES; X3 : Integer);
      BEGIN
        T := T+1;                 (* enter standard identifier *)
        WITH TAB[T]^ DO BEGIN
          NAME := X0;
          LINK := T-1;
          OBJ := X1;
          TYP := X2;
          REF := 0;
          NORMAL := True;
          LEV := 0;
          ADR := X3;
        END;
      END;                        { ENTER }

    BEGIN
      ENTER('          ', VARIABLE, NOTYP, 0);
      ENTER('FALSE     ', KONSTANT, BOOLS, 0);
      ENTER('TRUE      ', KONSTANT, BOOLS, 1);
      ENTER('REAL      ', TYPE1, REALS, 1);
      ENTER('CHAR      ', TYPE1, CHARS, 1);
      ENTER('BOOLEAN   ', TYPE1, BOOLS, 1);
      ENTER('INTEGER   ', TYPE1, INTS, 1);
      ENTER('ABS       ', FUNKTION, REALS, 0);
      ENTER('SQR       ', FUNKTION, REALS, 2);
      ENTER('ODD       ', FUNKTION, BOOLS, 4);
      ENTER('CHR       ', FUNKTION, CHARS, 5);
      ENTER('ORD       ', FUNKTION, INTS, 6);
      ENTER('SUCC      ', FUNKTION, INTS, 7);
      ENTER('PRED      ', FUNKTION, INTS, 8);
      ENTER('ROUND     ', FUNKTION, INTS, 9);
      ENTER('TRUNC     ', FUNKTION, INTS, 10);
      ENTER('SIN       ', FUNKTION, REALS, 11);
      ENTER('COS       ', FUNKTION, REALS, 12);
      ENTER('EXP       ', FUNKTION, REALS, 13);
      ENTER('LN        ', FUNKTION, REALS, 14);
      ENTER('SQRT      ', FUNKTION, REALS, 15);
      ENTER('ARCTAN    ', FUNKTION, REALS, 16);
      ENTER('EOF       ', FUNKTION, BOOLS, 17);
      ENTER('EOLN      ', FUNKTION, BOOLS, 18);
      ENTER('RANDOM    ', FUNKTION, INTS, 19);
      ENTER('NOTHING   ', FUNKTION, INTS, 20);
      ENTER('DISTANCE  ', FUNKTION, INTS, 21);
      ENTER('LOC_X     ', FUNKTION, INTS, 22);
      ENTER('LOC_Y     ', FUNKTION, INTS, 23);
      ENTER('SPEED     ', FUNKTION, INTS, 24);
      ENTER('DAMAGE    ', FUNKTION, INTS, 25);
      ENTER('TIME      ', FUNKTION, INTS, 26);
      ENTER('FUEL      ', FUNKTION, INTS, 27);
      ENTER('ENEMY     ', FUNKTION, INTS, 28);
      ENTER('ALLY      ', FUNKTION, INTS, 29);
      ENTER('OBSTRUCTIO', FUNKTION, INTS, 30);
      ENTER('METERS    ', FUNKTION, INTS, 31);
      ENTER('ALLYFUEL  ', FUNKTION, INTS, 32);
      ENTER('ALLYLOC_X ', FUNKTION, INTS, 33);
      ENTER('ALLYLOC_Y ', FUNKTION, INTS, 34);
      ENTER('ALLYSPEED ', FUNKTION, INTS, 35);
      ENTER('ALLYDAMAGE', FUNKTION, INTS, 36);
      ENTER('ALLYHEADIN', FUNKTION, INTS, 37);
      ENTER('ALLYMETERS', FUNKTION, INTS, 38);
      ENTER('DEAD      ', FUNKTION, BOOLS, 39);
      ENTER('WINNER    ', FUNKTION, BOOLS, 40);
      ENTER('LIMITEDFUE', FUNKTION, BOOLS, 41);
      ENTER('SHIELDRAIS', FUNKTION, BOOLS, 42);
      ENTER('ALLYSHIELD', FUNKTION, BOOLS, 43);
      ENTER('ALLYDEAD  ', FUNKTION, BOOLS, 44);
      ENTER('ALLYALIVE ', FUNKTION, BOOLS, 45);
      ENTER('ANGLE_TO  ', FUNKTION, INTS, 46);
      ENTER('SCAN      ', FUNKTION, INTS, 47);
      ENTER('OBJECTSCAN', FUNKTION, INTS, 48);
      ENTER('MAXRADARRA', FUNKTION, INTS, 49);
      ENTER('MAXSPEED  ', FUNKTION, INTS, 50);
      ENTER('ENGINE    ', FUNKTION, INTS, 51);
      ENTER('ECONOMY   ', FUNKTION, INTS, 52);
      ENTER('COMPACT   ', FUNKTION, INTS, 53);
      ENTER('STANDARD  ', FUNKTION, INTS, 54);
      ENTER('LARGE     ', FUNKTION, INTS, 55);
      ENTER('EXTRALARGE', FUNKTION, INTS, 56);
      ENTER('ARMOR     ', FUNKTION, INTS, 57);
      ENTER('LIGHT     ', FUNKTION, INTS, 58);
      ENTER('MEDIUM    ', FUNKTION, INTS, 59);
      ENTER('HEAVY     ', FUNKTION, INTS, 60);
      ENTER('MAXMISSILE', FUNKTION, INTS, 61);
      ENTER('WARHEADS  ', FUNKTION, INTS, 62);
      ENTER('WIMP      ', FUNKTION, INTS, 63);
      ENTER('NORMAL    ', FUNKTION, INTS, 64);
      ENTER('PREMIUM   ', FUNKTION, INTS, 65);
      ENTER('BOMBSLEFT ', FUNKTION, INTS, 66);
      ENTER('HAVESHIELD', FUNKTION, BOOLS, 67);
      ENTER('HAVECLOAK ', FUNKTION, BOOLS, 68);
      ENTER('HAVEREPAIR', FUNKTION, BOOLS, 69);
      ENTER('READ      ', PROZEDURE, NOTYP, 1);
      ENTER('READLN    ', PROZEDURE, NOTYP, 2);
      ENTER('WRITE     ', PROZEDURE, NOTYP, 3);
      ENTER('WRITELN   ', PROZEDURE, NOTYP, 4);
      ENTER('WAIT      ', PROZEDURE, NOTYP, 5);
      ENTER('SIGNAL    ', PROZEDURE, NOTYP, 6);
      ENTER('CLRSCR    ', PROZEDURE, NOTYP, 7);
      ENTER('XX_INIT   ', PROZEDURE, NOTYP, 8);
      ENTER('XX_UPDATE ', PROZEDURE, NOTYP, 9);
      ENTER('DRIVE     ', PROZEDURE, NOTYP, 10);
      ENTER('CANNON    ', PROZEDURE, NOTYP, 11);
      ENTER('XX_FINAL  ', PROZEDURE, NOTYP, 12);
      ENTER('RAISESHIEL', PROZEDURE, NOTYP, 13);
      ENTER('LOWERSHIEL', PROZEDURE, NOTYP, 14);
      ENTER('RAISECLOAK', PROZEDURE, NOTYP, 15);
      ENTER('LOWERCLOAK', PROZEDURE, NOTYP, 16);
      ENTER('MAKEREPAIR', PROZEDURE, NOTYP, 17);
      ENTER('STOPREPAIR', PROZEDURE, NOTYP, 18);
      ENTER('PLACEBOMB ', PROZEDURE, NOTYP, 19);
      ENTER('DETONATE  ', PROZEDURE, NOTYP, 20);
      ENTER('          ', PROZEDURE, NOTYP, 0);
    END;                          { ENTERSTDFCNS }

    PROCEDURE ERRORMSG;
    CONST MSG : ARRAY[0..60] OF String[40] =
      ('UNDEFINED IDENTIFIER',
       'MULTIPLE DEFINITION OF THIS IDENTIFIER',
       'EXPECTED AN IDENTIFIER',
       'PROGRAM MUST BEGIN WITH "PROGRAM"',
       'EXPECTED CLOSING PARENTHESIS ")"',
       {  5 } 'EXPECTED A COLON ":"',
       'INCORRECTLY USED SYMBOL',
       'EXPECTED IDENTIFIER OR THE SYMBOL "VAR"',
       'EXPECTED THE SYMBOL "OF"',
       'EXPECTED AN OPENING PARENTHESIS "("',
       { 10 } 'EXPECTED IDENTIFER, "ARRAY" OR "RECORD"',
       'EXPECTED AN OPENING BRACKET "["',
       'EXPECTED A CLOSING BRACKET "]"',
       'EXPECTED ".." WITHOUT INTERVENING BLANKS',
       'EXPECTED A SEMICOLON ";"',
       { 15 } 'BAD RESULT TYPE FOR A FUNCTION',
       'EXPECTED AN EQUAL SIGN "="',
       'EXPECTED BOOLEAN EXPRESSION ',
       'CONTROL VARIABLE OF THE WRONG TYPE',
       'MUST BE MATCHING TYPES',
       { 20 } '"OUTPUT" IS REQUIRED IN PROGRAM HEADING',
       'THE NUMBER IS TOO LARGE',
       'EXPECT PERIOD ".", CHECK BEGIN-END PAIRS',
       'BAD TYPE FOR A CASE STATEMENT',
       'ILLEGAL CHARACTER',
       { 25 } 'ILLEGAL CONSTANT OR CONSTANT IDENTIFIER',
       'ILLEGAL ARRAY SUBSCRIPT (CHECK TYPE)',
       'ILLEGAL BOUNDS FOR AN ARRAY INDEX',
       'INDEXED VARIABLE MUST BE AN ARRAY',
       'EXPECTED A TYPE IDENFIFIER',
       { 30 } 'UNDEFINED TYPE',
       'VAR WITH FIELD SELECTOR MUST BE RECORD',
       'EXPECTED TYPE "BOOLEAN"',
       'ILLEGAL TYPE FOR ARITHMETIC EXPRESSION',
       'EXPECTED INTEGER FOR "DIV" OR "MOD"',
       { 35 } 'INCOMPATIBLE TYPES FOR COMPARISON',
       'PARAMETER TYPES DO NOT MATCH',
       'EXPECTED A VARIABLE',
       'A STRING MUST HAVE ONE OR MORE CHAR',
       'NUMBER OF PARAMETERS DO NOT MATCH',
       { 40 } 'INVALID "TeamAlly" NAME FORMAT',
       'ILLEGAL PARAMETERS TO "WRITE"',
       'PARAMETER MUST BE OF TYPE "REAL"',
       'PARAMETER MUST BE OF TYPE "INTEGER"',
       'EXPECTED VARIABLE OR CONSTANT',
       { 45 } 'EXPECTED A VARIABLE OR PROCEDURE',
       'TYPES MUST MATCH IN AN ASSIGNMENT',
       'CASE LABEL NOT SAME TYPE AS CASE CLAUSE',
       'ARGUMENT TO STD. FUNCTION OF WRONG TYPE',
       'THE PROGRAM REQUIRES TOO MUCH STORAGE',
       { 50 } 'ILLEGAL SYMBOL FOR A CONSTANT',
       'EXPECTED BECOMES ":="',
       'EXPECTED "THEN"',
       'EXPECTED "UNTIL"',
       'EXPECTED "DO"',
       { 55 } 'EXPECTED "TO" OR "DOWNTO"',
       'EXPECTED "BEGIN"',
       'EXPECTED "END"',
       'EXPECTED ID, CONST, "NOT" OR "("',
       '"INPUT"  IS REQUIRED IN PROGRAM HEADING',
       { 60 } 'CONTROL CHARACTER PRESENT IN SOURCE ');

    VAR K : Integer;

    BEGIN
      K := 0;
      WriteLn; WriteLn(' ERROR MESSAGE(S)');
      IF DEBUG THEN
        BEGIN
          WriteLn(LISTING);
          WriteLn(LISTING, ' ERROR MESSAGE(S)');
        END;
      WHILE ERRS <> [] DO BEGIN
        WHILE NOT(K IN ERRS) DO K := K+1;
        WriteLn(K:2, '  ', MSG[K]);
        IF DEBUG THEN WriteLn(LISTING, K:2, '  ', MSG[K]);
        ERRS := ERRS-[K]
      END;
    END;                          { ERRORMSG }
