(***** Compiler parameters "optimized" for speed, not error checking *****)
{$R-}                             {Range checking OFF}
{$B-}                             {Boolean short circuiting ON}
{$S+}                             {Stack checking OFF}
{$I+}                             {I/O checking ON}
{$D-}                             {NO Debug information generated}
{$A+}                             {Align data on word boundaries}
{$F-}                             {FAR calls only when needed}
{$N-}                             {No numeric coprocessor}
{$M 65500,16384,655360}           {Turbo 3 default stack and heap}

(*

		  The P-Robots Compiler

   ...is based on a modified version of the PASCAL-S compiler to permit
   interleaved concurrent program execution.

   The changes to the normal PASCAL-S are from "Principles of Concurrent
   Programming by BEN-ARI.

   The original defining document for PASCAL-S is:

     PASCAL-S: A SUBSET AND ITS IMPLEMENTATION by N. WIRTH.


   HISTORY of this version of PASCAL-S:

   .  Original PASCAL-S developed by N. Wirth,  1976.

   .  PASCAL-S modified for the HP/3000 by D. Greer.

   .  M. Ben-Ari developed additions to the Pascal language to provide
      for simulated concurrency and gave source code for incorporating
      concurrency into Pascal-S.  See Ben-Ari : 'Principles of Concurrent
      Programming.  Pretice-Hall, 1982.

   .  B. Burd incorporated Ben-Ari's changes into PASCAL-S,
      developing CO-PASCAL for the VAX/11-750 under VMS, 1985.

   .  C. Schoening modified and enhanced CO-PASCAL for Turbo-Pascal v2.0
      under CP/M and MS-DOS, 1985.  See his article "CO-PASCAL" in
      Septmember, 1986 issue of Computer Language.  Source code placed in
      public domain and available from Computer Language Forum (GO CLM-1)
      on CompuServe.

   .  David Malmberg modified and enhanced for Turbo Pascal 4.0/5.0 and
      and QuickPascal and developed P-Robots (based on CRobots by Tom
      Poindexter), 1988.

   .  Version 4.0 with IDE released as "Freeware" in 1994.

   OTHER references of interest include:

   .  Welch and Hay, A Model Implementation of Standard Pascal,
      Prentice-Hall, 1986.  This is a fully-featured Pascal compiler
      (not a subset like Pascal-S) written in Pascal and explained in detail.

   .  Rees and Robson, Practical Compiling with Pascal-S, Addison-Wesley,
      1988.  A book length discussion of writing and using a Pascal-S
      compiler, with examples on how to extend the language and the compiler.

   .  Brinch Hansen, On Pascal Compilers, Prentice-Hall, 1985.  Develops
      a compiler for a subset of Pascal, similar to Wirth's Pascal-S.

   .  R. E. Berry, Programming Language Translation, Halsted Press:
      a division of John Wiley & sons, 1983 (corrected) edition.
      The best reference for understanding P-Robots and CO-PASCAL because
      it sticks to Wirth's original nomenclature and program structure,
      rather than renaming everything to be more self-documenting (as do
      all of the other references given above).

   .  Gary Bricault, Juggling Multiple Processes, BYTE, May 1988.  A
      discussion of Ben-Ari's approach to concurrency in Pascal-S.  Source
      code for concurrent integer-only Pascal-S (to accompany article)
      distributed by BYTE and available on BIX for downloading.

*)


PROGRAM P_Robots;

