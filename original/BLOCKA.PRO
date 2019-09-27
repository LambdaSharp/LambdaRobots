    PROCEDURE BLOCK(FSYS : SYMSET; ISFUN : Boolean; LEVEL : Integer);

    TYPE CONREC = RECORD CASE TP : TYPES OF
                      INTS, CHARS, BOOLS : (I : Integer);
                      REALS : (R : Real)
                  END;

    VAR DX : Integer;             (* data allocation index     *)
      PRT : Integer;              (* T-index of this procedure *)
      PRB : Integer;              (* B-index of this procedure *)
      X : Integer;


      PROCEDURE ENTERARRAY(TP : TYPES; L, H : Integer);
      BEGIN
        IF L > H THEN ERROR(27);
        IF (Abs(L) > XMAX) OR(Abs(H) > XMAX) THEN BEGIN
          ERROR(27);
          L := 0;
          H := 0;
        END;
        IF A = AMAX THEN FATAL(4) ELSE BEGIN
          A := A+1;
          WITH ATAB[A] DO BEGIN
            INXTYP := TP;
            LOW := L;
            HIGH := H
          END;
        END;
      END;                        { ENTERARRAY }

      PROCEDURE ENTERBLOCK;
      BEGIN
        IF B = BMAX THEN FATAL(2) ELSE BEGIN
          B := B+1;
          BTAB[B].LAST := 0;
          BTAB[B].LASTPAR := 0;
        END;
      END;                        { ENTERBLOCK }

      PROCEDURE ENTERREAL(X : Real);
      BEGIN
        IF C2 = C2MAX-1 THEN FATAL(3) ELSE BEGIN
          RCONST[C2+1] := X;
          C1 := 1;
          WHILE RCONST[C1] <> X DO C1 := C1+1;
          IF C1 > C2 THEN C2 := C1
        END;
      END;                        { ENTERREAL }

      PROCEDURE SKIP(FSYS : SYMSET; n : Integer);
      BEGIN
        ERROR(n);
        SKIPFLAG := True;
        WHILE NOT(SY IN FSYS) DO INSYMBOL;
        IF SKIPFLAG THEN ENDSKIP;
      END;                        { SKIP }

      PROCEDURE TEST(S1, S2 : SYMSET; n : Integer);
      BEGIN
        IF NOT(SY IN S1) THEN SKIP(S1+S2, n);
      END;                        { TEST }

      PROCEDURE TESTSEMICOLON;
      BEGIN
        IF SY = SEMICOLON THEN INSYMBOL ELSE BEGIN
          ERROR(14);
          IF SY IN[COMMA, COLON] THEN INSYMBOL
        END;
        TEST([IDENT]+BLOCKBEGSYS, FSYS, 6)
      END;                        { TESTSEMICOLON }

      PROCEDURE ENTER(ID : ALFA; K : xOBJECT);
      VAR J, L : Integer;
      BEGIN
        IF T = TMAX THEN FATAL(1) ELSE BEGIN
          TAB[0]^.NAME := ID;
          J := BTAB[DISPLAY[LEVEL]].LAST;
          L := J;
          WHILE TAB[J]^.NAME <> ID DO J := TAB[J]^.LINK;
          IF J <> 0 THEN ERROR(1) ELSE BEGIN
            T := T+1;
            WITH TAB[T]^ DO BEGIN
              NAME := ID;
              LINK := L;
              OBJ := K;
              TYP := NOTYP;
              REF := 0;
              LEV := LEVEL;
              ADR := 0;
            END;
            BTAB[DISPLAY[LEVEL]].LAST := T;
          END;
        END;
      END;                        { enter }

      FUNCTION LOC(ID : ALFA) : Integer;
      VAR I, J : Integer;         (* locate identifier, ID, in table *)
      BEGIN
        I := LEVEL;
        TAB[0]^.NAME := ID;       (* sentinel *)
        REPEAT
          J := BTAB[DISPLAY[I]].LAST;
          WHILE TAB[J]^.NAME <> ID DO J := TAB[J]^.LINK;
          I := I-1;
        UNTIL (I < 0) OR(J <> 0);
        IF J = 0 THEN ERROR(0);
        LOC := J;
      END;                        { LOC }

      PROCEDURE ENTERVARIABLE;
      BEGIN
        IF SY = IDENT THEN BEGIN
          ENTER(ID, VARIABLE);
          INSYMBOL;
        END ELSE ERROR(2);
      END;                        { ENTERVARIABLE }

      PROCEDURE CONSTANT(FSYS : SYMSET; VAR C : CONREC);
      VAR X, SIGN : Integer;
      BEGIN
        C.TP := NOTYP;
        C.I := 0;
        TEST(CONSTBEGSYS, FSYS, 50);
        IF SY IN CONSTBEGSYS THEN BEGIN
          IF SY = CHARCON THEN BEGIN
            C.TP := CHARS;
            C.I := INUM;
            INSYMBOL;
          END ELSE BEGIN
            SIGN := 1;
            IF SY IN[PLUS, MINUS] THEN BEGIN
              IF SY = MINUS THEN SIGN := -1;
              INSYMBOL;
            END;
            IF SY = IDENT THEN BEGIN
              X := LOC(ID);
              IF X <> 0 THEN IF TAB[X]^.OBJ <> KONSTANT THEN ERROR(25)
                ELSE BEGIN
                  C.TP := TAB[X]^.TYP;
                  IF C.TP = REALS THEN C.R := SIGN*RCONST[TAB[X]^.ADR]
                  ELSE C.I := SIGN*TAB[X]^.ADR
                END;
              INSYMBOL;
            END ELSE IF SY = INTCON THEN BEGIN
              C.TP := INTS; C.I := SIGN*INUM;
              INSYMBOL
            END ELSE IF SY = REALCON THEN BEGIN
              C.TP := REALS;
              C.R := SIGN*RNUM;
              INSYMBOL;
            END ELSE SKIP(FSYS, 50);
          END;
          TEST(FSYS, [], 6);
        END;
      END;                        { CONSTANT }

      PROCEDURE TYP(FSYS : SYMSET; VAR TP : TYPES; VAR RF, SZ : Integer);
      VAR X : Integer;
        ELTP : TYPES; ELRF : Integer;
        ELSZ, OFFSET, T0, T1 : Integer;

        PROCEDURE ARRAYTYP(VAR AREF, ARSZ : Integer);
        VAR ELTP : TYPES;
          LOW, HIGH : CONREC;
          ELRF, ELSZ : Integer;
        BEGIN
          CONSTANT([COLON, RBRACK, RPARENT, OFSY]+FSYS, LOW);
          IF LOW.TP = REALS THEN BEGIN
            ERROR(27);
            LOW.TP := INTS;
            LOW.I := 0;
          END;
          IF SY = COLON THEN INSYMBOL ELSE ERROR(13);
          CONSTANT([RBRACK, COMMA, RPARENT, OFSY]+FSYS, HIGH);
          IF HIGH.TP <> LOW.TP THEN BEGIN
            ERROR(27);
            HIGH.I := LOW.I;
          END;
          ENTERARRAY(LOW.TP, LOW.I, HIGH.I);
          AREF := A;
          IF SY = COMMA THEN BEGIN
            INSYMBOL;
            ELTP := ARRAYS;
            ARRAYTYP(ELRF, ELSZ)
          END ELSE BEGIN
            IF SY = RBRACK THEN INSYMBOL ELSE BEGIN
              ERROR(12);
              IF SY = RPARENT THEN INSYMBOL;
            END;
            IF SY = OFSY THEN INSYMBOL ELSE ERROR(8);
            TYP(FSYS, ELTP, ELRF, ELSZ);
          END;
          WITH ATAB[AREF] DO BEGIN
            ARSZ := (HIGH-LOW+1)*ELSZ;
            SIZE := ARSZ;
            ELTYP := ELTP;
            ELREF := ELRF;
            ELSIZE := ELSZ;
          END;
        END;                      { ARRAYTYP }

      BEGIN                       { TYP }
        TP := NOTYP;
        RF := 0;
        SZ := 0;
        TEST(TYPEBEGSYS, FSYS, 10);
        IF SY IN TYPEBEGSYS THEN BEGIN
          IF SY = IDENT THEN BEGIN
            X := LOC(ID);
            IF X <> 0 THEN WITH TAB[X]^ DO
              IF OBJ <> TYPE1 THEN ERROR(29) ELSE BEGIN
                TP := TYP;
                RF := REF;
                SZ := ADR;
                IF TP = NOTYP THEN ERROR(30);
              END;
            INSYMBOL;
          END ELSE
            IF SY = ARRAYSY THEN BEGIN
              INSYMBOL;
              IF SY = LBRACK THEN INSYMBOL ELSE BEGIN
                ERROR(11);
                IF SY = LPARENT THEN INSYMBOL
              END;
              TP := ARRAYS;
              ARRAYTYP(RF, SZ)
          END ELSE BEGIN          { RECORDS }
            INSYMBOL;
            ENTERBLOCK;
            TP := RECORDS;
            RF := B;
            IF LEVEL = LMAX THEN FATAL(5);
            LEVEL := LEVEL+1;
            DISPLAY[LEVEL] := B;
            OFFSET := 0;
            WHILE NOT(SY IN FSYS-[SEMICOLON, COMMA, IDENT]+[ENDSY]) DO BEGIN
              IF SY = IDENT THEN BEGIN (* field section *)
                T0 := T;
                ENTERVARIABLE;
                WHILE SY = COMMA DO BEGIN
                  INSYMBOL;
                  ENTERVARIABLE
                END;
                IF SY = COLON THEN INSYMBOL ELSE ERROR(5);
                T1 := T;
                TYP(FSYS+[SEMICOLON, ENDSY, COMMA, IDENT], ELTP, ELRF, ELSZ);
                WHILE T0 < T1 DO BEGIN
                  T0 := T0+1;
                  WITH TAB[T0]^ DO BEGIN
                    TYP := ELTP;
                    REF := ELRF;
                    NORMAL := True;
                    ADR := OFFSET;
                    OFFSET := OFFSET+ELSZ;
                  END;
                END;
              END;
              IF SY <> ENDSY THEN BEGIN
                IF SY = SEMICOLON THEN INSYMBOL ELSE BEGIN
                  ERROR(14);
                  IF SY = COMMA THEN INSYMBOL;
                END;
                TEST([IDENT, ENDSY, SEMICOLON], FSYS, 6);
              END;
            END;
            BTAB[RF].VSIZE := OFFSET;
            SZ := OFFSET;
            BTAB[RF].PSIZE := 0;
            INSYMBOL;
            LEVEL := LEVEL-1;
          END;
          TEST(FSYS, [], 6);
        END;
      END;                        { TYP }

      PROCEDURE PARAMETERLIST;    (* formal parameter list *)
      VAR TP : TYPES;
        RF, SZ, X, T0 : Integer;
        VALPAR : Boolean;
      BEGIN
        INSYMBOL;
        TP := NOTYP;
        RF := 0;
        SZ := 0;
        TEST([IDENT, VARSY], FSYS+[RPARENT], 7);
        WHILE SY IN[IDENT, VARSY] DO BEGIN
            IF SY <> VARSY THEN VALPAR := True ELSE BEGIN
              INSYMBOL;
              VALPAR := False
            END;
          T0 := T;
          ENTERVARIABLE;
          WHILE SY = COMMA DO BEGIN
            INSYMBOL;
            ENTERVARIABLE;
          END;
          IF SY = COLON THEN BEGIN
            INSYMBOL;
            IF SY <> IDENT THEN ERROR(2) ELSE BEGIN
              X := LOC(ID);
              INSYMBOL;
              IF X <> 0 THEN WITH TAB[X]^ DO
                IF OBJ <> TYPE1 THEN ERROR(29) ELSE BEGIN
                  TP := TYP;
                  RF := REF;
                    IF VALPAR THEN SZ := ADR ELSE SZ := 1
                END;
            END;
            TEST([SEMICOLON, RPARENT], [COMMA, IDENT]+FSYS, 14)
          END ELSE ERROR(5);
          WHILE T0 < T DO BEGIN
            T0 := T0+1;
            WITH TAB[T0]^ DO BEGIN
              TYP := TP;
              REF := RF;
              NORMAL := VALPAR;
              ADR := DX;
              LEV := LEVEL;
              DX := DX+SZ
            END
          END;
          IF SY <> RPARENT THEN BEGIN
            IF SY = SEMICOLON THEN INSYMBOL ELSE BEGIN
              ERROR(14);
              IF SY = COMMA THEN INSYMBOL
            END;
            TEST([IDENT, VARSY], [RPARENT]+FSYS, 6)
          END
        END;                      { while }
        IF SY = RPARENT THEN BEGIN
          INSYMBOL;
          TEST([SEMICOLON, COLON], FSYS, 6)
        END ELSE ERROR(4)
      END;                        { PARAMETERLIST }

      PROCEDURE CONSTDECLARATION;
      VAR C : CONREC;
      BEGIN
        INSYMBOL;
        TEST([IDENT], BLOCKBEGSYS, 2);
        WHILE SY = IDENT DO BEGIN
          ENTER(ID, KONSTANT);
          INSYMBOL;
          IF SY = EQL THEN INSYMBOL ELSE BEGIN
            ERROR(16);
            IF SY = BECOMES THEN INSYMBOL;
          END;
          CONSTANT([SEMICOLON, COMMA, IDENT]+FSYS, C);
          TAB[T]^.TYP := C.TP;
          TAB[T]^.REF := 0;
          IF C.TP = REALS THEN BEGIN
            ENTERREAL(C.R);
            TAB[T]^.ADR := C1;
          END ELSE TAB[T]^.ADR := C.I;
          TESTSEMICOLON;
        END;
      END;                        { CONSTDECLARATION }

      PROCEDURE TYPEDECLARATION;
      VAR TP : TYPES;
        RF, SZ, T1 : Integer;
      BEGIN
        INSYMBOL;
        TEST([IDENT], BLOCKBEGSYS, 2);
        WHILE SY = IDENT DO BEGIN
          ENTER(ID, TYPE1);
          T1 := T;
          INSYMBOL;
          IF SY = EQL THEN INSYMBOL ELSE BEGIN
            ERROR(16);
            IF SY = BECOMES THEN INSYMBOL
          END;
          TYP([SEMICOLON, COMMA, IDENT]+FSYS, TP, RF, SZ);
          WITH TAB[T1]^ DO BEGIN
            TYP := TP;
            REF := RF;
            ADR := SZ
          END;
          TESTSEMICOLON;
        END;
      END;                        { TYPEDECLARATION }

      PROCEDURE VARDECLARTION;
      VAR T0, T1, RF, SZ : Integer;
        TP : TYPES;
      BEGIN
        INSYMBOL;
        WHILE SY = IDENT DO BEGIN
          T0 := T;
          ENTERVARIABLE;
          WHILE SY = COMMA DO BEGIN
            INSYMBOL;
            ENTERVARIABLE;
          END;
          IF SY = COLON THEN INSYMBOL ELSE ERROR(5);
          T1 := T;
          TYP([SEMICOLON, COMMA, IDENT]+FSYS, TP, RF, SZ);
          WHILE T0 < T1 DO BEGIN
            T0 := T0+1;
            WITH TAB[T0]^ DO BEGIN
              TYP := TP;
              REF := RF;
              LEV := LEVEL;
              ADR := DX;
              NORMAL := True;
              DX := DX+SZ;
            END;
          END;
          TESTSEMICOLON;
        END;
      END;                        { VARDECLARTION }

      PROCEDURE PROCDECLARATION;
      VAR ISFUN : Boolean;
      BEGIN
        ISFUN := (SY = FUNCSY);
        INSYMBOL;
        IF SY <> IDENT THEN BEGIN
          ERROR(2);
          ID := '          '
        END;
        IF ISFUN THEN ENTER(ID, FUNKTION) ELSE ENTER(ID, PROZEDURE);
        TAB[T]^.NORMAL := True;
        INSYMBOL;
        BLOCK([SEMICOLON]+FSYS, ISFUN, LEVEL+1);
        IF SY = SEMICOLON THEN INSYMBOL ELSE ERROR(14);
        EMIT(32+Ord(ISFUN))       { EXIT }
      END;                        { procedure DECLARATION }

      (*---------------------------------------------------------STATEMENT--*)

      PROCEDURE STATEMENT(FSYS : SYMSET);
      VAR I : Integer;
        X : ITEM;
        PROCEDURE EXPRESSION(FSYS : SYMSET; VAR X : ITEM); Forward;

        PROCEDURE SELECTOR(FSYS : SYMSET; VAR V : ITEM);
        VAR X : ITEM; A, J : Integer;
        BEGIN                     (* SY IN [LPARENT, LBRACK, PERIOD] *)
          REPEAT
            IF SY = PERIOD THEN BEGIN
              INSYMBOL;           (* field selector *)
              IF SY <> IDENT THEN ERROR(2) ELSE BEGIN
                IF V.TYP <> RECORDS THEN ERROR(31)
                ELSE BEGIN        (* search field identifier *)
                  J := BTAB[V.REF].LAST;
                  TAB[0]^.NAME := ID;
                  WHILE TAB[J]^.NAME <> ID DO J := TAB[J]^.LINK;
                  IF J = 0 THEN ERROR(0);
                  V.TYP := TAB[J]^.TYP;
                  V.REF := TAB[J]^.REF;
                  A := TAB[J]^.ADR;
                  IF A <> 0 THEN EMIT1(9, A);
                END;
                INSYMBOL;
              END;
            END ELSE BEGIN        (* array selector *)
              IF SY <> LBRACK THEN ERROR(11);
              REPEAT
                INSYMBOL;
                EXPRESSION(FSYS+[COMMA, RBRACK], X);
                IF V.TYP <> ARRAYS THEN ERROR(28) ELSE BEGIN
                  A := V.REF;
                  IF ATAB[A].INXTYP <> X.TYP THEN ERROR(26)
                  ELSE IF ATAB[A].ELSIZE = 1 THEN EMIT1(20, A) ELSE EMIT1(21, A);
                  V.TYP := ATAB[A].ELTYP;
                  V.REF := ATAB[A].ELREF;
                END;
              UNTIL SY <> COMMA;
              IF SY = RBRACK THEN INSYMBOL ELSE BEGIN
                ERROR(12);
                IF SY = RPARENT THEN INSYMBOL
              END;
            END;
          UNTIL NOT(SY IN[LBRACK, LPARENT, PERIOD]);
          TEST(FSYS, [], 6);
        END;                      { SELECTOR }

        PROCEDURE CALL(FSYS : SYMSET; I : Integer);
        VAR X : ITEM;
          LASTP, CP, K : Integer;
        BEGIN
          EMIT1(18, I);           (* mark stack *)
          LASTP := BTAB[TAB[I]^.REF].LASTPAR;
          CP := I;
          IF SY = LPARENT THEN BEGIN (* actual parameter list *)
            REPEAT
              INSYMBOL;
              IF CP >= LASTP THEN ERROR(39) ELSE BEGIN
                CP := CP+1;
                IF TAB[CP]^.NORMAL THEN BEGIN (*  value parameter *)
                  EXPRESSION(FSYS+[COMMA, COLON, RPARENT], X);
                  IF X.TYP = TAB[CP]^.TYP THEN BEGIN
                    IF X.REF <> TAB[CP]^.REF THEN ERROR(36)
                    ELSE IF X.TYP = ARRAYS THEN EMIT1(22, ATAB[X.REF].SIZE)
                    ELSE IF X.TYP = RECORDS
                    THEN EMIT1(22, BTAB[X.REF].VSIZE)
                  END ELSE IF (X.TYP = INTS) AND(TAB[CP]^.TYP = REALS)
                  THEN EMIT1(26, 0) ELSE IF X.TYP <> NOTYP THEN ERROR(36);
                END ELSE BEGIN    (* variable parameter *)
                  IF SY <> IDENT THEN ERROR(2) ELSE BEGIN
                    K := LOC(ID);
                    INSYMBOL;
                    IF K <> 0 THEN BEGIN
                      IF TAB[K]^.OBJ <> VARIABLE THEN ERROR(37);
                      X.TYP := TAB[K]^.TYP;
                      X.REF := TAB[K]^.REF;
                      IF TAB[K]^.NORMAL THEN EMIT2(0, TAB[K]^.LEV, TAB[K]^.ADR)
                      ELSE EMIT2(1, TAB[K]^.LEV, TAB[K]^.ADR);
                      IF SY IN[LBRACK, LPARENT, PERIOD]
                      THEN SELECTOR(FSYS+[COMMA, COLON, RPARENT], X);
                      IF (X.TYP <> TAB[CP]^.TYP) OR(X.REF <> TAB[CP]^.REF)
                      THEN ERROR(36);
                    END;
                  END;
                END;
              END;
              TEST([COMMA, RPARENT], FSYS, 6);
            UNTIL SY <> COMMA;
            IF SY = RPARENT THEN INSYMBOL ELSE ERROR(4);
          END;
          IF CP < LASTP THEN ERROR(39); (* too few actual parameters *)
          EMIT1(19, BTAB[TAB[I]^.REF].PSIZE-1);
          IF TAB[I]^.LEV < LEVEL THEN EMIT2(3, TAB[I]^.LEV, LEVEL)
        END;                      { CALL }

        FUNCTION RESULTTYPE(A, B : TYPES) : TYPES;
        BEGIN
          IF (A > REALS) OR(B > REALS) THEN BEGIN
            ERROR(33);
            RESULTTYPE := NOTYP;
          END ELSE IF (A = NOTYP) OR(B = NOTYP) THEN RESULTTYPE := NOTYP
          ELSE IF A = INTS THEN IF B = INTS THEN RESULTTYPE := INTS
            ELSE BEGIN
              RESULTTYPE := REALS;
              EMIT1(26, 1);
          END ELSE BEGIN
            RESULTTYPE := REALS;
            IF B = INTS THEN EMIT1(26, 0)
          END;
        END;                      { RESULTTYPE }
