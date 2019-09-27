(***** Compiler parameters "optimized" for speed, not error checking *****)
{$R-}                             {Range checking OFF}
{$B-}                             {Boolean short circuiting ON}
{$S+}                             {Stack checking OFF}
{$I+}                             {I/O checking ON}
{$D-}                             {NO Debug information generated}
{$A+}                             {Align data on word boundaries}
{$F-}                             {FAR calls only when needed}
{$N-}                             {No numeric coprocessor}

UNIT CompUnit;

INTERFACE

USES
  CRT,
  DOS,
  Globals;

PROCEDURE HideCursor;

FUNCTION DisplayMode : Byte;

PROCEDURE SetColors;

PROCEDURE RestoreCursor;

PROCEDURE compile;


  {===========================================================================}

IMPLEMENTATION


  PROCEDURE HideCursor;
  VAR OldOne : Integer;

    PROCEDURE ChangeCursor(NewSize : Integer; VAR OldSize : Integer);

{ This utility will let you change the type (shape and visibility) of the
cursor.  At the same time, it saves the previous type.
For block cursor, set new:=  13   (Works on all machines.)
For invisible cursor, set new:=8192 }

    BEGIN
      INLINE(
        $31/$C0                   {       XOR   AX,AX            ;Set AX=0}
        /$8E/$C0                  {       MOV   ES,AX            ;Set ES=0}
        /$26/$A1/$60/$04          {       ES: MOV AX,WO[$0460]   ;Read old cursor type}
        /$C4/$7E/<OldSize         {       LES   DI,[BP+<OldSize]     ;Set address for Old}
        /$AB                      {       STOSW                  ;Store old value}
        /$B4/$01                  {       MOV   AH,$01           ;}
        /$8B/$4E/<NewSize         {       MOV   CX,[BP+<NewSize]     ;Get New value}
        /$CD/$10                  {       INT   $10              ;Set cursor change}
        );
    END;
  BEGIN                           {HideCursor}
    OldOne := 13;
    ChangeCursor(8192, OldOne);   {For some strange reason, must be done twice}
    ChangeCursor(8192, OldOne);
  END;


  FUNCTION DisplayMode : Byte;

  VAR Regs : registers;

  BEGIN
    Regs.ah := $0F;
    INTR($10, DOS.registers(Regs));
    DisplayMode := Regs.al;
  END;                            {DisplayMode}


  PROCEDURE SetColors;

  BEGIN
    IF DisplayMode = Mono
    THEN BEGIN
      TextColor(LIGHTGRAY);
      TEXTBACKGROUND(BLACK);
      BColor := BLACK;
      MColor := White;            {High lighted white}
      RobotColor[1] := LIGHTGRAY;
      RobotColor[2] := LIGHTGRAY;
      RobotColor[3] := LIGHTGRAY;
      RobotColor[4] := LIGHTGRAY;
    END
    ELSE BEGIN                    {some kind of color capability}
      TextColor(LIGHTGRAY);
      TEXTBACKGROUND(BLACK);
      BColor := BLACK;
      MColor := White;            {High-lighted white}
      RobotColor[1] := LightGreen;
      RobotColor[2] := Yellow;
      RobotColor[3] := LightCyan;
      RobotColor[4] := LightRed;
    END;
  END;                            {SetColors}


  PROCEDURE RestoreCursor;

    PROCEDURE SetCursor(Starting, Ending : Byte);
      {-Set the starting and ending scan lines for the cursor.}
    BEGIN
      INLINE(
        $B4/$01                   {MOV AH,1}
        /$8A/$6E/<Starting        {MOV CH,[BP+<Starting]}
        /$8A/$4E/<Ending          {MOV CL,[BP+<Ending]}
        /$55                      {PUSH BP}
        /$CD/$10                  {INT $10}
        /$5D                      {POP BP}
        );
    END;

  BEGIN
    IF DisplayMode = Mono THEN
      SetCursor(11, 12)
    ELSE
      SetCursor(6, 7);
  END;                            {RestoreCursor}


  PROCEDURE compile;

  TYPE InString = String[LLNG];

  CONST KEY : ARRAY[1..NKW] OF String[10] =
    ('AND       ', 'ARRAY     ',
     'BEGIN     ', 'CASE      ',
     'COBEGIN   ', 'COEND     ',
     'CONST     ', 'DIV       ',
     'DO        ', 'DOWNTO    ',
     'ELSE      ', 'END       ',
     'FOR       ', 'FUNCTION  ',
     'IF        ', 'MOD       ',
     'NOT       ', 'OF        ',
     'OR        ', 'PROCEDURE ',
     'PROGRAM   ', 'RECORD    ',
     'REPEAT    ', 'THEN      ',
     'TO        ', 'TYPE      ',
     'UNTIL     ', 'VAR       ',
     'WHILE     ');


  TYPE SYMBOL = (INTCON, REALCON, CHARCON, Word,
                 PLUS, MINUS, TIMES, IDIV, RDIV, IMOD,
                 NOTSY, ANDSY, ORSY,
                 EQL, NEQ, GTR, GEQ, LSS, LEQ,
                 LPARENT, RPARENT, LBRACK, RBRACK,
                 COMMA, SEMICOLON, PERIOD, COLON,
                 BECOMES, CONSTSY, TYPESY, VARSY, ARRAYSY, RECORDSY,
                 FUNCSY, PROCSY, PROGRAMSY, IDENT, BEGINSY, ENDSY,
                 REPEATSY, UNTILSY, WHILESY, DOSY, FORSY,
                 IFSY, THENSY, ELSESY, CASESY, OFSY, TOSY, DOWNTOSY);

    SYMSET = SET OF SYMBOL;

  CONST KSY : ARRAY[1..NKW] OF SYMBOL =
    (ANDSY, ARRAYSY, BEGINSY, CASESY,
     BEGINSY, ENDSY, CONSTSY, IDIV,
     DOSY, DOWNTOSY, ELSESY, ENDSY,
     FORSY, FUNCSY, IFSY, IMOD,
     NOTSY, OFSY, ORSY, PROCSY,
     PROGRAMSY, RECORDSY, REPEATSY, THENSY,
     TOSY, TYPESY, UNTILSY, VARSY,
     WHILESY);

  VAR DISPLAY : ARRAY[0..LMAX] OF Integer;
    SPS : ARRAY[' '..']'] OF SYMBOL;

