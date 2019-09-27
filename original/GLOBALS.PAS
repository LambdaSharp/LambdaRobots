(***** Compiler parameters "optimized" for speed, not error checking *****)
{$R-}                             {Range checking OFF}
{$B-}                             {Boolean short circuiting ON}
{$S+}                             {Stack checking OFF}
{$I+}                             {I/O checking ON}
{$D-}                             {NO Debug information generated}
{$A+}                             {Align data on word boundaries}
{$F-}                             {FAR calls only when needed}
{$N-}                             {No numeric coprocessor}

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
      QuickPascal and developed P-Robots (based on CRobots by Tom Poindexter),
      1988.  The current version of P-Robots was developed and compiled using
      Borland Pascal (Turbo 7.0).

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

UNIT Globals;

INTERFACE

CONST
  ALNG = 10;                      (* NO. OF SIGNIFICANT CHARS IN IDENTIFIERS *)
  AMAX = 60;                      (* SIZE OF array-TABLE   *)
  BMAX = 40;                      (* SIZE OF BLOCK-TABLE   *)
  C2MAX = 40;                     (* SIZE OF REAL CONSTANT TABLE      *)
  CSMAX = 30;                     (* MAX NO. OF CASES      *)
  CMAX = 4000;                    (* SIZE OF CODE          *)
  ERMAX = 59;                     (* MAX ERROR NO.         *)
  EMAX = 77;                      (* MAX EXPONENT of REAL NUMBERS     *)
  EMIN = -78;                     (* MIN EXPONENT          *)
  HEADER = 'Pascal Robots -- Version 3.0 -- 1993';
  KMAX = 7;                       (* max No. of significant digits    *)
  LLNG = 253;                     (* input line length     *)
  LINELENG = 132;                 (* output line length    *)
  LINELIMIT = 200;                (* max No. of run time output lines *)
  NKW = 29;                       (* No. of key words      *)
  NMAX = MaxInt;                  (* 2**16-1               *)
  LMAX = 7;                       (* maximum level         *)
  ORDMINCHAR = 0;                 (* ORD of first char     *)
  ORDMAXCHAR = 255;               (* ORD of last char      *)
  OMAX = 71;                      (* highest order code    *)
  SMAX = 400;                     (* size of string-table  *)
  STMAX = 5000;                   (* Run-Time stack space *)
  STKINCR = 900;                  (* Stack space available to each process *)
  PMAX = 5;                       (* max # of concurrent processes    *)
  TMAX = 400;                     (* size of identifier table         *)
  XMAX = MaxInt;                  (* 2**16-1                          *)

TYPE INDEX = -XMAX.. +XMAX;
  ALFA = ARRAY[1..ALNG] OF Char;
  FNAME = String[20];             (* [8] name + [2] drive  *)
  xOBJECT = (KONSTANT, VARIABLE, TYPE1, PROZEDURE, FUNKTION);
  TYPES = (NOTYP, INTS, REALS, BOOLS, CHARS, ARRAYS, RECORDS);
  CHTP = (LETTER, LOWCASE, NUMBER, SPECIAL, ILLEGAL);
  ITEM = RECORD
           TYP : TYPES;
           REF : INDEX;
         END;
  ORDER = RECORD
            F : -OMAX.. +OMAX;
            X : -LMAX.. +LMAX;
            Y : -NMAX.. +NMAX;
          END;

  TYPSET = SET OF TYPES;

  R_Name = String[8];

  TABPointer = ^TABRecord;

  TABRecord = RECORD              (* identifier table *)
                NAME : ALFA;
                LINK : INDEX;
                OBJ : xOBJECT;
                TYP : TYPES;
                REF : INDEX;
                NORMAL : Boolean;
                LEV : 0..LMAX;
                ADR : Integer;
              END;

  TABList = ARRAY[0..TMAX] OF TABPointer;

  CODEPointer = ^CODERecord;

  CODERecord = ORDER;

  CODEList = ARRAY[0..CMAX] OF CODEPointer;

VAR
  {P-ROBOTS Globals}

  BColor, MColor : Integer;
  RobotColor : ARRAY[1..4] OF Integer;
  FileName : ARRAY[1..5] OF FNAME;
  NumRobots : Integer;
  CurrentLine, CurrentFile : Integer;
  ReadingFromFile : Boolean;

  MatchPlay, UnLimited_Fuel, GameHasObstacles : Boolean;
  Match, MatchNumber, NumberOfObstacles : Integer;
  FriendlyName : ARRAY[1..4] OF R_Name;

  {Compiler Globals}
  DATA,
  LISTING,
  SOURCE : Text;
  SFILE,
  DFILE : FNAME;                  (* source & data file DISK name            *)
  CH : Char;                      (* previous  char read from source program *)
  STANTYPS : TYPSET;
  PROGNAME : ALFA;
  CmdLine : String[30];
  SS : ARRAY[1..25] OF Char;      (* used for P_CODE_IO *)
  m, n : Integer;
  option : Char;
  DEBUG : Boolean;                (* run-time program info on/off flag  *)
  CreatedRunTimeStack : Boolean;

(*
   ===============
   compiler tables  - (Global) used by INTERPRT and COMPILE
   ===============
*)

  TAB : TABList;                  (* identifier table -- stored in heap *)

  ATAB : ARRAY[1..AMAX] OF RECORD (* array-table *)
                             INXTYP, ELTYP : TYPES;
                             ELREF, LOW, HIGH, ELSIZE, SIZE : INDEX;
                           END;

  BTAB : ARRAY[1..BMAX] OF RECORD (* block-table  *)
                             LAST,
                             LASTPAR,
                             PSIZE,
                             VSIZE : INDEX;
                           END;

  STAB : ARRAY[0..SMAX] OF Char;  (* string table *)
  CODE : CODEList;                (* Compiled P-Code -- Stored in heap *)
  RCONST : ARRAY[1..C2MAX] OF Real;
  B : Integer;                    (* ---> index to BTAB *)

  IFLAG,                          (* INPUT  found in program head *)
  OFLAG,                          (* OUTPUT found in program head *)
  DFLAG : Boolean;                (* DATA   found in program head *)

  (*------------------------------------------------------COMPILE-----*)


  {===========================================================================}

IMPLEMENTATION


  {empty}


END.
