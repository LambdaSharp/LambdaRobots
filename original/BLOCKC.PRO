        PROCEDURE IFSTATEMENT;
        VAR X : ITEM;
          LC1, LC2 : Integer;
        BEGIN
          INSYMBOL;
          EXPRESSION(FSYS+[THENSY, DOSY], X);
          IF NOT(X.TYP IN[BOOLS, NOTYP]) THEN ERROR(17);
          LC1 := LC;
          EMIT(11);               { JMPC }
          IF SY = THENSY THEN INSYMBOL ELSE BEGIN
            ERROR(52);
            IF SY = DOSY THEN INSYMBOL;
          END;
          STATEMENT(FSYS+[ELSESY]);
          IF SY = ELSESY THEN BEGIN
            INSYMBOL;
            LC2 := LC;
            EMIT(10);
            CODE[LC1]^.Y := LC;
            STATEMENT(FSYS);
            CODE[LC2]^.Y := LC;
          END ELSE CODE[LC1]^.Y := LC;
        END;                      { IFSTATEMENT }

        PROCEDURE CASESTATEMENT;
        VAR X : ITEM;
          I, J, K, LC1 : Integer;
          CASETAB : ARRAY[1..CSMAX] OF RECORD
                                         Val,
                                         LC : INDEX
                                       END;
          EXITTAB : ARRAY[1..CSMAX] OF Integer;

          PROCEDURE CASELABEL;
          VAR LAB : CONREC; K : Integer;
          BEGIN
            CONSTANT(FSYS+[COMMA, COLON], LAB);
            IF LAB.TP <> X.TYP THEN ERROR(47) ELSE
              IF I = CSMAX THEN FATAL(6) ELSE BEGIN
                I := I+1;
                K := 0;
                CASETAB[I].Val := LAB.I;
                CASETAB[I].LC := LC;
                REPEAT
                  K := K+1
                UNTIL CASETAB[K].Val = LAB.I;
                IF K < I THEN ERROR(1); (*MULTIPLE DEFINITION*)
              END;
          END;                    (*CASELABEL*)

          PROCEDURE ONECASE;
          BEGIN IF SY IN CONSTBEGSYS THEN
              BEGIN CASELABEL;
                WHILE SY = COMMA DO BEGIN
                  INSYMBOL;
                  CASELABEL
                END;
                IF SY = COLON THEN INSYMBOL ELSE ERROR(5);
                STATEMENT([SEMICOLON, ENDSY]+FSYS);
                J := J+1;
                EXITTAB[J] := LC; EMIT(10)
              END
          END (*ONECASE*) ;

        BEGIN
          INSYMBOL;
          I := 0;
          J := 0;
          EXPRESSION(FSYS+[OFSY, COMMA, COLON], X);
          IF NOT(X.TYP IN[INTS, BOOLS, CHARS, NOTYP]) THEN ERROR(23);
          LC1 := LC; EMIT(12);    (*JMPX*)
          IF SY = OFSY THEN INSYMBOL ELSE ERROR(8);
          ONECASE;
          WHILE SY = SEMICOLON DO BEGIN
            INSYMBOL;
            ONECASE
          END;
          CODE[LC1]^.Y := LC;
          FOR K := 1 TO I DO BEGIN
            EMIT1(13, CASETAB[K].Val);
            EMIT1(13, CASETAB[K].LC)
          END;
          EMIT1(10, 0);
          FOR K := 1 TO J DO CODE[EXITTAB[K]]^.Y := LC;
          IF SY = ENDSY THEN INSYMBOL ELSE ERROR(57)
        END (*CASESTATEMENT*) ;

        PROCEDURE repeatSTATEMENT;
        VAR X : ITEM; LC1 : Integer;
        BEGIN
          LC1 := LC;
          INSYMBOL; STATEMENT([SEMICOLON, UNTILSY]+FSYS);
          WHILE SY IN[SEMICOLON]+STATBEGSYS DO BEGIN
            IF SY = SEMICOLON THEN INSYMBOL ELSE ERROR(14);
            STATEMENT([SEMICOLON, UNTILSY]+FSYS)
          END;
          IF SY = UNTILSY THEN BEGIN
            INSYMBOL; EXPRESSION(FSYS, X);
            IF NOT(X.TYP IN[BOOLS, NOTYP]) THEN ERROR(17);
            EMIT1(11, LC1)
          END ELSE ERROR(53)
        END (*repeatSTATEMENT*) ;

        PROCEDURE whileSTATEMENT;
        VAR X : ITEM; LC1, LC2 : Integer;
        BEGIN
          INSYMBOL;
          LC1 := LC;
          EXPRESSION(FSYS+[DOSY], X);
          IF NOT(X.TYP IN[BOOLS, NOTYP]) THEN ERROR(17);
          LC2 := LC; EMIT(11);
          IF SY = DOSY THEN INSYMBOL ELSE ERROR(54);
          STATEMENT(FSYS);
          EMIT1(10, LC1);
          CODE[LC2]^.Y := LC
        END;                      (* WHILESTATEMENT *)

        PROCEDURE FORSTATEMENT;
        VAR CVT : TYPES;
          X : ITEM;
          I, F, LC1, LC2 : Integer;
        BEGIN
          INSYMBOL;
          IF SY = IDENT THEN BEGIN
            I := LOC(ID);
            INSYMBOL;
            IF I = 0 THEN CVT := INTS
            ELSE IF TAB[I]^.OBJ = VARIABLE THEN BEGIN
              CVT := TAB[I]^.TYP;
              IF NOT TAB[I]^.NORMAL THEN ERROR(37)
              ELSE EMIT2(0, TAB[I]^.LEV, TAB[I]^.ADR);
              IF NOT(CVT IN[NOTYP, INTS, BOOLS, CHARS]) THEN ERROR(18)
            END ELSE BEGIN
              ERROR(37);
              CVT := INTS
            END
          END ELSE SKIP([BECOMES, TOSY, DOWNTOSY, DOSY]+FSYS, 2);
          IF SY = BECOMES THEN BEGIN
            INSYMBOL;
            EXPRESSION([TOSY, DOWNTOSY, DOSY]+FSYS, X);
            IF X.TYP <> CVT THEN ERROR(19);
          END ELSE SKIP([TOSY, DOWNTOSY, DOSY]+FSYS, 51);
          F := 14;
          IF SY IN[TOSY, DOWNTOSY] THEN BEGIN
            IF SY = DOWNTOSY THEN F := 16;
            INSYMBOL;
            EXPRESSION([DOSY]+FSYS, X);
            IF X.TYP <> CVT THEN ERROR(19)
          END ELSE SKIP([DOSY]+FSYS, 55);
          LC1 := LC; EMIT(F);
          IF SY = DOSY THEN INSYMBOL ELSE ERROR(54);
          LC2 := LC;
          STATEMENT(FSYS);
          EMIT1(F+1, LC2);
          CODE[LC1]^.Y := LC
        END;                      (* FORSTATEMENT *)

        PROCEDURE STANDPROC(n : Integer);
        VAR I, F : Integer;
          X, Y : ITEM;
        BEGIN
          CASE n OF
            1, 2,                 (* READ *)
            3, 4,                 { WRITE }
            5, 6,                 { WAIT, SIGNAL }
            7 : { ClrScr } ERROR(0); {Unknown identifier}

            8 : { Initialize } EMIT(65);
            9 : { Update } EMIT(66);
            10, 11 :              { DRIVE, CANNON }
              IF SY <> LPARENT THEN ERROR(9) ELSE BEGIN
                INSYMBOL;
                EXPRESSION(FSYS+[COMMA, RPARENT], X);
                IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                IF SY = COMMA THEN INSYMBOL ELSE ERROR(39);
                {2nd parm} EXPRESSION(FSYS+[COMMA, RPARENT], X);
                IF X.TYP <> INTS THEN EMIT1(8, 9); {Round and make INTEGER}
                EMIT(n+57);       {DRIVE or CANNON}
                IF SY = RPARENT THEN INSYMBOL ELSE ERROR(4)
              END;
            12 : { Finalize } EMIT(69);
            13 : { RaiseShield } EMIT(70);
            14 : { LowerShield } EMIT(71);
            15 : { RaiseCloak } EMIT(72);
            16 : { LowerCloak } EMIT(73);
            17 : { MakeRepairs } EMIT(74);
            18 : { StopRepairs } EMIT(75);
            19 : { PlaceBomb } EMIT(76);
            20 : { Detonate } EMIT(77);
          END                     (* CASE *)
        END;                      (* STANDPROC *)

      BEGIN                       (*STATEMENT*)
        IF SY IN STATBEGSYS+[IDENT] THEN
          CASE SY OF
            IDENT : BEGIN
                      I := LOC(ID);
                      INSYMBOL;
                      IF I <> 0 THEN CASE TAB[I]^.OBJ OF

                        KONSTANT,
                        TYPE1 : ERROR(45);
                        VARIABLE : ASSIGNMENT(TAB[I]^.LEV, TAB[I]^.ADR);
                        PROZEDURE : IF TAB[I]^.LEV <> 0 THEN CALL(FSYS, I)
                                    ELSE STANDPROC(TAB[I]^.ADR);
                        FUNKTION : IF TAB[I]^.REF = DISPLAY[LEVEL]
                                   THEN ASSIGNMENT(TAB[I]^.LEV+1, 0)
                                   ELSE ERROR(45);
                      END;        (* case *)
                    END;

            BEGINSY : IF ID = 'COBEGIN   ' THEN BEGIN
                        EMIT(4);
                        COMPOUNDSTMNT;
                        EMIT(5)
                      END ELSE COMPOUNDSTMNT;

            IFSY : IFSTATEMENT;
            CASESY : CASESTATEMENT;
            WHILESY : whileSTATEMENT;
            REPEATSY : repeatSTATEMENT;
            FORSY : FORSTATEMENT;
          END;
        TEST(FSYS, [], 14)
      END (*STATEMENT*) ;

    BEGIN                         (*BLOCK*)
      DX := 5;
      PRT := T;
      IF LEVEL > LMAX THEN FATAL(5);
      TEST([LPARENT, COLON, SEMICOLON], FSYS, 14);
      ENTERBLOCK;
      DISPLAY[LEVEL] := B;
      PRB := B;
      TAB[PRT]^.TYP := NOTYP;
      TAB[PRT]^.REF := PRB;
      IF (SY = LPARENT) AND(LEVEL > 1) THEN PARAMETERLIST;
      BTAB[PRB].LASTPAR := T;
      BTAB[PRB].PSIZE := DX;
      IF ISFUN THEN
        IF SY = COLON THEN BEGIN
          INSYMBOL;               (*FUNCTION TYPE*)
          IF SY = IDENT THEN BEGIN
            X := LOC(ID);
            INSYMBOL;
            IF X <> 0 THEN
              IF TAB[X]^.OBJ <> TYPE1 THEN ERROR(29) ELSE
                IF TAB[X]^.TYP IN STANTYPS THEN TAB[PRT]^.TYP := TAB[X]^.TYP
              ELSE ERROR(15)
          END ELSE SKIP([SEMICOLON]+FSYS, 2)
        END ELSE ERROR(5);
      IF SY = SEMICOLON THEN INSYMBOL ELSE ERROR(14);
      REPEAT
        IF SY = CONSTSY THEN CONSTDECLARATION;
        IF SY = TYPESY THEN TYPEDECLARATION;
        IF SY = VARSY THEN VARDECLARTION;
        BTAB[PRB].VSIZE := DX;
        WHILE SY IN[PROCSY, FUNCSY] DO PROCDECLARATION;
        TEST([BEGINSY], BLOCKBEGSYS+STATBEGSYS, 56)
      UNTIL SY IN STATBEGSYS;
      TAB[PRT]^.ADR := LC;
      INSYMBOL;
      STATEMENT([SEMICOLON, ENDSY]+FSYS);
      WHILE SY IN[SEMICOLON]+STATBEGSYS DO BEGIN
        IF SY = SEMICOLON THEN INSYMBOL ELSE ERROR(14);
        STATEMENT([SEMICOLON, ENDSY]+FSYS)
      END;
      IF SY = ENDSY THEN INSYMBOL ELSE ERROR(57);
      TEST(FSYS+[PERIOD], [], 6)
    END;                          { block }