(*
   =============================
   key words and special symbols
   =============================
*)

    (* indicies to tables  *)

    T,                            (* ---> TAB,    *)
    A,                            (* ---> ATAB,   *)
    SX,                           (* ---> STAB,   *)
    C1,                           (* ---> RCONST, *)
    C2,                           (* ---> RCONST  *)

    LC (* program Location Counter *) : Integer;

(*
      =========================
       Error Control Variables
      =========================
*)

    ERRS : SET OF 0..ERMAX;       (* compilation errors        *)
    ERRPOS : Integer;
    SKIPFLAG : Boolean;           (* used by procedure ENDSKIP *)

(*
      =============================
      Insymbol (scanner) Variables
      =============================
*)

    SY : SYMBOL;                  (* last symbol read by INSYMBOL *)
    ID : ALFA;                    (* identifier from     INSYMBOL *)
    INUM : Integer;               (* integer from        INSYMBOL *)
    RNUM : Real;                  (* real number from    INSYMBOL *)
    SLENG : Integer;              (* string length                *)
    CHARTP : ARRAY[Char] OF CHTP; (* character types *)
    LINE : InString;              (* input line      *)
    CC : Integer;                 (* character counter            *)
    LL : Integer;                 (* length of current line       *)
    LINECOUNT : Integer;          (* source line counter          *)