USES
  DOS,
  CRT,
  Globals,
  CompUnit,
  RoboUnit;


  (*----------------------------------------------------INTERPRET------*)


  PROCEDURE INTERPRET;

    {$I INTERPT.PRO  }


  (*---------------------------------------------------TITLE----------*)

  PROCEDURE Title;

  TYPE S = String[80];

  CONST
    cw1 = ' Pascal Robots -- Version 4.0 ';
    cw2 = ' Copyrighted 1994 -- All Rights Reserved ';
    cw3 = ' by David R. Malmberg ';
    cw4 = ' Distributed by ';
    cw5 = ' Softworks ';
    cw6 = ' 829 Fifteenth Street ';
    cw7 = ' Hermosa Beach, California  90254 ';

    { Center }
    {Write the string of text centered}
    {on an 80-column display. }

    PROCEDURE center(st : S);
    CONST Blanks = '                                            ';
    VAR I : Integer;
    BEGIN
      {delete leading blanks}
      IF Length(st) > 1 THEN WHILE st[1] = ' ' DO
        st := Copy(st, 2, Length(st)-1);
      Write(Copy(Blanks, 1, (80-Length(st)) DIV 2));
      WriteLn(st);
    END;                          {center}

  BEGIN
    ClrScr;
    HideCursor;
    SetColors;
    ClrScr;
    TextColor(RobotColor[2]);
    WriteLn;
    WriteLn;
    WriteLn;
    center('P-ROBOTS');
    TextColor(RobotColor[1]);
    WriteLn;
    WriteLn;
    WriteLn;
    WriteLn;
    WriteLn;
    WriteLn;
    center(cw1);
    center(cw2);
    center(cw3);
    TextColor(RobotColor[3]);
    WriteLn;
    WriteLn;
    WriteLn;
    WriteLn;
    center(cw4);
    center(cw5);
    center(cw6);
    center(cw7);
    Delay(7000);
  END;

  PROCEDURE HELP;
  BEGIN
    WriteLn(' The correct syntax is :  P-ROBOTS ROBOT1 ... ROBOT4');
    WriteLn(' where ROBOT1, ROBOT2, ROBOT3 and ROBOT4 are names of ROBOTS.');
    WriteLn;
    WriteLn(' For example : P-ROBOTS SNIPER BOMBER');
    WriteLn(' would play and display a single match between the robots SNIPER and BOMBER.');
    WriteLn;
    WriteLn(' To play multiple matches (that are NOT displayed) enter an optional /MNNN');
    WriteLn(' where NNN represents the number of the matches you wish to play.');
    WriteLn;
    WriteLn(' For example : P-ROBOTS GHOST SNIPER KILLER BOMBER /M50');
    WriteLn(' would play 50 matches between the robots GHOST, SNIPER, KILLER and BOMBER.');
    WriteLn;
    WriteLn(' To play with unlimited fuel resources, enter an optional /U');
    WriteLn;
    WriteLn(' To play with 1 to 5 obstructions, enter an optional /On');
    WriteLn(' where n is a number from 1 to 5, e.g., /O2 would create two obstructions.');
    WriteLn;
    Halt;
  END;

  PROCEDURE CreateHeapSpace;
  VAR I : Integer;
  BEGIN
    FOR I := 0 TO TMAX DO New(TAB[I]);
    FOR I := 0 TO CMAX DO New(CODE[I]);
  END;


  (*---------------------------------------------------P-Robots Main-----*)

VAR I, Spot : Integer;
  F_Error : Boolean;

