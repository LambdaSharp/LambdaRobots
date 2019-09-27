(*
    Interpret the code in P-CODE, also uses the tables TAB & BTAB
    and the variable S to simulate the run time stack.
*)

{ procedure INTERPRET;  (* global code, tab, btab *) }


LABEL 97, 98;                     (* error trap label *)

CONST
  STEPMAX = 8;
  TRU = 1;
  FALS = 0;
  CHARL = 0;
  CHARH = 63;

TYPE
  StackPointer = ^StackRecord;

  StackRecord = RECORD
                  CASE DUM : TYPES OF (*   S[B+0] = FCT RESULT  *)
                    INTS : (I : Integer); (*   S[B+1] = RETURN ADR  *)
                    REALS : (R : Real); (*   S[B+2] = STATIC LINK *)
                    BOOLS : (B : Boolean); (*   S[B+3] = DYNAMIC LINK*)
                    CHARS : (C : Char); (*   S[B+4] = TABLE INDEX *)
                END;

  StackList = ARRAY[0..STMAX] OF StackPointer;

VAR
  IR : ORDER;                     (* instruction buffer *)
  PS : (RUN, FIN, CASCHK, DIVCHK, INXCHK, STKCHK, LINCHK, LNGCHK, REDCHK, DEADLOCK);
  H1, H2, H3, H4 : LongInt;
  FLD : ARRAY[1..4] OF Integer;   (* default field widths *)

  S : StackList;                  (* Stack for P-Code compiler/interpreter *)

  NPR,
  STEPCOUNT : LongInt;
  Friend, DataElement : Integer;

  PROCEDURE CHOOSEPROC;
  VAR D : Integer;
  BEGIN
    Delay(10-SpeedOption);        {********* Basic speed/slow-down routine}
    D := PMAX+1;
    CURPR := (CURPR+1) MOD(PMAX+1);
    WHILE ((NOT PTAB[CURPR].ACTIVE) OR(PTAB[CURPR].SUSPEND <> 0)) AND(D >= 0)
    DO BEGIN
      D := D-1;
      CURPR := (CURPR+1) MOD(PMAX+1)
    END;
    IF (D < 0) THEN PS := DEADLOCK
    ELSE STEPCOUNT := 8;
  END;


