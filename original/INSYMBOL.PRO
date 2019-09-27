    PROCEDURE INSYMBOL;           (* reads next symbol *)
    LABEL 1, 2, 3;
    VAR I, J, K, E : Integer;

      PROCEDURE READSCALE;
      VAR S, SIGN : Integer;
      BEGIN
        NEXTCH;
        SIGN := 1;
        S := 0;
        IF CH = '+' THEN NEXTCH ELSE
          IF CH = '-' THEN BEGIN
            NEXTCH;
            SIGN := -1;
          END;
        IF NOT(CH IN['0'..'9'])
        THEN ERROR(40)
        ELSE REPEAT
          S := 10*S+Ord(CH)-Ord('0');
          NEXTCH;
        UNTIL NOT(CH IN['0'..'9']);
        E := S*SIGN+E;
      END;                        { READSCALE }

      PROCEDURE ADJUSTSCALE;
      VAR S : Integer;
        D, T : Real;
      BEGIN
        IF K+E > EMAX THEN ERROR(21)
        ELSE IF K+E < EMIN THEN RNUM := 0
        ELSE BEGIN
          S := Abs(E);
          T := 1.0;
          D := 10.0;
          REPEAT
            WHILE NOT Odd(S) DO BEGIN
              S := S DIV 2;
              D := Sqr(D);
            END;
            S := S-1;
            T := D*T;
          UNTIL S = 0;
          IF E >= 0 THEN RNUM := RNUM*T
          ELSE RNUM := RNUM/T;
        END;
      END;                        { ADJUSTSCALE }

    BEGIN                         { INSYMBOL }
1:    WHILE CH = ' ' DO NEXTCH;
      IF CHARTP[CH] = ILLEGAL THEN BEGIN
        NEXTCH;
        ERROR(24); WriteLn(' char is :: ', Ord(CH));
        GOTO 1;
      END;
      CASE CH OF

        'A'..'Z',
        'a'..'z' : BEGIN          (* identifier or wordsymbol *)
                     K := 0;
                     ID := '          ';
                     REPEAT
                       IF K < ALNG THEN BEGIN
                         K := K+1;
                         ID[K] := Upcase(CH);
                       END;
                       NEXTCH;
                     UNTIL (CH <> '_') AND(CHARTP[CH]IN[SPECIAL, ILLEGAL]);
                     I := 1;
                     J := NKW;    (*BINARY SEARCH*)
                     REPEAT K := (I+J) DIV 2;
                       IF ID <= KEY[K] THEN J := K-1;
                       IF ID >= KEY[K] THEN I := K+1;
                     UNTIL I > J;
                       IF I-1 > J THEN SY := KSY[K] ELSE SY := IDENT;
                   END;

        '0'..'9' : BEGIN          { NUMBER }
                     K := 0;
                     INUM := 0;
                     SY := INTCON;
                     REPEAT
                       INUM := INUM*10+Ord(CH)-Ord('0');
                       K := K+1;
                       NEXTCH;
                     UNTIL CHARTP[CH] <> NUMBER;
                     IF (K > KMAX) OR(INUM > NMAX) THEN BEGIN
                       ERROR(21);
                       INUM := 0;
                       K := 0;
                     END;
                     IF CH = '.' THEN BEGIN
                       NEXTCH;
                       IF CH = '.' THEN CH := ':'
                       ELSE BEGIN
                         SY := REALCON;
                         RNUM := INUM;
                         E := 0;
                         WHILE CHARTP[CH] = NUMBER DO BEGIN
                           E := E-1;
                           RNUM := 10.0*RNUM+(Ord(CH)-Ord('0'));
                           NEXTCH
                         END;
                         IF E = 0 THEN ERROR(40);
                         IF CH = 'E' THEN READSCALE;
                         IF E <> 0 THEN ADJUSTSCALE
                       END;
                     END ELSE IF CH = 'E' THEN BEGIN
                       SY := REALCON;
                       RNUM := INUM;
                       E := 0;
                       READSCALE;
                       IF E <> 0 THEN ADJUSTSCALE;
                     END;
                   END;

        ':' : BEGIN
                NEXTCH;
                IF CH = '=' THEN BEGIN
                  SY := BECOMES;
                  NEXTCH;
                END ELSE SY := COLON;
              END;

        '<' : BEGIN
                NEXTCH;
                IF CH = '=' THEN BEGIN
                  SY := LEQ;
                  NEXTCH;
                END ELSE IF CH = '>' THEN BEGIN
                  SY := NEQ;
                  NEXTCH;
                END ELSE SY := LSS;
              END;

        '>' : BEGIN
                NEXTCH;
                IF CH = '=' THEN BEGIN
                  SY := GEQ;
                  NEXTCH;
                END ELSE SY := GTR;
              END;

        '.' : BEGIN
                NEXTCH;
                IF CH = '.' THEN BEGIN
                  SY := COLON;
                  NEXTCH;
                END ELSE SY := PERIOD;
              END;

        '''' : BEGIN
                 K := 0;
2:               NEXTCH;
                 IF CH = '''' THEN BEGIN
                   NEXTCH;
                   IF CH <> '''' THEN GOTO 3
                 END;
                 IF SX+K = SMAX THEN FATAL(7);
                 STAB[SX+K] := CH;
                 K := K+1;
                 IF CC = 1 THEN K := 0 (*END OF LINE*)
                 ELSE GOTO 2;
3:               IF K = 1 THEN BEGIN
                   SY := CHARCON;
                   INUM := Ord(STAB[SX]);
                 END ELSE
                   IF K = 0 THEN BEGIN
                     ERROR(38);
                     SY := CHARCON;
                     INUM := 0;
                 END ELSE BEGIN
                   SY := Word;
                   INUM := SX;
                   SLENG := K;
                   SX := SX+K;
                 END;
               END;

        '(' : BEGIN
                NEXTCH;
                IF CH <> '*' THEN SY := LPARENT
                ELSE BEGIN        (* comment *)
                  NEXTCH;
                  REPEAT
                    WHILE CH <> '*' DO NEXTCH;
                    NEXTCH
                  UNTIL CH = ')';
                  NEXTCH;
                  GOTO 1;
                END
              END;

        '{' : BEGIN               (* comment *)
                WHILE CH <> '}' DO NEXTCH;
                NEXTCH;
                GOTO 1;
              END;

        '+', '-', '*', '/', ')', '=', ',', '[', ']', ';' :
          BEGIN
            SY := SPS[CH];
            NEXTCH;
          END;

        '$', '!', '@', '\', '^', '_', '?', '"', '&', '%' :
          BEGIN
            ERROR(24);
            WriteLn(' [ $!@\^_?"&%  ]');
            NEXTCH;
            GOTO 1;
          END;
      END;
    END;                          { INSYMBOL }
