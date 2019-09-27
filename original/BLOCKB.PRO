
        (*---------------------------------------------------EXPRESSION-----*)

        PROCEDURE EXPRESSION (* FSYS: SYMSET; var X: ITEM *) ;
      (*
         Note: dynamic variables for Y have been used due to the
               constraints imposed upon local variables in recursion.
      *)

        TYPE ITEMptr = ^ITEM;     (* static > dynamic : SCHOENING *)
        VAR Y : ITEMptr;
          OP : SYMBOL;

          PROCEDURE SIMPLEEXPRESSION(FSYS : SYMSET; VAR X : ITEM);
          VAR Y : ITEMptr;
            OP : SYMBOL;

            PROCEDURE TERM(FSYS : SYMSET; VAR X : ITEM);
            VAR Y : ITEMptr;
              OP : SYMBOL;
              TS : TYPSET;

              PROCEDURE FACTOR(FSYS : SYMSET; VAR X : ITEM);
              VAR I, F : Integer;

                PROCEDURE STANDFCT(n : Integer);
                VAR TS : TYPSET;
                  ParaCount : Integer;
                  HasParameters : Boolean;
                BEGIN             (*STANDARD FUNCTION NO. N*)
                  HasParameters := True;
                  IF SY = LPARENT THEN INSYMBOL
                  ELSE IF ((n >= 22) AND(n <= 45)) OR(n = 20) OR((n >= 48) AND(n <= 69))
                  {LOC_X, LOC_Y .... ALLYALIVE, ObjectScanned .... HaveRepairKit}
                  THEN BEGIN
                    HasParameters := False; {none of these tokens has parameters}
                    IF ((n >= 22) AND(n <= 38)) OR(n = 20) OR((n >= 48) AND(n <= 66))
                    {i.e., they are like constants}
                    THEN TAB[I]^.TYP := INTS
                    ELSE TAB[I]^.TYP := BOOLS;
                    EMIT1(8, n);
                  END
                  ELSE ERROR(9);
                  IF (n < 17) OR(n = 19) THEN BEGIN
                    EXPRESSION(FSYS+[RPARENT], X); {One parameter}
                    CASE n OF

                      { ABS, SQR } 0, 2 : BEGIN
                                            TS := [INTS, REALS];
                                            TAB[I]^.TYP := X.TYP;
                                            IF X.TYP = REALS THEN n := n+1
                                          END;

                      { ODD, CHR } 4, 5 : TS := [INTS];

                      { ORD } 6 : TS := [INTS, BOOLS, CHARS];

                      { SUCC, PRED } 7, 8 : BEGIN
                                              TS := [INTS, BOOLS, CHARS];
                                              TAB[I]^.TYP := X.TYP
                                            END;

                      { ROUND, TRUNC } 9, 10, 11, 12, 13, 14, 15, 16 :
                        { SIN, COS,... } BEGIN
                          TS := [INTS, REALS];
                          IF X.TYP = INTS THEN EMIT1(26, 0)
                        END;

                      { RANDOM } 19 : BEGIN
                                        TS := [INTS];
                                        TAB[I]^.TYP := X.TYP;
                                      END;
                    END;          (* case *)
                    IF X.TYP IN TS THEN EMIT1(8, n) ELSE
                      IF X.TYP <> NOTYP THEN ERROR(48);

                  END ELSE IF n = 21 THEN
                    BEGIN         {Distance -- has four parameters}
                      FOR ParaCount := 1 TO 3 DO
                        BEGIN
                          EXPRESSION(FSYS+[COMMA, RPARENT], X);
                          IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                          IF SY = COMMA THEN INSYMBOL ELSE ERROR(39);
                        END;
                      {Last parm} EXPRESSION(FSYS+[COMMA, RPARENT], X);
                      IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                      EMIT1(8, n); {Distance}
                      TAB[I]^.TYP := INTS;
                    END           {N = 21, i.e., Distance}

                  ELSE IF (n = 46) OR(n = 47) THEN {Two Parameters}
                    BEGIN         {ANGLE_TO or SCAN}
                      EXPRESSION(FSYS+[COMMA, RPARENT], X);
                      IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                      IF SY = COMMA THEN INSYMBOL ELSE ERROR(39);
                      {2nd parm} EXPRESSION(FSYS+[COMMA, RPARENT], X);
                      IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                      EMIT1(8, n); {Angle_To or Scan}
                      TAB[I]^.TYP := INTS;
                    END           {N = 46, i.e., ANGLE_TO or N = 47, i.e., SCAN}

                  ELSE IF ((n = 17) OR(n = 18)) THEN (* N in [17,18] *)
                    { EOF, EOLN -- not allowed} ERROR(0);
                  X.TYP := TAB[I]^.TYP;
                  IF HasParameters THEN IF SY = RPARENT THEN INSYMBOL ELSE ERROR(4)
                END;              (* STANDFCT *)

              BEGIN               (* FACTOR *)
                X.TYP := NOTYP;
                X.REF := 0;
                TEST(FACBEGSYS, FSYS, 58);
                WHILE SY IN FACBEGSYS DO BEGIN
                  {   ID   } IF SY = IDENT THEN BEGIN
                    I := LOC(ID);
                    INSYMBOL;
                    WITH TAB[I]^ DO CASE OBJ OF

                      KONSTANT : BEGIN
                                   X.TYP := TYP;
                                   X.REF := 0;
                                   IF X.TYP = REALS THEN EMIT1(25, ADR)
                                   ELSE EMIT1(24, ADR)
                                 END;

                      VARIABLE : BEGIN
                                   X.TYP := TYP;
                                   X.REF := REF;
                                   IF SY IN[LBRACK, LPARENT, PERIOD] THEN BEGIN
                                       IF NORMAL THEN F := 0 ELSE F := 1;
                                     EMIT2(F, LEV, ADR);
                                     SELECTOR(FSYS, X);
                                     IF X.TYP IN STANTYPS THEN EMIT(34)
                                   END ELSE BEGIN
                                     IF X.TYP IN STANTYPS THEN
                                         IF NORMAL THEN F := 1 ELSE F := 2
                                     ELSE
                                         IF NORMAL THEN F := 0 ELSE F := 1;
                                     EMIT2(F, LEV, ADR)
                                   END;
                                 END;

                      TYPE1, PROZEDURE : ERROR(44);

                      FUNKTION : BEGIN
                                   X.TYP := TYP;
                                   IF LEV <> 0 THEN CALL(FSYS, I) ELSE STANDFCT(ADR);
                                 END

                    END           (*CASE,WITH*)
                  END ELSE
                    IF SY IN[CHARCON, INTCON, REALCON] THEN BEGIN
                      IF SY = REALCON THEN BEGIN
                        X.TYP := REALS;
                        ENTERREAL(RNUM);
                        EMIT1(25, C1)
                      END ELSE
                        BEGIN
                          IF SY = CHARCON THEN X.TYP := CHARS
                          ELSE X.TYP := INTS;
                          EMIT1(24, INUM)
                        END;
                      X.REF := 0; INSYMBOL
                  END ELSE
                    {   (   } IF SY = LPARENT THEN BEGIN
                      INSYMBOL;
                      EXPRESSION(FSYS+[RPARENT], X);
                      IF SY = RPARENT THEN INSYMBOL ELSE ERROR(4);
                  END ELSE
                    {  NOT  } IF SY = NOTSY THEN
                      BEGIN
                        INSYMBOL;
                        FACTOR(FSYS, X);
                        IF X.TYP = BOOLS THEN EMIT(35) ELSE
                          IF X.TYP <> NOTYP THEN ERROR(32)
                      END;
                  TEST(FSYS, FACBEGSYS, 6)
                END               (*while*)
              END;                (*FACTOR*)

            BEGIN                 (*TERM*)
              New(Y);
              FACTOR(FSYS+[TIMES, RDIV, IDIV, IMOD, ANDSY], X);
              WHILE SY IN[TIMES, RDIV, IDIV, IMOD, ANDSY] DO BEGIN
                OP := SY;
                INSYMBOL;
                FACTOR(FSYS+[TIMES, RDIV, IDIV, IMOD, ANDSY], Y^);
                {  *  } IF OP = TIMES THEN BEGIN
                  X.TYP := RESULTTYPE(X.TYP, Y^.TYP);
                  CASE X.TYP OF
                    NOTYP : ;
                    INTS : EMIT(57);
                    REALS : EMIT(60);
                  END
                END ELSE
                  {  /  } IF OP = RDIV THEN BEGIN
                    IF X.TYP = INTS THEN BEGIN
                      EMIT1(26, 1);
                      X.TYP := REALS
                    END;
                    IF Y^.TYP = INTS THEN BEGIN
                      EMIT1(26, 0);
                      Y^.TYP := REALS
                    END;
                    IF (X.TYP = REALS) AND(Y^.TYP = REALS) THEN EMIT(61)
                    ELSE BEGIN
                      IF (X.TYP <> NOTYP) AND(Y^.TYP <> NOTYP) THEN ERROR(33);
                      X.TYP := NOTYP
                    END
                END ELSE
                  { AND } IF OP = ANDSY THEN BEGIN
                    IF (X.TYP = BOOLS) AND(Y^.TYP = BOOLS) THEN EMIT(56)
                    ELSE BEGIN
                      IF (X.TYP <> NOTYP) AND(Y^.TYP <> NOTYP) THEN ERROR(32);
                      X.TYP := NOTYP
                    END
                END ELSE
                  { DIV,MOD } BEGIN (*OP in [IDIV,IMOD]*)
                    IF (X.TYP = INTS) AND(Y^.TYP = INTS) THEN
                      IF OP = IDIV THEN EMIT(58) ELSE EMIT(59)
                    ELSE BEGIN
                      IF (X.TYP <> NOTYP) AND(Y^.TYP <> NOTYP) THEN ERROR(34);
                      X.TYP := NOTYP
                    END
                  END
              END;
              Dispose(Y);
            END (*TERM*) ;

          BEGIN                   (*SIMPLEEXPRESSION*)
            New(Y);
            { +, - } IF SY IN[PLUS, MINUS] THEN BEGIN
              OP := SY;
              INSYMBOL;
              TERM(FSYS+[PLUS, MINUS], X);
              IF X.TYP > REALS THEN ERROR(33)
              ELSE IF OP = MINUS THEN EMIT(36)
            END ELSE TERM(FSYS+[PLUS, MINUS, ORSY], X);
            WHILE SY IN[PLUS, MINUS, ORSY] DO BEGIN
              OP := SY;
              INSYMBOL;
              TERM(FSYS+[PLUS, MINUS, ORSY], Y^);
              { OR   } IF OP = ORSY THEN BEGIN
                IF (X.TYP = BOOLS) AND(Y^.TYP = BOOLS) THEN EMIT(51)
                ELSE BEGIN
                  IF (X.TYP <> NOTYP) AND(Y^.TYP <> NOTYP) THEN ERROR(32);
                  X.TYP := NOTYP
                END
              END ELSE BEGIN
                X.TYP := RESULTTYPE(X.TYP, Y^.TYP);
                CASE X.TYP OF
                  NOTYP : ;
                  INTS : IF OP = PLUS THEN EMIT(52) ELSE EMIT(53);
                  REALS : IF OP = PLUS THEN EMIT(54) ELSE EMIT(55);
                END;
              END;
            END;
            Dispose(Y);
          END;                    (* SIMPLEEXPRESSION *)

        BEGIN                     (*EXPRESSION*)
          New(Y);
          SIMPLEEXPRESSION(FSYS+[EQL, NEQ, LSS, LEQ, GTR, GEQ], X);
          IF SY IN[EQL, NEQ, LSS, LEQ, GTR, GEQ] THEN BEGIN
            OP := SY;
            INSYMBOL;
            SIMPLEEXPRESSION(FSYS, Y^);
            IF (X.TYP IN[NOTYP, INTS, BOOLS, CHARS]) AND(X.TYP = Y^.TYP) THEN
              CASE OP OF
                EQL : EMIT(45);
                NEQ : EMIT(46);
                LSS : EMIT(47);
                LEQ : EMIT(48);
                GTR : EMIT(49);
                GEQ : EMIT(50);
            END ELSE BEGIN
              IF X.TYP = INTS THEN BEGIN
                X.TYP := REALS;
                EMIT1(26, 1)
              END ELSE IF Y^.TYP = INTS THEN BEGIN
                Y^.TYP := REALS;
                EMIT1(26, 0);
              END;
              IF (X.TYP = REALS) AND(Y^.TYP = REALS) THEN CASE OP OF
                EQL : EMIT(39);
                NEQ : EMIT(40);
                LSS : EMIT(41);
                LEQ : EMIT(42);
                GTR : EMIT(43);
                GEQ : EMIT(44);
              END ELSE ERROR(35);
            END;
            X.TYP := BOOLS;
          END;
          Dispose(Y);
        END (*EXPRESSION*) ;

        PROCEDURE ASSIGNMENT(LV, AD : Integer);
        VAR X, Y : ITEM; F : Integer;
          (* TAB[I]^.OBJ in [VARIABLE,PROZEDURE] *)
        BEGIN
          X.TYP := TAB[I]^.TYP;
          X.REF := TAB[I]^.REF;
            IF TAB[I]^.NORMAL THEN F := 0 ELSE F := 1;
          EMIT2(F, LV, AD);
          IF SY IN[LBRACK, LPARENT, PERIOD] THEN SELECTOR([BECOMES, EQL]+FSYS, X);
          IF SY = BECOMES THEN INSYMBOL ELSE BEGIN
            ERROR(51);
            IF SY = EQL THEN INSYMBOL
          END;
          EXPRESSION(FSYS, Y);
          IF X.TYP = Y.TYP THEN
            IF X.TYP IN STANTYPS THEN EMIT(38) ELSE
              IF X.REF <> Y.REF THEN ERROR(46) ELSE
                IF X.TYP = ARRAYS THEN EMIT1(23, ATAB[X.REF].SIZE)
            ELSE EMIT1(23, BTAB[X.REF].VSIZE)
          ELSE
            IF (X.TYP = REALS) AND(Y.TYP = INTS) THEN BEGIN
              EMIT1(26, 0); EMIT(38)
          END ELSE
            IF (X.TYP <> NOTYP) AND(Y.TYP <> NOTYP) THEN ERROR(46)
        END;                      { ASSIGNMENT }

        PROCEDURE COMPOUNDSTMNT;
        BEGIN
          INSYMBOL;
          STATEMENT([SEMICOLON, ENDSY]+FSYS);
          WHILE SY IN[SEMICOLON]+STATBEGSYS DO BEGIN
            IF SY = SEMICOLON THEN INSYMBOL ELSE ERROR(14);
            STATEMENT([SEMICOLON, ENDSY]+FSYS)
          END;
          IF SY = ENDSY THEN INSYMBOL ELSE ERROR(57)
        END;                      { CompoundStatement }