BEGIN                             { INTERPRET }
  IF NOT CreatedRunTimeStack THEN
    BEGIN
      FOR H1 := 0 TO STMAX DO New(S[H1]); (* Create stack space in heap *)
      CreatedRunTimeStack := True;
    END;
  S[1]^.I := 0;
  S[2]^.I := 0;
  S[3]^.I := -1;
  S[4]^.I := BTAB[1].LAST;
  WITH PTAB[0] DO BEGIN
    B := 0;
    SUSPEND := 0;
    DISPLAY[1] := 0;
    T := BTAB[2].VSIZE-1;
    PC := TAB[S[4]^.I]^.ADR;
    ACTIVE := True;
    STACKSIZE := STMAX-PMAX*STKINCR;
  END;
  FOR CURPR := 1 TO PMAX DO WITH PTAB[CURPR] DO BEGIN
    ACTIVE := False;
    DISPLAY[1] := 0;
    PC := 0;
    SUSPEND := 0;
    B := PTAB[CURPR-1].STACKSIZE+1;
    STACKSIZE := B+STKINCR-1;
    T := B-1;
  END;
  NPR := 0;
  CURPR := 0;
  PFLAG := False;
  STEPCOUNT := 0;
  Randomize;                      (* initialize TURBO random number generator *)
  PS := RUN;
  LNCNT := 0;
  OCNT := 0;
  CHRCNT := 0;
  FLD[1] := 10;
  FLD[2] := 22;
  FLD[3] := 10;
  FLD[4] := 1;

  REPEAT
    IF PTAB[0].ACTIVE THEN CURPR := 0
    ELSE IF STEPCOUNT = 0 THEN
      BEGIN
        CHOOSEPROC;
        IF PS = DEADLOCK THEN GOTO 98
    END ELSE STEPCOUNT := STEPCOUNT-1;
    WITH PTAB[CURPR] DO BEGIN
      IR := CODE[PC]^;
      PC := PC+1;
      OCNT := OCNT+1;
    END;
    IF PFLAG THEN BEGIN
      IF IR.F = 18 THEN NPR := NPR+1;
      CURPR := NPR;
    END;
    Friend := Robot[CURPR].AllyNumber; {Number for current robot's ally (if any)}
    WITH PTAB[CURPR] DO

      CASE IR.F OF

        0 : BEGIN                 { load address }
              T := T+1;
              IF T > STACKSIZE THEN PS := STKCHK
              ELSE S[T]^.I := DISPLAY[IR.X]+IR.Y;
            END;
        1 : BEGIN                 { load value   }
              T := T+1;
              IF T > STACKSIZE THEN PS := STKCHK
              ELSE S[T]^ := S[DISPLAY[IR.X]+IR.Y]^;
            END;
        2 : BEGIN                 { load indirect }
              T := T+1;
              IF T > STACKSIZE THEN PS := STKCHK
              ELSE S[T]^ := S[S[DISPLAY[IR.X]+IR.Y]^.I]^;
            END;
        3 : BEGIN                 { update display }
              H1 := IR.Y;
              H2 := IR.X;
              H3 := B;
              REPEAT
                DISPLAY[H1] := H3;
                H1 := H1-1;
                H3 := S[H3+2]^.I;
              UNTIL (H1 = H2);
            END;
        4 : PFLAG := True;        (* CObegin *)
        5 : BEGIN                 (* COend   *)
              PFLAG := False;
              PTAB[0].ACTIVE := False;
            END;
        6,                        { wait }
        7 : { signal } BEGIN {empty} END;
        8 : CASE IR.Y OF          { standard functions and procedures }
              0 : S[T]^.I := Abs(S[T]^.I);
              1 : S[T]^.R := Abs(S[T]^.R);
              2 : S[T]^.I := Sqr(S[T]^.I);
              3 : S[T]^.R := Sqr(S[T]^.R);
              4 : S[T]^.B := Odd(S[T]^.I);
              5,
              6 : BEGIN {empty} END;
              7 : S[T]^.I := Succ(S[T]^.I);
              8 : S[T]^.I := Pred(S[T]^.I);
              9 : S[T]^.I := Round(S[T]^.R);
              10 : S[T]^.I := Trunc(S[T]^.R);
              11 : S[T]^.R := Sin(S[T]^.R*AngToRad);
              12 : S[T]^.R := Cos(S[T]^.R*AngToRad);
              13 : S[T]^.R := Exp(S[T]^.R);
              14 : S[T]^.R := LN(S[T]^.R);
              15 : S[T]^.R := Sqrt(S[T]^.R);
              16 : S[T]^.I := Round(ArcTan(S[T]^.R)*RadToAng);
              17 : BEGIN          {EOF}
                   END;
              18 : BEGIN          {EoLn}
                   END;
              19 : S[T]^.I := Random(S[T]^.I+1);
              20 : BEGIN          {Nothing}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Nothing; {Nothing Constant}
                   END;
              21 : BEGIN          {Distance}
                     T := T-3;
                     S[T]^.I := Distance(S[T]^.I, S[T+1]^.I, S[T+2]^.I, S[T+3]^.I);
                   END;
              22 : BEGIN          {Loc_X}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].X;
                   END;
              23 : BEGIN          {Loc_Y}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].Y;
                   END;
              24 : BEGIN          {Speed}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].Speed;
                   END;
              25 : BEGIN          {Damage}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].Damage;
                   END;
              26 : BEGIN          {Time}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Time;
                   END;
              27 : BEGIN          {Fuel}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].Fuel;
                   END;
              28 : BEGIN          {Enemy}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Enemy; {Enemy Constant}
                   END;
              29 : BEGIN          {Ally}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Ally; {Ally Constant}
                   END;
              30 : BEGIN          {Obstruction}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Obstruction; {Constant}
                   END;
              31 : BEGIN          {Meters}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Meters[CURPR];
                   END;
              32 : BEGIN          {AllyFuel}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].Fuel;
                   END;
              33 : BEGIN          {AllyLoc_X}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].X;
                   END;
              34 : BEGIN          {AllyLoc_Y}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].Y;
                   END;
              35 : BEGIN          {AllySpeed}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].Speed;
                   END;
              36 : BEGIN          {AllyDamage}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].Damage;
                   END;
              37 : BEGIN          {AllyHeading}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[Friend].Heading;
                   END;
              38 : BEGIN          {AllyMeters}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Meters[Friend];
                   END;
              39 : BEGIN          {Dead}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Dead[CURPR];
                   END;
              40 : BEGIN          {Winner}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Winner;
                   END;
              41 : BEGIN          {LimitedFuel}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := NOT UnLimited_Fuel;
                   END;
              42 : BEGIN          {ShieldRaised}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := (Robot[CURPR].Shield = Up);
                   END;
              43 : BEGIN          {AllyShieldRaised}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := (Robot[Friend].Shield = Up);
                   END;
              44 : BEGIN          {AllyDead}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Dead[Friend];
                   END;
              45 : BEGIN          {AllyAlive}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := NOT Dead[Friend];
                   END;
              46 : BEGIN          {Angle_To}
                     T := T-1;
                     S[T]^.I := Angle_To(Robot[CURPR].X, Robot[CURPR].Y, S[T]^.I, S[T+1]^.I);
                   END;
              47 : BEGIN          {Scan}
                     T := T-1;
                     S[T]^.I := Scan(S[T]^.I, S[T+1]^.I);
                   END;
              48 : BEGIN          {ObjectScanned}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := ObjectScanned[CURPR];
                   END;
              49 : BEGIN          {MaxRadarRange}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].ScannerRange;
                   END;
              50 : BEGIN          {MaxSpeed}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].MaxSpeed;
                   END;
              51 : BEGIN          {Engine}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := ORD(Robot[CURPR].Engine);
                   END;
              52 : BEGIN          {Economy}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XEconomy; {Special CONSTANT}
                   END;
              53 : BEGIN          {Compact}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XCompact; {Special CONSTANT}
                   END;
              54 : BEGIN          {Standard}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XStandard; {Special CONSTANT}
                   END;
              55 : BEGIN          {Large}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XLarge; {Special CONSTANT}
                   END;
              56 : BEGIN          {ExtraLarge}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XExtraLarge; {Special CONSTANT}
                   END;
              57 : BEGIN          {Armor}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := ORD(Robot[CURPR].Armor);
                   END;
              58 : BEGIN          {Light}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XLight; {Special CONSTANT}
                   END;
              59 : BEGIN          {Medium}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XMedium; {Special CONSTANT}
                   END;
              60 : BEGIN          {Heavy}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XHeavy; {Special CONSTANT}
                   END;
              61 : BEGIN          {MaxMissileRange}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].MaxMissileRange;
                   END;
              62 : BEGIN          {WarHeads}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := ORD(Robot[CURPR].WarHeads);
                   END;
              63 : BEGIN          {Wimp}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XWimp; {Special CONSTANT}
                   END;
              64 : BEGIN          {Normal}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XNormal; {Special CONSTANT}
                   END;
              65 : BEGIN          {Premium}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := XPremium; {Special CONSTANT}
                   END;
              66 : BEGIN          {BombsLeft}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := Robot[CURPR].Bombs;
                   END;
              67 : BEGIN          {HaveShield}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Robot[CURPR].HasShield;
                   END;
              68 : BEGIN          {HaveCloak}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Robot[CURPR].HasCloak;
                   END;
              69 : BEGIN          {HaveRepairKit}
                     T := T+1;
                       IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.B := Robot[CURPR].HasRepairKit;
                   END;
            END;
        9 : S[T]^.I := S[T]^.I+IR.Y; (* offset *)
        10 : PC := IR.Y;          (* jump   *)
        11 : BEGIN                (* conditional junp *)
               IF NOT S[T]^.B THEN PC := IR.Y;
               T := T-1
             END;
        12 : BEGIN                (* switch *)
               H1 := S[T]^.I;
               T := T-1;
               H2 := IR.Y;
               H3 := 0;
               REPEAT
                 IF CODE[H2]^.F <> 13 THEN BEGIN
                   H3 := 1;
                   PS := CASCHK;
                 END ELSE
                   IF CODE[H2]^.Y = H1 THEN BEGIN
                     H3 := 1;
                     PC := CODE[H2+1]^.Y
                 END ELSE H2 := H2+2;
               UNTIL (H3 <> 0);
             END;
        14 : BEGIN                (* for1UP *)
               H1 := S[T-1]^.I;
               IF H1 <= S[T]^.I THEN S[S[T-2]^.I]^.I := H1
               ELSE BEGIN
                 T := T-3;
                 PC := IR.Y;
               END;
             END;
        15 : BEGIN                (* for2up *)
               H2 := S[T-2]^.I;
               H1 := S[H2]^.I+1;
               IF H1 <= S[T]^.I THEN BEGIN
                 S[H2]^.I := H1;
                 PC := IR.Y
               END ELSE T := T-3;
             END;
        16 : BEGIN                (* for1down *)
               H1 := S[T-1]^.I;
               IF H1 >= S[T]^.I THEN S[S[T-2]^.I]^.I := H1
               ELSE BEGIN
                 PC := IR.Y;
                 T := T-3;
               END;
             END;
        17 : BEGIN                (* for2down *)
               H2 := S[T-2]^.I;
               H1 := S[H2]^.I-1;
               IF H1 >= S[T]^.I THEN BEGIN
                 S[H2]^.I := H1;
                 PC := IR.Y;
               END ELSE T := T-3;
             END;
        18 : BEGIN                (* mark stack *)
               H1 := BTAB[TAB[IR.Y]^.REF].VSIZE;
               IF T+H1 > STACKSIZE THEN PS := STKCHK
               ELSE BEGIN
                 T := T+5;
                 S[T-1]^.I := H1-1;
                 S[T]^.I := IR.Y;
               END;
             END;
        19 : BEGIN                (* call *)
               ACTIVE := True;
               H1 := T-IR.Y;
               H2 := S[H1+4]^.I;  (* H2 points to TAB *)
               H3 := TAB[H2]^.LEV;
               DISPLAY[H3+1] := H1;
               H4 := S[H1+3]^.I+H1;
               S[H1+1]^.I := PC;
               S[H1+2]^.I := DISPLAY[H3];
               S[H1+3]^.I := B;
               IF PFLAG THEN S[H1+3]^.I := PTAB[0].B
               ELSE S[H1+3]^.I := B;
               FOR H3 := T+1 TO H4 DO S[H3]^.I := 0;
               B := H1;
               T := H4;
               PC := TAB[H2]^.ADR;
             END;
        20 : BEGIN                { INDEX1 }
               H1 := IR.Y;        (* H1 points to ATAB *)
               H2 := ATAB[H1].LOW;
               H3 := S[T]^.I;
               IF H3 < H2 THEN PS := INXCHK
               ELSE IF H3 > ATAB[H1].HIGH THEN PS := INXCHK
               ELSE BEGIN
                 T := T-1;
                 S[T]^.I := S[T]^.I+(H3-H2);
               END;
             END;
        21 : BEGIN                { INDEX }
               H1 := IR.Y;        (* H1 POINTS TO ATAB *)
               H2 := ATAB[H1].LOW; H3 := S[T]^.I;
                 IF H3 < H2 THEN PS := INXCHK ELSE
                   IF H3 > ATAB[H1].HIGH THEN PS := INXCHK
               ELSE BEGIN
                 T := T-1;
                 S[T]^.I := S[T]^.I+(H3-H2)*ATAB[H1].ELSIZE;
               END;
             END;
        22 : BEGIN                { load block }
               H1 := S[T]^.I; T := T-1;
                 H2 := IR.Y+T; IF H2 > STACKSIZE THEN PS := STKCHK ELSE
                   WHILE T < H2 DO BEGIN
                     T := T+1;
                     S[T]^ := S[H1]^;
                     H1 := H1+1;
                   END
             END;
        23 : BEGIN                { copy block }
               H1 := S[T-1]^.I;
               H2 := S[T]^.I; H3 := H1+IR.Y;
               WHILE H1 < H3 DO BEGIN
                 S[H1]^ := S[H2]^;
                 H1 := H1+1;
                 H2 := H2+1;
               END;
               T := T-2;
             END;
        24 : BEGIN                { literal }
               T := T+1;
                 IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.I := IR.Y;
             END;
        25 : BEGIN                { load real }
               T := T+1;
                 IF T > STACKSIZE THEN PS := STKCHK ELSE S[T]^.R := RCONST[IR.Y];
             END;
        26 : BEGIN                (* FLOAT *)
               H1 := T-IR.Y;
               S[H1]^.R := S[H1]^.I;
             END;
        27 : BEGIN                (* READ *)
             END;
        28 : BEGIN                (* write STRING *)
             END;
        29 : BEGIN                (* write1 *)
             END;
        30 : BEGIN                (* write2 *)
             END;
        31 : PS := FIN;
        32 : BEGIN                (* EXIT procedure *)
               T := B-1;
               PC := S[B+1]^.I;
               IF PC <> 0 THEN B := S[B+3]^.I
               ELSE BEGIN
                 NPR := NPR-1;
                 ACTIVE := False;
                 STEPCOUNT := 0;
                 PTAB[0].ACTIVE := (NPR = 0)
               END;
             END;
        33 : BEGIN                (* EXIT function *)
               T := B;
               PC := S[B+1]^.I;
               B := S[B+3]^.I;
             END;
        34 : S[T]^ := S[S[T]^.I]^;
        35 : S[T]^.B := NOT S[T]^.B;
        36 : S[T]^.I := -S[T]^.I;
        37 : BEGIN
               CHRCNT := CHRCNT+S[T-1]^.I;
               IF CHRCNT > LINELENG THEN PS := LNGCHK
               ELSE Write(S[T-2]^.R:S[T-1]^.I:S[T]^.I);
               T := T-3;
             END;
        38 : BEGIN                (*STORE*)
               S[S[T-1]^.I]^ := S[T]^;
               T := T-2;
             END;
        39..61 : BEGIN
                   T := T-1;
                   CASE IR.F OF

                     39 : S[T]^.B := (S[T]^.R = S[T+1]^.R);
                     40 : S[T]^.B := (S[T]^.R <> S[T+1]^.R);
                     41 : S[T]^.B := (S[T]^.R < S[T+1]^.R);
                     42 : S[T]^.B := (S[T]^.R <= S[T+1]^.R);
                     43 : S[T]^.B := (S[T]^.R > S[T+1]^.R);
                     44 : S[T]^.B := (S[T]^.R >= S[T+1]^.R);
                     45 : S[T]^.B := (S[T]^.I = S[T+1]^.I);
                     46 : S[T]^.B := (S[T]^.I <> S[T+1]^.I);
                     47 : S[T]^.B := (S[T]^.I < S[T+1]^.I);
                     48 : S[T]^.B := (S[T]^.I <= S[T+1]^.I);
                     49 : S[T]^.B := (S[T]^.I > S[T+1]^.I);
                     50 : S[T]^.B := (S[T]^.I >= S[T+1]^.I);
                     51 : S[T]^.B := (S[T]^.B OR S[T+1]^.B);
                     52 : S[T]^.I := S[T]^.I+S[T+1]^.I;
                     53 : S[T]^.I := S[T]^.I-S[T+1]^.I;
                     54 : S[T]^.R := S[T]^.R+S[T+1]^.R;
                     55 : S[T]^.R := S[T]^.R-S[T+1]^.R;
                     56 : S[T]^.B := (S[T]^.B AND S[T+1]^.B);
                     57 : S[T]^.I := S[T]^.I*S[T+1]^.I;
                     58 : IF S[T+1]^.I = 0 THEN PS := DIVCHK
                          ELSE S[T]^.I := S[T]^.I DIV S[T+1]^.I;
                     59 : IF S[T+1]^.I = 0 THEN PS := DIVCHK
                          ELSE S[T]^.I := S[T]^.I MOD S[T+1]^.I;
                     60 : S[T]^.R := S[T]^.R*S[T+1]^.R;
                     61 : S[T]^.R := S[T]^.R/S[T+1]^.R;
                   END;           { case }
                 END;             { begin }

        62 : BEGIN {ReadLn} END;
        63 : BEGIN {WriteLn} END;
        64 : BEGIN {ClrScr} END;
        65 : Initialize_Robots;
        66 : Update_Robots;
        67 : BEGIN                {Drive}
               T := T-2;
               Drive(S[T+1]^.I, S[T+2]^.I);
             END;
        68 : BEGIN                {Cannon}
               T := T-2;
               Cannon(S[T+1]^.I, S[T+2]^.I);
             END;
        69 : Finalize_Robots;
        70 : IF Robot[CURPR].HasShield THEN Robot[CURPR].Shield := Up;               {RaiseShield}
        71 : Robot[CURPR].Shield := Down;             {LowerShield}
        72 : IF Robot[CURPR].HasCloak THEN Robot[CURPR].Cloak := Up;                {RaiseCloak}
        73 : Robot[CURPR].Cloak := Down;              {LowerCloak}
        74 : StartRepairs;                            {Start Repairs for CURPR}
        75 : StopRepairs;                             {Stop Repairs for CURPR}
        76 : DropBomb;                                {Place Bomb for CURPR}
        77 : DetonateBomb;                            {Explode Bomb for CURPR}

      END (*case*) ;
  UNTIL PS <> RUN;