BEGIN                             { MAIN }
  IF (ParamCount < 1) OR (ParamCount > 9) THEN HELP;
  UnLimited_Fuel := FALSE;        (* Default is limited fuel *)
  GameHasObstacles := False;      (* Default is no obstacles *)
  DoingMatchStats := False;
  CreatedRunTimeStack := False;
  NumberOfObstacles := 0;
  NumRobots := 0;
  SpeedOption := 7;               {Fast is default speed}
  IQOption := 1;                  (* relatively "stupid", slow CPU for Robots' computer *)
  F_Error := False;
  MatchPlay := False;
  AnotherContest := 'Y';
  FOR I := 1 TO ParamCount DO
    BEGIN
      SFILE := ParamStr(I);
      FOR m := 1 TO Length(SFILE) DO SFILE[m] := Upcase(SFILE[m]);
      IF POS('.', SFILE) <> 0 THEN
        BEGIN
          Spot := POS('.', SFILE);
          SFILE := Copy(SFILE, Spot, 255); {strip off file extension}
        END;
      IF (SFILE[1] = '/') AND (SFILE[2] = 'U')
      THEN UnLimited_Fuel := TRUE
      ELSE IF (SFILE[1] = '/') AND (SFILE[2] = 'M')
      THEN BEGIN
        MatchPlay := True;
        SFILE := Copy(SFILE, 3, 255);
        Val(SFILE, MatchNumber, m);
        IF m <> 0 THEN
          BEGIN
            WriteLn('Not a valid number in /M specification.');
            F_Error := True;
          END;
        IF MatchNumber < 0 THEN   {Create summary statistics file}
          BEGIN
            DoingMatchStats := True;
            MatchNumber := -MatchNumber; {Reverse sign to positive}
            Assign(MATCHSTATS, 'RESULTS.PRB');
            {$I-}
            Rewrite(MATCHSTATS);
            {$I+}
            IF IoResult <> 0 THEN
              BEGIN
                WriteLn('Cannot open file : RESULTS.PRB');
                Halt;
              END;
          END;                    {MatchNumber negative}
      END
      ELSE IF (SFILE[1] = '/') AND (SFILE[2] = 'O')
      THEN BEGIN
        GameHasObstacles := True;
        SFILE := Copy(SFILE, 3, 255);
        Val(SFILE, NumberOfObstacles, m);
        IF m <> 0 THEN
          BEGIN
            WriteLn('Not a valid number in /O specification.');
            F_Error := True;
          END;
        IF NumberOfObstacles < 1 THEN
          BEGIN
            NumberOfObstacles := 0;
            GameHasObstacles := False;
          END;
        IF NumberOfObstacles >= 5
        THEN NumberOfObstacles := 5; {5 is maximum number of Obstacles}
      END
      ELSE IF (SFILE[1] = '/') AND (SFILE[2] = 'S')
      THEN BEGIN
        SFILE := Copy(SFILE, 3, 255);
        Val(SFILE, SpeedOption, m);
        IF m <> 0 THEN
          BEGIN
            WriteLn('Not a valid number in /S specification.');
            F_Error := True;
          END;
        IF SpeedOption < 1 THEN
          SpeedOption := 1;
        IF SpeedOption > 10
        THEN SpeedOption := 10;
      END ELSE
        BEGIN
          Assign(SOURCE, SFILE+'.PR');
          {$I-}
          Reset(SOURCE);
          Close(SOURCE);
          {$I+}
          IF IoResult <> 0 THEN
            BEGIN
              WriteLn('Can not find file : ', SFILE+'.PR');
              F_Error := True;
            END
          ELSE BEGIN
            NumRobots := NumRobots+1;
            IF NumRobots <= 4
            THEN FileName[NumRobots] := SFILE
            ELSE BEGIN
              WriteLn('Can not have more than four Robots.');
              F_Error := True;
            END;
          END;
        END;
    END;

  IF F_Error THEN Halt;

  DEBUG := True;

  IF DEBUG THEN
    BEGIN
      Assign(LISTING, 'LISTING.TXT');
      {$I-}
      Rewrite(LISTING);
      {$I+}
      IF IoResult <> 0 THEN
        BEGIN
          WriteLn('Cannot open file : LISTING.TXT');
          Halt;
        END;
    END;                          {Debug}

  Assign(SOURCE, FileName[1]+'.PR');
  {$I-}
  Reset(SOURCE);                  {get ready to read first file}
  {$I+}



  Title;
  ScrInit; {Sets screen-related variables -- like CRT rows and columns}
  ClrScr;
  HideCursor;
  CreateHeapSpace;
  compile;
  ClrScr;
  AnotherContest := 'Y';
  IF MatchPlay
  THEN WHILE AnotherContest = 'Y' DO
    BEGIN
      FOR Match := 1 TO MatchNumber DO INTERPRET;
      WriteLn;
      Write('Another Contest (Y/N) ? ');
      AnotherContest := Readkey;
      AnotherContest := UpCase(AnotherContest);
    END
  ELSE WHILE AnotherContest = 'Y' DO
   BEGIN
     HideCursor;
     INTERPRET;
   END;
  WriteLn;
  Close(LISTING);
  Erase(LISTING);
  IF DoingMatchStats THEN Close(MATCHSTATS);
  RestoreCursor;
  IF Height <> 25 THEN TextMode(C80);
END.