(*
   ======
    sets
   ======
*)

    CONSTBEGSYS, TYPEBEGSYS,
    BLOCKBEGSYS, FACBEGSYS, STATBEGSYS : SYMSET;

    LineOfProgram : ARRAY[0..64] OF String[70];

    PROCEDURE InitLines;
    BEGIN
      TextColor(RobotColor[1]);
      ClrScr;
      ReadingFromFile := False;
      CurrentLine := -1;
      CurrentFile := 1;
      LineOfProgram[0] := 'PROGRAM Test(Input,Output);  VAR COMM : ARRAY[1..20] OF Integer;';
      LineOfProgram[1] := ' ';

      LineOfProgram[2] := 'PROCEDURE Target;'; {Pascal code for "default" opponent}
      LineOfProgram[3] := 'VAR';
      LineOfProgram[4] := ' angle, Last_D, range : Integer;';
      LineOfProgram[5] := ' PROCEDURE GoTo(x,y : INTEGER);';
      LineOfProgram[6] := ' BEGIN';
      LineOfProgram[7] := '  angle := Angle_To(x, y);';
      LineOfProgram[8] := '  drive(angle, 100);';
      LineOfProgram[9] := '  WHILE ((distance(loc_x, loc_y, x, y) > 150) AND (speed > 0))';
      LineOfProgram[10] := '   DO {Nothing};';
      LineOfProgram[11] := '  drive(angle, 20);';
      LineOfProgram[12] := '  WHILE ((distance(loc_x, loc_y, x, y) > 10) AND (speed > 0))';
      LineOfProgram[13] := '   DO {Nothing};';
      LineOfProgram[14] := '  drive(angle, 0);  {I.E., Stop}';
      LineOfProgram[15] := ' END; {GoTo(X,Y)}';
      LineOfProgram[16] := ' FUNCTION Hurt : BOOLEAN;';
      LineOfProgram[17] := ' VAR Curr_D : INTEGER;';
      LineOfProgram[18] := '     Answer : BOOLEAN;';
      LineOfProgram[19] := ' BEGIN';
      LineOfProgram[20] := '  Curr_D := Damage;';
      LineOfProgram[21] := '  IF Curr_D > last_D';
      LineOfProgram[22] := '   THEN Answer := TRUE';
      LineOfProgram[23] := '   ELSE Answer := FALSE;';
      LineOfProgram[24] := '  Last_D := Curr_D;';
      LineOfProgram[25] := '  Hurt := Answer;';
      LineOfProgram[26] := ' END;';
      LineOfProgram[27] := ' Procedure Move;';
      LineOfProgram[28] := ' VAR x, y : INTEGER;';
      LineOfProgram[29] := ' BEGIN';
      LineOfProgram[30] := '  x := random(400) + 300;';
      LineOfProgram[31] := '  y := random(400) + 300;';
      LineOfProgram[32] := '  Goto(x,y);';
      LineOfProgram[33] := ' END; {Move}';
      LineOfProgram[34] := 'BEGIN {Target Main}';
      LineOfProgram[35] := ' angle := 0;';
      LineOfProgram[36] := ' GoTo(500,500);';
      LineOfProgram[37] := ' REPEAT';
      LineOfProgram[38] := '  range := scan(angle,10);';
      LineOfProgram[39] := '  WHILE range > 40 DO';
      LineOfProgram[40] := '   BEGIN';
      LineOfProgram[41] := '    Cannon(angle,range);';
      LineOfProgram[42] := '    range := scan(angle,10);';
      LineOfProgram[43] := '   END;';
      LineOfProgram[44] := '  angle := angle + 20;';
      LineOfProgram[45] := '  IF Hurt THEN Move;';
      LineOfProgram[46] := ' UNTIL DEAD OR WINNER;';
      LineOfProgram[47] := 'END; {Target Main}';

      LineOfProgram[48] := 'PROCEDURE XX_Score;';
      LineOfProgram[49] := '  BEGIN';
      LineOfProgram[50] := '    REPEAT';
      LineOfProgram[51] := '      XX_Update;';
      LineOfProgram[52] := '    UNTIL WINNER;';
      LineOfProgram[53] := '  END;';

      LineOfProgram[54] := 'BEGIN {Main}';
      LineOfProgram[55] := '  XX_Init;';
      LineOfProgram[56] := '  CoBEGIN;';
      LineOfProgram[57] := FileName[1]+';';
      IF NumRobots = 1
      THEN LineOfProgram[58] := 'Target;'
      ELSE LineOfProgram[58] := FileName[2]+';';
      IF NumRobots < 3
      THEN LineOfProgram[59] := ' '
      ELSE LineOfProgram[59] := FileName[3]+';';
      IF NumRobots < 4
      THEN LineOfProgram[60] := ' '
      ELSE LineOfProgram[60] := FileName[4]+';';
      LineOfProgram[61] := '  XX_Score;';
      LineOfProgram[62] := '  CoEND;';
      LineOfProgram[63] := '  XX_Final;';
      LineOfProgram[64] := 'END.'; {Last line of program}
    END;


    (*--------------------------------------------------------ERROR-----*)

    PROCEDURE ERROR(n : Integer);