98: BEGIN
    RestoreCursor;
    IF PS = FIN THEN DEBUG := False;
    IF (PS <> RUN) AND DEBUG THEN BEGIN (* fatal error in user's program *)
      GotoXY(1, 1);
      WITH PTAB[CURPR]
      DO Write(' HALT at', PC:5, ' in process', CURPR:4, ' because of ');
      CASE PS OF
        FIN : WriteLn('NORMAL PROGRAM TERMINATION');
        DEADLOCK : WriteLn('DEADLOCK');
        RUN : WriteLn('ERROR (SEE DAYFILE)');
        CASCHK : WriteLn('UNDEFINED CASE');
        DIVCHK : WriteLn('DIVIDE BY 0');
        INXCHK : WriteLn('INVALID INDEX');
        STKCHK : WriteLn('STORAGE OVERFLOW');
        LINCHK : WriteLn('TOO MUCH OUTPUT');
        LNGCHK : WriteLn('LINE TOO LONG');
        REDCHK : WriteLn('READ PAST END OF FILE');
      END;
      WriteLn;
      Write('Hit RETURN to continue');
      ReadLn;
      WriteLn(LISTING);
      WriteLn(LISTING, ' *********  POST MORTEM DUMP ********');
      WriteLn(LISTING);
      WITH PTAB[CURPR]
      DO Write(LISTING, ' HALT at', PC:5, ' in process', CURPR:4, ' because of ');
      CASE PS OF
        FIN : WriteLn(LISTING, 'NORMAL PROGRAM TERMINATION');
        DEADLOCK : WriteLn(LISTING, 'DEADLOCK');
        RUN : WriteLn(LISTING, 'ERROR (SEE DAYFILE)');
        CASCHK : WriteLn(LISTING, 'UNDEFINED CASE');
        DIVCHK : WriteLn(LISTING, 'DIVIDE BY 0');
        INXCHK : WriteLn(LISTING, 'INVALID INDEX');
        STKCHK : WriteLn(LISTING, 'STORAGE OVERFLOW');
        LINCHK : WriteLn(LISTING, 'TOO MUCH OUTPUT');
        LNGCHK : WriteLn(LISTING, 'LINE TOO LONG');
        REDCHK : WriteLn(LISTING, 'READ PAST END OF FILE');
      END;
      WriteLn(LISTING);
97:   WriteLn(LISTING);
    END;
  END;
  IF DEBUG THEN BEGIN
    WriteLn(LISTING);
    WriteLn(LISTING, OCNT, ' STEPS');
  END;
END;                              {Interpt}