(*
   write error on current line & add to TOT ERR
*)
    BEGIN
      IF ERRPOS = 0 THEN BEGIN
        Write('[**> ', ' ':6);
        IF DEBUG THEN Write(LISTING, '[**> ', ' ':6);
      END;
      IF CC > ERRPOS THEN BEGIN
        Write(' ':CC-ERRPOS, '^', n:2);
        IF DEBUG THEN Write(LISTING, ' ':CC-ERRPOS, '^', n:2);
        ERRPOS := CC+3;
        ERRS := ERRS+[n];
      END;
    END;                          { ERROR }

(*-----------------------------------------------------ENDSKIP------

   ENDSKIP changed to just print blanks for skipped symbols.
   This should cause less confusion than the underlining did.
*)

    PROCEDURE ENDSKIP;            (* underline skipped part of input *)
    BEGIN
      WHILE ERRPOS < CC DO BEGIN
        Write(' ');
        IF DEBUG THEN Write(LISTING, ' ');
        ERRPOS := ERRPOS+1;
      END;
      SKIPFLAG := False;
    END;                          { ENDSKIP }

    PROCEDURE FATAL(n : Integer); Forward;
    PROCEDURE NEXTCH; Forward;


(*---------------------------------------------------------EMIT-----
   emit actual code into the code table
*)
    PROCEDURE EMIT(FCT : Integer);
    BEGIN
      IF LC = CMAX THEN FATAL(6);
      CODE[LC]^.F := FCT;
      LC := LC+1;
    END;                          { EMIT }

    PROCEDURE EMIT1(FCT, B : Integer);
    BEGIN
      IF LC = CMAX THEN FATAL(6);
      WITH CODE[LC]^ DO BEGIN
        F := FCT;
        Y := B;
      END;
      LC := LC+1;
    END;                          { EMIT1 }

    PROCEDURE EMIT2(FCT, A, B : Integer);
    BEGIN
      IF LC = CMAX THEN FATAL(6);
      WITH CODE[LC]^ DO BEGIN
        F := FCT;
        X := A;
        Y := B;
      END;
      LC := LC+1;
    END;                          { EMIT2 }

    (*-----------------------INITTABLES----ERRORMSG----ENTERSTDFCNS-----*)


    {$I INIT.PRO }

    (*-----------------------------------------------------INSYMBOL-----*)


    {$I INSYMBOL.PRO }

(*--------------------------------------------------PRINTTABLES-----
   this procedure prints out the internal compiler and
   interpreter tables.  This procedure is called if the
   DEBUG flag is TRUE.
*)

    PROCEDURE PRINTTABLES;
    VAR I : Integer;
      O : ORDER;
    BEGIN
      WriteLn(LISTING);
    END;                          { PRINTTABLES }

    (*--------------------------------------------------------BLOCK-----*)


        {$I BLOCKA.PRO }

        {$I BLOCKB.PRO }

        {$I BLOCKC.PRO }

    (*--------------------------------------------------------FATAL-----*)

    PROCEDURE FATAL;              (* internal table overflow *)
    BEGIN
      IF ERRS <> [] THEN ERRORMSG;
      WriteLn;
      Write('COMPILER TABLE for ');
      CASE n OF
        1 : Write('IDENTIFIER');
        2 : Write('PROCEDURES');
        3 : Write('REALS');
        4 : Write('ARRAYS');
        5 : Write('LEVELS');
        6 : Write('CODE');
        7 : Write('STRINGS');
      END;
      WriteLn(' is too SMALL');
      WriteLn; WriteLn;
      WriteLn(' FATAL termination of Pascal Robots');
      WriteLn(LISTING);
      Write(LISTING, 'COMPILER TABLE for ');
      CASE n OF
        1 : Write(LISTING, 'IDENTIFIER');
        2 : Write(LISTING, 'PROCEDURES');
        3 : Write(LISTING, 'REALS');
        4 : Write(LISTING, 'ARRAYS');
        5 : Write(LISTING, 'LEVELS');
        6 : Write(LISTING, 'CODE');
        7 : Write('STRINGS');
      END;
      WriteLn(LISTING, ' is too SMALL');
      WriteLn(LISTING); WriteLn(LISTING);
      WriteLn(LISTING, ' FATAL termination of Pascal Robots');
      Close(LISTING);
      RestoreCursor;
      Halt;
    END;                          { FATAL }

    (*-------------------------------------------------------NEXTCH-----*)

    PROCEDURE NEXTCH;             (* read next char from either "internal program" or file *)

      PROCEDURE GetNextLineFromFile;
      VAR
        Spot : Integer;
        InputLine : InString;
      BEGIN
        LL := 0;
        CC := 0;
        InputLine := '';
        WHILE NOT EoLn(SOURCE) DO
          BEGIN
            LL := LL+1;
            Read(SOURCE, CH);
            Write(CH);
            IF DEBUG THEN Write(LISTING, CH);
            LINE[LL] := CH;
            InputLine := InputLine+Upcase(CH);
          END;
        LL := LL+1;
        WriteLn;
        IF DEBUG THEN WriteLn(LISTING);
        ReadLn(SOURCE);
        LINE[LL] := ' ';
        IF Pos('TEAMALLY', InputLine) <> 0 THEN {Handle "Ally" declaration}
          BEGIN
            Spot := Pos('"', InputLine); {Find first " mark}
            IF Spot = 0 THEN ERROR(40);
            InputLine := Copy(InputLine, Spot+1, 255);
            Spot := Pos('"', InputLine); {Find second " mark}
            IF Spot = 0 THEN ERROR(40);
            FriendlyName[CurrentFile] := Copy(InputLine, 1, Spot-1);
            GetNextLineFromFile;  {Skip to next line}
          END;
      END;

      PROCEDURE GetNextLineFromList; {get line from "internal" list}
      BEGIN
        CC := 0;
        CurrentLine := CurrentLine+1;
        IF (CurrentLine = 1)
        THEN BEGIN
          ReadingFromFile := True;
          LINE := ' ';
          LL := 1;
        END
        ELSE BEGIN
          LINE := LineOfProgram[CurrentLine]+' ';
          LL := Length(LINE);
        END;
      END;

    BEGIN                         {NEXTCH}
      IF CC = LL THEN
        IF ReadingFromFile THEN
          BEGIN
            IF EoF(SOURCE) THEN
              BEGIN
                Close(SOURCE);
                WriteLn;
                WriteLn;
                IF NumRobots = 1 THEN
                  BEGIN
                    ReadingFromFile := False;
                    CurrentLine := 1; {starting by reading in TARGET Robot}
                    GetNextLineFromList;
                  END
                ELSE IF NumRobots = CurrentFile THEN
                  BEGIN
                    ReadingFromFile := False;
                    CurrentLine := 47; {Switch to reading end of "Driver" program}
                    GetNextLineFromList;
                  END
                ELSE              {more robot files to read}
                  BEGIN
                    CurrentFile := CurrentFile+1;
                    TextColor(RobotColor[CurrentFile]);
                    Assign(SOURCE, FileName[CurrentFile]+'.PR');
                    {$I-}
                    Reset(SOURCE);
                    {$I+}
                    ReadingFromFile := True;
                  END
              END;
            IF ERRPOS <> 0 THEN
              BEGIN
                IF SKIPFLAG THEN ENDSKIP;
                ERRPOS := 0;
                WriteLn;
                IF DEBUG THEN WriteLn(LISTING);
              END;
            IF ReadingFromFile THEN
              BEGIN
                LINECOUNT := LINECOUNT+1;
                Write(LINECOUNT:4, '  ');
                Write(LC:5, '  ');
                IF DEBUG THEN BEGIN
                  Write(LISTING, LINECOUNT:4, '  ');
                  Write(LISTING, LC:5, '  ');
                END;
                GetNextLineFromFile;
              END;
          END                     {ReadingFromFile}
        ELSE GetNextLineFromList;
      CC := CC+1;
      CH := LINE[CC];
      IF (Ord(CH) < Ord(' ')) THEN ERROR(60)
    END;                          { NEXTCH }


  BEGIN                           { COMPILE }

(*
   =============================
     check for program heading
   =============================
*)
    InitLines;
    INITIALIZE;
    INSYMBOL;
    IF SY <> PROGRAMSY THEN ERROR(3) ELSE BEGIN
      INSYMBOL;
      IF SY <> IDENT THEN ERROR(2) ELSE BEGIN
        PROGNAME := ID;
        INSYMBOL;
        IF SY <> LPARENT THEN ERROR(9) ELSE REPEAT
          INSYMBOL;
          IF SY <> IDENT THEN ERROR(2) ELSE BEGIN
            IF ID = 'INPUT     ' THEN IFLAG := True
            ELSE IF ID = 'OUTPUT    ' THEN OFLAG := True
            ELSE IF (NOT DFLAG) THEN BEGIN
              DFILE := '          ';
              m := 0;
              WHILE ID[m+1]IN['A'..'Z', '0'..'9', ':']
              DO m := m+1;
              Move(ID, DFILE[11-m], m);
              DFLAG := True;
              WriteLn(' DFLAG <- TRUE ', DFILE, m:5);
            END ELSE ERROR(0);
            INSYMBOL;
          END;
        UNTIL SY <> COMMA;
        IF SY = RPARENT THEN INSYMBOL ELSE ERROR(4);
        IF NOT OFLAG THEN ERROR(20)
      END
    END;

    ENTERSTDFCNS;

    WITH BTAB[1] DO BEGIN
      LAST := T;
      LASTPAR := 1;
      PSIZE := 0;
      VSIZE := 0
    END;

(*
   ============
     COMPILE
   ============
*)

    BLOCK(BLOCKBEGSYS+STATBEGSYS, False, 1);

    IF (SY <> PERIOD) THEN ERROR(22);
    EMIT(31);                     (* halt *)
    IF (BTAB[2].VSIZE > STMAX-STKINCR*PMAX) THEN ERROR(49);
    IF DEBUG THEN PRINTTABLES;
    IF ERRS <> [] THEN BEGIN
      ERRORMSG;
      Close(LISTING);
      Halt;
    END;
  END;                            { COMPILE }

END.
