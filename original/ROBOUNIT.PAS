(***** Compiler parameters "optimized" for speed, not error checking *****)
{$R-}                             {Range checking OFF}
{$B-}                             {Boolean short circuiting ON}
{$S+}                             {Stack checking OFF}
{$I+}                             {I/O checking ON}
{$D-}                             {NO Debug information generated}
{$A+}                             {Align data on word boundaries}
{$F-}                             {FAR calls only when needed}
{$N-}                             {No numeric coprocessor}

UNIT RoboUnit;

INTERFACE

USES
  CRT,
  DOS,
  Globals;

  (* INTERPT Stuff *)

TYPE PTYPE = 0..PMAX;

VAR
  CURPR : PTYPE;

  PTAB : ARRAY[PTYPE] OF RECORD
                           PC : Integer; (* PROGRAM COUNTER *)
                           T : Integer; (* TOP STACK INDEX *)
                           B : Integer; (* BASE INDEX *)
                           DISPLAY : ARRAY[1..LMAX] OF Integer;
                           STACKSIZE : Integer;
                           SUSPEND : Integer;
                           ACTIVE : Boolean
                         END;

  PFLAG : Boolean;
  LNCNT, OCNT, BLKCNT, CHRCNT : LongInt; (* COUNTERS *)

  (* Pascal Robots - Stuff *)

CONST
  Nothing = 0;                    {Scanned Object CONSTs}
  Enemy = 1;
  Ally = 2;
  Obstruction = 3;
  XLight = 0;                     {Type of Armor CONSTs}
  XMedium = 1;
  XHeavy = 2;
  XWimp = 0;                      {Type of WarHead CONSTs}
  XNormal = 1;
  XPremium = 2;
  XEconomy = 0;                   {Type of Engine CONSTs}
  XCompact = 1;
  XStandard = 2;
  XLarge = 3;
  XExtraLarge = 4;

  NoGood = 10000;                 {Not valid or good value}
  RadToAng = 57.295779513;        {Convert from radians to degrees}
  AngToRad = 0.0174533;           {Convert from degrees to radians}
  UL = #218;                      {Screen character codes}
  UR = #191;
  LL = #192;
  LR = #217;
  VL = #179;
  HL = #196;
  MissileSym = #4;
  BombSym = #11;
  ObstacleSym = #177;

  Max_Turn_Speed = 50;            (* maximum speed for direction change *)
  MaxCycles = 400000;             (* maximum "CPU" cycles before ending contest *)
  Meters_Per_Jigger = 100;        (* Fuel consumptions rate divisor*)
  Jiggers_Per_Missile = 3;        (* Fuel required to fire a missile *)
  Cycles_Per_Jigger = 1000;       (* Fuel usage rate divisor *)

  (* missile constants *)
  Missile_Speed = 750;            (* how far in one motion cycle *)
  Missile_Per_Robot = 2;          (* number of active missiles per robot *)
  Explosion_Count = 4;            (* motion cycles before erasing exploding missile *)

  (* distance of damage factors *)
  Direct_Hit_Range = 5;
  Near_Hit_Range = 20;
  Far_Hit_Range = 40;
  Fuel_Per_Damage = 2;            {Fuel usage per unit of damage absorbed by shield}

  { Text fonts, 25 or 43/50 rows }
  Font25 = 1;
  Font28 = 2;
  Font50 = 3;

  MaxWordLength = 25;
  TotalOptionPoints = 10;         {Maximum number of posints for options}

TYPE
  S = String[250];
  Words = String[20];
  ExplChars = String[11];
  ShieldorCloakStatus = (Up, Down);
  ArmorType = (Light, Medium, Heavy); {Type of Armor}
  WarHeadType = (Wimp, NORMAL, Premium); {Type of WarHead}
  EngineType = (Economy, Compact, Standard, Large, ExtraLarge); {Type of Engine}
  OptionIndex = (Radar, Fuel, Engine, Armor, Warheads, Bombs, Shielding, Cloaking, Repairing);

  RobotType = RECORD
                NAME : R_Name;
                X : LongInt;      (* current x location 0 - 999 *)
                Y : LongInt;      (* current y location 0 - 999 *)
                Beg_X : LongInt;  (* beginning x location 0 - 999 *)
                Beg_Y : LongInt;  (* beginning y location 0 - 999 *)
                Range : LongInt;  (* distance traveled on this heading *)
                Last_X : LongInt; (* last plotted physical screen x *)
                Last_Y : LongInt; (* last plotted physical screen y *)
                Speed : Integer;  (* current speed *)
                MaxSpeed : Integer; (* Maximum speed of robot *)
                ScannerRange : Integer; (* Maximum range of robot's scanner *)
                Desired_Speed : Integer; (* desired speed *)
                Heading : Integer; (* current heading, 0 - 359 *)
                Desired_Heading : Integer; (* desired heading, 0 - 359 *)
                Damage : Integer; (* current damage sustained in percent *)
                Scan : Integer;   (* current scan direction *)
                Reload : Integer; (* number of cycles between reloading *)
                Fuel : Integer;   (* Number of "Jiggers" of Fuel left *)
                Shield : ShieldorCloakStatus; (* Shield state, i.e., Up or Down *)
                Cloak : ShieldorCloakStatus; (* Cloak state, i.e., Up or Down *)
                AntiCloakingDevice : Boolean; (* Can scan cloaked enemies ? *)
                TeamMember : Boolean; (* Member of team ? *)
                AllyNumber : Integer; (* Ally's robot number *)
                Engine : EngineType; (* Robot's engine type *)
                EngineJiggers : Integer; {"Jiggers" of fuel used per 100 meters of travel}
                Armor : ArmorType; (* Robot's armor *)
                MaxMissileRange : Integer; (* desired range of missile *)
                Warheads : WarHeadType; (* Robot's WarHead for missiles *)
                Bombs : Integer;  (* Number of bombs left *)
                Bomb_X : LongInt; (* bomb's x location 0 - 999 *)
                Bomb_Y : LongInt; (* bomb's y location 0 - 999 *)
                (* Amount of damage depends on Armor type *)
                Direct_Hit_Damage : Integer; (* Damage within 5 meters *)
                Near_Hit_Damage : Integer; (* Damage within 20 meters *)
                Far_Hit_Damage : Integer; (* Damage within 40 meters *)
                Collision_Damage : Integer; (* Damage for collision *)
                Direct_Hit_Damage_Bonus : Integer; (* Additional damage due to missile type *)
                Near_Hit_Damage_Bonus : Integer;
                Far_Hit_Damage_Bonus : Integer;
                HasShield : Boolean;
                ShieldJiggers : Integer;
                HasCloak : Boolean;
                CloakJiggers : Integer;
                HasRepairKit : Boolean;
                MakingRepairs : Boolean;
                RepairJiggers : Integer;
              END;

  Stat = (Available, Flying, Exploding);

  MissileType = RECORD
                  Status : Stat;  (* missile status *)
                  Beg_X : LongInt; (* beginning x, 0 - 999 *)
                  Beg_Y : LongInt; (* beginning y, 0 - 999 *)
                  Cur_X : LongInt; (* current x, 0 - 999 *)
                  Cur_Y : LongInt; (* current y, 0 -999 *)
                  Last_X : LongInt; (* last plotted screen x *)
                  Last_Y : LongInt; (* last plotted screen y *)
                  Heading : Integer; (* heading, 0 - 359 *)
                  Count : Integer; (* cycle count for exploding missiles *)
                  Range : Integer; (* range of missile *)
                  Curr_Dist : Integer; (* current distance from orgin *)
                END;

  Rectangle = RECORD
                Top : LongInt;    (* Top, 0 - 999 *)
                Bottom : LongInt; (* Bottom, 0 - 999 *)
                Right : LongInt;  (* Right, 0 - 999 *)
                Left : LongInt;   (* Left, 0 -999 *)
                ScreenTop : LongInt; (* Top, 1 - Height *)
                ScreenBottom : LongInt; (* Bottom, 1 - Height *)
                ScreenRight : LongInt; (* Right, 1 - Width *)
                ScreenLeft : LongInt; (* Left, 1 - Width *)
              END;


VAR
  MATCHSTATS : Text;              (* File for Match statistics *)
  ConfigFile : Text;
  DoingMatchStats : Boolean;
  Options : ARRAY[OptionIndex] OF Integer; (* Point values for "Options" *)
  DefaultOptions : ARRAY[OptionIndex] OF Integer; (* Default Point values for "Options" *)
  Obstacle : ARRAY[1..5] OF Rectangle;
  Robot : ARRAY[PTYPE] OF RobotType;
  Meters : ARRAY[PTYPE] OF Integer; (* Total distance traveled by Robot *)
  StartingFuel : ARRAY[PTYPE] OF Integer; (* Starting Fuel at beginning of battle *)
  DamagedFuel : ARRAY[PTYPE] OF Integer; (* Amount of Fuel used to aborb damage *)
  CurrentRepairPoints : ARRAY[PTYPE] OF Integer; (* Current Number of Repair points *)
  LastRepairPoints : ARRAY[PTYPE] OF Integer; (* Last Number of Repair points *)
  ObjectScanned : ARRAY[PTYPE] OF Integer; (* Last object scanned by Robot *)
  Cycles_Shield_Up : ARRAY[PTYPE] OF LongInt; (* Number of Cycles Shields are up *)
  Cycles_Cloak_Up : ARRAY[PTYPE] OF LongInt; (* Number of Cycles Cloaks are up *)
  Cycles_Repairing : ARRAY[PTYPE] OF LongInt; (* Number of Cycles repairs being made *)
  Missiles_Fired : ARRAY[PTYPE] OF Integer; (* # of missiles fired *)
  WinningDamage : ARRAY[PTYPE] OF Integer; (* Average Damage for games won *)
  AverageDamage : ARRAY[PTYPE] OF Integer; (* Average Damage for all games *)
  LongestGame : ARRAY[PTYPE] OF LongInt; (* CPU cycles for longest game won *)
  ShortestGame : ARRAY[PTYPE] OF LongInt; (* CPU cycles for shortest game won *)
  AverageGame : ARRAY[PTYPE] OF LongInt; (* Average CPU cycles for games won *)
  LosingGame : ARRAY[PTYPE] OF LongInt; (* Average CPU cycles to lose game *)
  SpeedOption : Integer;          {Speed = 1, slowest ... Speed = 10, fastest}
  Cycles_Per_Motion : Integer;    (* number of cycles before motion update *)
  Speed_Adjustment : Integer;     (* speed adjustment factor *)
  Acceleration : Integer;         (* acceleration factor per motion cycle *)
  Reload_Cycles : Integer;        (* motion cycles before reloading possible *)
  IQOption : Integer;             (* "Speed" of "Smarts" of Robots' CPU *)
  Missile : ARRAY[PTYPE, 1..Missile_Per_Robot] OF MissileType;
  Dead : ARRAY[PTYPE] OF Boolean;
  NumberOfWins : ARRAY[PTYPE] OF Integer;
  Time : Integer;
  Ticks : LongInt;
  Winner, EndItNow : Boolean;
  MovementCounter : Integer;
  ShowIt, HideIt : ExplChars;
  ScreenLine : Integer;
  CurrentCycles, LastCycles : LongInt;
  Width, {61}                     {dimensions of playing field}
  Height : Integer;               {25 or 43/50}
  CRTRows,                        { Number of rows }
  CRTCols,                        { Number of columns }
  VideoMode : Byte;               { Video-mode }
  Regs : REGISTERS;               { Register variable }
  AnotherContest : Char;
  CloakDisplayCount : Integer;

FUNCTION Distance(X1, Y1, X2, Y2 : Integer) : Integer;

FUNCTION Angle_To(X1, Y1, X2, Y2 : Integer) : Integer;

FUNCTION Scan(Degree, Resolution : Integer) : Integer;

PROCEDURE Drive(Degree, Speed : Integer);

PROCEDURE Cannon(Degree, Range : Integer);

PROCEDURE Initialize_Robots;

PROCEDURE Update_Robots;

PROCEDURE Finalize_Robots;

PROCEDURE StartRepairs;           {Start Repairs for CURPR}

PROCEDURE StopRepairs;            {Stop Repairs for CURPR}

PROCEDURE DropBomb;               {Place Bomb for CURPR}

PROCEDURE DetonateBomb;           {Explode Bomb for CURPR}

PROCEDURE ScrInit;                {Initializes some Screen-related variables}


IMPLEMENTATION                    (***************************************)


  PROCEDURE GetFont(VAR CRTRows : Byte); {Gets number of rows on screen}
  BEGIN
    FillChar(Regs, SizeOf(Regs), 0);
    Regs.AH := $11;
    Regs.AL := $30;
    Regs.BH := $02;
    Intr($10, Regs);
    CRTRows := Regs.DL+1;
  END;


  PROCEDURE SetFont(Font : Byte); {Sets number of rows on screen : 25 or 43/50}
  BEGIN
    CASE Font OF
      Font25 : BEGIN
                 FillChar(Regs, SizeOf(Regs), 0);
                 Regs.AH := $00;
                 Regs.AL := VideoMode;
                 Intr($10, Regs);
                 CRTRows := 25;
               END;
      Font28 : BEGIN
                 FillChar(Regs, SizeOf(Regs), 0);
                 Regs.AH := $11;
                 Regs.AL := $11;
                 Intr($10, Regs);
                 GetFont(CRTRows);
               END;
      Font50 : BEGIN
                 FillChar(Regs, SizeOf(Regs), 0);
                 Regs.AH := $11;
                 Regs.AL := $12;
                 Intr($10, Regs);
                 GetFont(CRTRows);
               END;
    END;
  END;

  FUNCTION IsEGAorVGA : Boolean;  {Returns TRUE if EGA or VGA Display}
  VAR Answer : Boolean;
  BEGIN                           {From Neil Rubenking's TP 6.0 Tips & Tech, Page 741}
    FillChar(Regs, SizeOf(Regs), 0);
    Regs.AH := $12;               {Function 12h, Subfunction 10h}
    Regs.BL := $10;
    Intr($10, Regs);
    Answer := True;
    IF Regs.BH > 1 THEN Answer := False; {0 = color, 1 = mono, >1 = error}
    IF Regs.BL > 3 THEN Answer := False; {0..3=64K..256k video RAM, >3 = error}
    IsEGAorVGA := Answer;
  END;

  FUNCTION GetVideoMode : Byte;   {Returns the Video Mode}
  BEGIN
    FillChar(Regs, SizeOf(Regs), 0);
    Regs.AH := $0F;
    Intr($10, Regs);
    GetVideoMode := Regs.AL;
  END;


  PROCEDURE SetVideoMode(Mode : Byte); {Sets the Video Mode}
  BEGIN
    IF NOT Mode IN[$02, $03, $07] THEN Exit;
    FillChar(Regs, SizeOf(Regs), 0);
    Regs.AH := $00;
    Regs.AL := Mode;
    Intr($10, Regs);
  END;

  PROCEDURE ScrInit;              {Initializes some Screen-related variables}
  BEGIN
    VideoMode := GetVideoMode;
    IF NOT VideoMode IN[$02, $03, $07] THEN Exit; { Wrong video mode }
    CRTCols := 80;
    IF IsEGAorVGA AND(NOT MatchPlay)
    THEN BEGIN
      SetFont(Font50);
      TextMode(C80+Font8x8);
    END
    ELSE SetFont(Font25);
    GetFont(CRTRows);
    Width := 61;                  {dimensions of playing field}
    Height := CRTRows;            {25 or 43/50}
  END;

  FUNCTION MapX(X : LongInt) : LongInt;
    (* Map X (from 0 to 999) to within Screen Area Column 2 to Width-1 *)
  VAR Temp : Real;
  BEGIN
    Temp := X MOD 1000;
    Temp := Temp*(Width-3);
    MapX := 2+Trunc(Temp/999.0);
  END;

  FUNCTION MapY(Y : LongInt) : LongInt;
    (* Map Y (from 0 to 999) to within Screen Area Row Height-1 to 2 (i.e., inverted) *)
  BEGIN
    MapY := Height-1-(Y MOD 1000)*(Height-3) DIV 999;
  END;

  FUNCTION MapScreenX(X : LongInt) : LongInt;
    (* Map Screen X (from Column 2 to Width-1) to X (0 - 999) *)
  VAR Temp : Real;
  BEGIN
    IF (X <= 2)
    THEN MapScreenX := 0
    ELSE IF (X >= (Width-1))
    THEN MapScreenX := 999
    ELSE BEGIN
      Temp := 999.0*(X-2.0)/(1.0*(Width-3));
      MapScreenX := Trunc(Temp+0.5);
    END;
  END;

  FUNCTION MapScreenY(Y : LongInt) : LongInt;
    (* Map Screen Y (from Row Height-1 to 2 {i.e, inverted}) to Y (0 - 999) *)
  VAR Temp : Real;
  BEGIN
    IF (Y <= 2)
    THEN MapScreenY := 999
    ELSE IF (Y >= (Height-1))
    THEN MapScreenY := 0
    ELSE BEGIN
      Temp := 999.0-999.0*(Y-2.0)/(1.0*(Height-3));
      MapScreenY := Trunc(Temp+0.5);
    END;
  END;

  FUNCTION Inside_Rectangle(X, Y : LongInt; Rect : Rectangle) : Boolean;
  CONST
    Margin = 0;                   {Margin of error -- roundoff etc.}
  BEGIN
    Inside_Rectangle := (X >= (Rect.Left-Margin)) AND(X <= (Rect.Right+Margin))
                        AND(Y >= (Rect.Bottom-Margin)) AND(Y <= (Rect.Top+Margin));
  END;

  PROCEDURE MoveItToEdge(VAR X, Y : LongInt; VAR Moved : Boolean);
    {Moves Robot or Missile back to "edge" of Obstruction from inside}
  VAR
    I, TL, TR, TB, TT, Closest : Integer;
  BEGIN
    Moved := False;
    FOR I := 1 TO NumberOfObstacles DO
      IF Inside_Rectangle(X, Y, Obstacle[I]) THEN
        WITH Obstacle[I] DO
          BEGIN
            Moved := True;
            {Calculate distance to edges}
            TL := X-Left;
            TR := Right-X;
            TB := Y-Bottom;
            TT := Top-Y;
            {Find closest edge}
            Closest := TL;
            IF TR < Closest THEN Closest := TR;
            IF TB < Closest THEN Closest := TB;
            IF TT < Closest THEN Closest := TT;
            {Move just beyond closest edge -- outside of Obstacle}
            IF Closest = TL THEN X := Left-1;
            IF Closest = TR THEN X := Right+1;
            IF Closest = TB THEN Y := Bottom-1;
            IF Closest = TT THEN Y := Top+1;
          END;
  END;                            {MoveItToEdge}

  FUNCTION Inside_Screen_Rectangle(X, Y : LongInt; Rect : Rectangle) : Boolean;
  BEGIN
    Inside_Screen_Rectangle := (X >= Rect.ScreenLeft) AND(X <= Rect.ScreenRight)
                               AND(Y >= Rect.ScreenBottom) AND(Y <= Rect.ScreenTop);
  END;

  FUNCTION On_Field(X, Y : LongInt) : Boolean;
  VAR Ans : Boolean;
    I : Integer;
  BEGIN
    Ans := ((X >= 2) AND(X < Width) AND(Y >= 2) AND(Y < Height));
    IF GameHasObstacles
    THEN FOR I := 1 TO NumberOfObstacles DO
      IF Inside_Screen_Rectangle(X, Y, Obstacle[I]) THEN Ans := False;
    On_Field := Ans;
  END;

  FUNCTION Distance(X1, Y1, X2, Y2 : Integer) : Integer;
  VAR RealD, Rx1, Rx2, Ry1, Ry2 : Real;
  BEGIN
    Rx1 := X1;
    Rx2 := X2;
    Ry1 := Y1;
    Ry2 := Y2;
    RealD := Sqrt(Sqr(Rx1-Rx2)+Sqr(Ry1-Ry2));
    Distance := Trunc(RealD+0.5); {round for integer answer}
  END;

  FUNCTION Angle_To(X1, Y1, X2, Y2 : Integer) : Integer;
    {Returns angle (in degrees) from X1,Y1 to X2,Y2}
  VAR DeltaX, DeltaY, D : Real;
  BEGIN
    DeltaX := X2-X1;
    DeltaY := Y2-Y1;
    { arctan only returns -90 to +90, so figure out how to use }
    { the arctan() value.    }

    {If DeltaX is zero, we either move due north or south }
    IF (DeltaX = 0)
    THEN
      IF (DeltaY > 0)
      THEN D := PI/2              { north - 90 degrees}
      ELSE D := 3*PI/2            { south - 270 degrees}
    ELSE                          {DeltaX <> 0}
      IF (DeltaX < 0) THEN        { north/south-west, quadrant 2,3 }
        D := PI+ArcTan(DeltaY/DeltaX)
    ELSE
      IF (DeltaY < 0) THEN
        D := 2*PI+ArcTan(DeltaY/DeltaX) { south-east, quadrant 4 }
    ELSE
      D := ArcTan(DeltaY/DeltaX); { north-east, quadrant 1 }
    Angle_To := Round(D*RadToAng); {convert from Radians to Degrees}
  END;

  FUNCTION Intersect(VAR X1, Y1, X2, Y2 : Integer; Rect : Rectangle) : Boolean;
{ Based on a "clipping" routine from the Borland Turbo Graphix Toolbox.
  The routine checks to see if the line from (X1,Y1) to (X2,Y2) intersects
  the rectangle Rect and returns True or False accordingly.  If the line does
  intersect, then the points (X1,Y1) and (X2,Y2) are changed and returned as
  the points on the rectangle's perimeter where the intersection takes place.}
  VAR
    Ix1, Iy1, Ix2, Iy2, Dummy, X1Loc, X2Loc : Integer;
    IntersectLoc : Boolean;
    Temp : Real;
    X1Rect, Y1Rect, X2Rect, Y2Rect : Integer;

    FUNCTION Inside(X, Xx1, Xx2 : Integer) : Integer;
    BEGIN
      Inside := 0;
      IF X < Xx1 THEN
        Inside := -1
      ELSE IF X > Xx2 THEN
        Inside := 1;
    END;                          { Inside }

  BEGIN                           { Intersect }
    Intersect := True;
    IntersectLoc := True;
    WITH Rect DO
      BEGIN
        X1Rect := Left;
        Y1Rect := Bottom;
        X2Rect := Right;
        Y2Rect := Top;
      END;
    X1Loc := X1Rect;
    X2Loc := X2Rect;
    Ix1 := Inside(X1, X1Loc, X2Loc);
    Iy1 := Inside(Y1, Y1Rect, Y2Rect);
    Ix2 := Inside(X2, X1Loc, X2Loc);
    Iy2 := Inside(Y2, Y1Rect, Y2Rect);
    IF ((Ix1 < 0) AND(Ix2 < 0)) OR {both points to left of Rect}
    ((Ix1 > 0) AND(Ix2 > 0)) OR   {both points to right of Rect}
    ((Iy1 < 0) AND(Iy2 < 0)) OR   {both points below Rect}
    ((Iy1 > 0) AND(Iy2 > 0))      {both points above Rect}
    THEN Intersect := False
    ELSE IF (Ix1 OR Ix2 OR Iy1 OR Iy2) <> 0 THEN
      BEGIN
        IF X1 <> X2 THEN
          BEGIN
            IF Ix1 <> 0 THEN
              BEGIN
                IF Ix1 < 0 THEN
                  Dummy := X1Loc
                ELSE
                  Dummy := X2Loc;
                IF Y2 <> Y1 THEN
                  BEGIN
                    Temp := (Y2-Y1)/(X2-X1)*(Dummy-X1);
                    IF Temp > MaxInt THEN
                      Temp := MaxInt
                    ELSE IF Temp < -32767 THEN
                      Temp := -32767;
                    Y1 := Y1+Trunc(Temp);
                  END;
                X1 := Dummy;
              END;
            IF (Ix2 <> 0) AND(X1 <> X2) THEN
              BEGIN
                IF Ix2 < 0 THEN
                  Dummy := X1Loc
                ELSE
                  Dummy := X2Loc;
                IF Y2 <> Y1 THEN
                  BEGIN
                    Temp := (Y2-Y1)/(X2-X1)*(Dummy-X1);
                    IF Temp > MaxInt THEN
                      Temp := MaxInt
                    ELSE IF Temp < -32767 THEN
                      Temp := -32767;
                    Y2 := Y1+Trunc(Temp);
                  END;
                X2 := Dummy;
              END;
            Iy1 := Inside(Y1, Y1Rect, Y2Rect);
            Iy2 := Inside(Y2, Y1Rect, Y2Rect);
          END;
        IF Y1 <> Y2 THEN
          BEGIN
            IF Iy1 <> 0 THEN
              BEGIN
                IF Iy1 < 0 THEN
                  Dummy := Y1Rect
                ELSE
                  Dummy := Y2Rect;
                IF X1 <> X2 THEN
                  BEGIN
                    Temp := (X2-X1)/(Y2-Y1)*(Dummy-Y1);
                    IF Temp > MaxInt THEN
                      Temp := MaxInt
                    ELSE IF Temp < -32767 THEN
                      Temp := -32767;
                    X1 := X1+Trunc(Temp);
                  END;
                Y1 := Dummy;
              END;
            IF Iy2 <> 0 THEN
              BEGIN
                IF Iy2 < 0 THEN
                  Dummy := Y1Rect
                ELSE
                  Dummy := Y2Rect;
                IF X1 <> X2 THEN
                  BEGIN
                    Temp := (X2-X1)/(Y2-Y1)*(Dummy-Y1);
                    IF Temp > MaxInt THEN
                      Temp := MaxInt
                    ELSE IF Temp < -32767 THEN
                      Temp := -32767;
                    X2 := X1+Trunc(Temp);
                  END;
                Y2 := Dummy;
              END;
          END;
        Iy1 := Inside(Y1, Y1Rect, Y2Rect);
        Iy2 := Inside(Y2, Y1Rect, Y2Rect);
        IF (Iy1 <> 0) OR(Iy2 <> 0) THEN
          IntersectLoc := False;
        IF IntersectLoc THEN
          BEGIN
            Ix1 := Inside(X1, X1Loc, X2Loc);
            Ix2 := Inside(X2, X1Loc, X2Loc);
            IF (Ix2 <> 0) OR(Ix1 <> 0) THEN
              IntersectLoc := False;
          END;
        Intersect := IntersectLoc;
      END;
  END;                            { Intersect }

  FUNCTION Hits_Obstruction(X1, Y1, X2, Y2 : Integer;
                            VAR HitX, HitY, Dist : Integer) : Boolean;
  { This Function tests if the line from (X1,Y1) to (X2,Y2) intersects with
    any of the Obstructions and returns a True or False accordingly.
    If the answer is true, the location of the hit is returned by the
    point (HitX,HitY) and the distance from the starting point (X1,Y1) to
    the hit is returned as Dist.}
  VAR
    I, TempDist : Integer;
    BestX, BestY, BestDist : Integer;
    StartX, StartY, EndX, EndY : Integer;
    Answer : Boolean;

  BEGIN
    Answer := False;
    BestX := 0;
    BestY := 0;
    BestDist := NoGood;
    FOR I := 1 TO NumberOfObstacles DO
      BEGIN
        StartX := X1;
        StartY := Y1;
        WITH Obstacle[I] DO
          BEGIN
            IF StartX = Left
            THEN StartX := Left-1;
            IF StartX = Right
            THEN StartX := Right+1;
            IF StartY = Bottom
            THEN StartY := Bottom-1;
            IF StartY = Top
            THEN StartY := Top+1;
          END;
        EndX := X2;
        EndY := Y2;
        IF Intersect(StartX, StartY, EndX, EndY, Obstacle[I]) THEN
          BEGIN
            Answer := True;
            TempDist := Distance(X1, Y1, StartX, StartY); {find dist to hit}
            IF TempDist < BestDist THEN
              BEGIN
                BestX := StartX;
                BestY := StartY;
                BestDist := TempDist;
              END;
          END;                    {IF Intersect}
      END;
    IF Answer THEN
      BEGIN
        HitX := BestX;
        HitY := BestY;
        Dist := BestDist;
      END;
    Hits_Obstruction := Answer;
  END;                            { Hits_Obstruction }


  PROCEDURE UpdateDamage(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+7; Y := (RobotNum-1)*5+2;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].Damage:3);
  END;

  PROCEDURE UpdateFuel(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    IF NOT UnLimited_Fuel THEN
      BEGIN
        TextColor(RobotColor[RobotNum]);
        X := Width+15; Y := (RobotNum-1)*5+1;
        GotoXY(X, Y); Write('   ');
        GotoXY(X, Y); Write(Robot[RobotNum].Fuel:4);
      END;
  END;

  PROCEDURE UpdateScanDir(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+16; Y := (RobotNum-1)*5+2;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].Scan:3);
  END;

  PROCEDURE UpdateSpeed(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+7; Y := (RobotNum-1)*5+3;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].Speed:3);
  END;

  PROCEDURE UpdateHeading(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+16; Y := (RobotNum-1)*5+3;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].Heading:3);
  END;

  PROCEDURE UpdateXLoc(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+7; Y := (RobotNum-1)*5+4;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].X:3);
  END;

  PROCEDURE UpdateYLoc(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    X := Width+16; Y := (RobotNum-1)*5+4;
    GotoXY(X, Y); Write('   ');
    GotoXY(X, Y); Write(Robot[RobotNum].Y:3);
  END;

  FUNCTION Scan(Degree, Resolution : Integer) : Integer;
  CONST
    MaxRange = 1500;
  VAR
    ObstacleNum, ObstacleX, ObstacleY, ObstacleDist,
    Ans, I, Angle, Dist, TargetNum : Integer;
    DummyBoolean, QuadsSplit : Boolean;
    TopAngle, BottomAngle : Integer;
    TryHeading, NewX, NewY : Integer;

  BEGIN
    WHILE (Degree < 0) DO Degree := 360+Degree;
    Degree := Degree MOD 360;
    Resolution := Resolution MOD 11;
    Robot[CURPR].Scan := Degree;  {update Scanning Direction for current robot}
    IF NOT MatchPlay THEN UpdateScanDir(CURPR);
    Ans := NoGood;

    {Check for scanning of Obstacles - if any}
    IF GameHasObstacles
    THEN BEGIN
      ObstacleDist := NoGood;
      TryHeading := Degree;
      NewX := Robot[CURPR].X+Trunc(MaxRange*Cos(AngToRad*TryHeading));
      NewY := Robot[CURPR].Y+Trunc(MaxRange*Sin(AngToRad*TryHeading));
      DummyBoolean := Hits_Obstruction(Robot[CURPR].X, Robot[CURPR].Y, NewX, NewY,
                                       ObstacleX, ObstacleY, ObstacleDist);
      IF ObstacleDist < Ans
      THEN BEGIN
        Ans := ObstacleDist;
        ObjectScanned[CURPR] := Obstruction;
      END;                        {ObstacleDist < Ans}

      ObstacleDist := NoGood;
      TryHeading := Degree+Resolution;
      NewX := Robot[CURPR].X+Trunc(MaxRange*Cos(AngToRad*TryHeading));
      NewY := Robot[CURPR].Y+Trunc(MaxRange*Sin(AngToRad*TryHeading));
      DummyBoolean := Hits_Obstruction(Robot[CURPR].X, Robot[CURPR].Y, NewX, NewY,
                                       ObstacleX, ObstacleY, ObstacleDist);
      IF ObstacleDist < Ans
      THEN BEGIN
        Ans := ObstacleDist;
        ObjectScanned[CURPR] := Obstruction;
      END;                        {ObstacleDist < Ans}

      ObstacleDist := NoGood;
      TryHeading := Degree-Resolution;
      NewX := Robot[CURPR].X+Trunc(MaxRange*Cos(AngToRad*TryHeading));
      NewY := Robot[CURPR].Y+Trunc(MaxRange*Sin(AngToRad*TryHeading));
      DummyBoolean := Hits_Obstruction(Robot[CURPR].X, Robot[CURPR].Y, NewX, NewY,
                                       ObstacleX, ObstacleY, ObstacleDist);
      IF ObstacleDist < Ans
      THEN BEGIN
        Ans := ObstacleDist;
        ObjectScanned[CURPR] := Obstruction;
      END;                        {ObstacleDist < Ans}
   END;                          {GameHasObstacles}

    {Now, Scan for robots}
    TopAngle := Degree+Resolution;
    BottomAngle := Degree-Resolution;
    QuadsSplit := False;
    IF (TopAngle < 90)            {i.e., in first quadrant}
    AND(BottomAngle > 270)        {i.e., in fourth quadrant}
    THEN BEGIN
      TopAngle := TopAngle+30;    {transform angles to get both in first quadrant}
      BottomAngle := BottomAngle-330;
      QuadsSplit := True;
    END;
    TargetNum := 0;
    FOR I := 1 TO NumRobots DO
      IF (I <> CURPR) AND(NOT Dead[I]) AND ((Robot[I].Cloak = Down) OR Robot[CURPR].AntiCloakingDevice) THEN
        BEGIN
          Angle := Angle_To(Robot[CURPR].X, Robot[CURPR].Y, Robot[I].X, Robot[I].Y);
          IF QuadsSplit THEN IF (Angle < 90)
            THEN Angle := Angle+30
            ELSE Angle := Angle-330; {transform Angle also -- if appropriate}
          IF ((Angle <= TopAngle) AND(Angle >= BottomAngle))
          THEN BEGIN              {a robot is within the SCAN resolution}
            Dist := Distance(Robot[CURPR].X, Robot[CURPR].Y, Robot[I].X, Robot[I].Y);
            IF Dist < Ans THEN
              BEGIN
                Ans := Dist;      {set SCAN value to nearest robot}
                TargetNum := I;   {Robot number of target}
                IF TargetNum = Robot[CURPR].AllyNumber
                THEN ObjectScanned[CURPR] := Ally
                ELSE ObjectScanned[CURPR] := Enemy;
              END;
          END;
        END;

    IF (Ans = NoGood) OR(Ans > Robot[CURPR].ScannerRange)
    THEN BEGIN
      ObjectScanned[CURPR] := Nothing;
      Ans := 0;                   {Nothing in Scan area or out of range}
    END;
    Scan := Ans;
  END;
  {Scan}

  PROCEDURE Drive(Degree, Speed : Integer);
  BEGIN
    WHILE (Degree < 0) DO Degree := 360+Degree;
    Degree := Degree MOD 360;
    IF Speed < 0 THEN Speed := 0;
    IF Speed > Robot[CURPR].MaxSpeed THEN Speed := Robot[CURPR].MaxSpeed;
    Robot[CURPR].Desired_Heading := Degree;
    Robot[CURPR].Desired_Speed := Speed;
    IF Robot[CURPR].Fuel = 0 THEN Robot[CURPR].Desired_Speed := 0;
  END;

  PROCEDURE StartRepairs;         {For Current Robot}
  BEGIN
    IF Robot[CURPR].HasRepairKit THEN {A Repair Kit is available}
      IF Robot[CURPR].Speed = 0
      THEN Robot[CURPR].MakingRepairs := True;
  END;

  PROCEDURE StopRepairs;          {For Current Robot}
  BEGIN
    Robot[CURPR].MakingRepairs := False;
  END;

  PROCEDURE Explosion(X, Y : Integer; Characters : ExplChars);
  BEGIN
    X := MapX(X); Y := MapY(Y);
    {Top Row}
    IF On_Field(X-1, Y-1) THEN BEGIN GotoXY(X-1, Y-1); Write(Characters[1]); END;
    IF On_Field(X, Y-1) THEN BEGIN GotoXY(X, Y-1); Write(Characters[2]); END;
    IF On_Field(X+1, Y-1) THEN BEGIN GotoXY(X+1, Y-1); Write(Characters[3]); END;
    {Now - Middle explosion line}
    IF On_Field(X-2, Y) THEN BEGIN GotoXY(X-2, Y); Write(Characters[4]); END;
    IF On_Field(X-1, Y) THEN BEGIN GotoXY(X-1, Y); Write(Characters[5]); END;
    IF On_Field(X, Y) THEN BEGIN GotoXY(X, Y); Write(Characters[6]); END;
    IF On_Field(X+1, Y) THEN BEGIN GotoXY(X+1, Y); Write(Characters[7]); END;
    IF On_Field(X+2, Y) THEN BEGIN GotoXY(X+2, Y); Write(Characters[8]); END;
    {Bottom Row}
    IF On_Field(X-1, Y+1) THEN BEGIN GotoXY(X-1, Y+1); Write(Characters[9]); END;
    IF On_Field(X, Y+1) THEN BEGIN GotoXY(X, Y+1); Write(Characters[10]); END;
    IF On_Field(X+1, Y+1) THEN BEGIN GotoXY(X+1, Y+1); Write(Characters[11]); END;
  END;

  PROCEDURE DropBomb;             {Current Robot}
  BEGIN
    IF Robot[CURPR].Bombs > 0 THEN
      {A Bomb is available}
      IF (Robot[CURPR].Bomb_X = 0) AND(Robot[CURPR].Bomb_Y = 0)
      THEN                        {Not dropped already -- so it can be dropped now}
        BEGIN                     {Drop it!}
          Robot[CURPR].Bomb_X := Robot[CURPR].X;
          Robot[CURPR].Bomb_Y := Robot[CURPR].Y;
        END;
  END;

  PROCEDURE DisplayBomb(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    IF (Robot[RobotNum].Bomb_X <> 0) OR(Robot[RobotNum].Bomb_Y <> 0)
    THEN BEGIN                    {There is a bomb for this Robot -- so, show it!}
      TextColor(RobotColor[RobotNum]+Blink);
      X := MapX(Robot[RobotNum].Bomb_X); Y := MapY(Robot[RobotNum].Bomb_Y);
      IF On_Field(X, Y)
      THEN BEGIN
        GotoXY(X, Y);
        Write(BombSym);
      END;
    END;                          {Bomb exists}
  END;

  PROCEDURE DetonateBomb;         {Current Robot}
  VAR n, Dist, DamageHits : Integer;
  BEGIN
    IF (Robot[CURPR].Bombs > 0) AND (Robot[CURPR].Fuel > 0) THEN
      {A Bomb is available and have fuel}
      BEGIN                       {Boooomm!}
        IF (Robot[CURPR].Bomb_X <> 0) OR(Robot[CURPR].Bomb_Y <> 0)
        THEN BEGIN
          IF NOT MatchPlay THEN
            BEGIN                 {show explosion}
              TextColor(MColor+Blink);
              Explosion(Robot[CURPR].Bomb_X, Robot[CURPR].Bomb_Y, ShowIt);
              Delay(3000);        { Now -- erase explosion}
              Explosion(Robot[CURPR].Bomb_X, Robot[CURPR].Bomb_Y, HideIt);
            END;                  {Show Explosion}
          {inflict damage on nearby robots -- if any}
          FOR n := 1 TO NumRobots DO
            BEGIN
              DamageHits := 0;
              Dist := Distance(Robot[n].X, Robot[n].Y, Robot[CURPR].Bomb_X, Robot[CURPR].Bomb_Y);
              IF (Robot[n].Shield = Down)
              THEN BEGIN
                IF (Dist <= 300)
                THEN DamageHits := 75-(Dist DIV 4);
                Robot[n].Damage := Robot[n].Damage+DamageHits;
                IF Robot[n].Damage > 100 THEN Robot[n].Damage := 100;
                IF NOT MatchPlay THEN UpdateDamage(n);
              END;                {Shield is Down}
              IF (Robot[n].Shield = Up)
              THEN BEGIN
                IF (Dist <= 300)
                THEN DamageHits := 75-(Dist DIV 4);
                DamagedFuel[n] := DamagedFuel[n]+DamageHits;
              END;                {Shield is Up}
            END;                  {for Robot n}
        END;                      {Bomb has been dropped previously}
        Robot[CURPR].Bomb_X := 0;
        Robot[CURPR].Bomb_Y := 0;
        Dec(Robot[CURPR].Bombs);
      END;
  END;

  PROCEDURE Cannon(Degree, Range : Integer);
  VAR NumAvail, I : Integer;
  BEGIN
    WHILE (Degree < 0) DO Degree := 360+Degree;
    Degree := Degree MOD 360;
    IF Range < 1 THEN Range := 1;
    IF Range > Robot[CURPR].MaxMissileRange THEN Range := Robot[CURPR].MaxMissileRange;
    NumAvail := 0;
    FOR I := 1 TO Missile_Per_Robot DO
      IF (Missile[CURPR, I].Status = Available) THEN NumAvail := I;
    IF (NumAvail > 0) AND(Robot[CURPR].Fuel > 0) AND(Robot[CURPR].Reload = 0) THEN
      {Missile and Fuel Available and Not Reloading}
      BEGIN                       {shoot cannon}
        Missiles_Fired[CURPR] := Missiles_Fired[CURPR]+1; (* Update count *)
        Robot[CURPR].Reload := Reload_Cycles;
        Missile[CURPR, NumAvail].Status := Flying;
        Missile[CURPR, NumAvail].Beg_X := Robot[CURPR].X;
        Missile[CURPR, NumAvail].Beg_Y := Robot[CURPR].Y;
        Missile[CURPR, NumAvail].Cur_X := Robot[CURPR].X;
        Missile[CURPR, NumAvail].Cur_Y := Robot[CURPR].Y;
        Missile[CURPR, NumAvail].Heading := Degree;
        Missile[CURPR, NumAvail].Range := Range;
        Missile[CURPR, NumAvail].Curr_Dist := 0;
        Missile[CURPR, NumAvail].Count := Explosion_Count;
      END;
  END;


  { Skip_spaces }
  {Given a string of type s and an initial}
  {pointer into the string (type s), finds}
  {the next 'valid' character and returns }
  {its location in the pointer. }
  PROCEDURE Skip_spaces(sentence : S; VAR I : Integer; L : Integer);
  BEGIN
    WHILE ((sentence[I] < '0') OR(sentence[I] > 'z')) AND(I <= L) DO
      I := I+1;
  END;                            {skip_spaces}

  { Capitalize }
  {Given a string (of type s), capitalizes}
  {any lower case characters in it. }
  PROCEDURE Capitalize(VAR sentence : S);
  VAR I : Integer;                {index into s}
  BEGIN
    {delete leading blanks}
    I := 0;
    REPEAT
      I := I+1;
    UNTIL sentence[I] <> ' ';
    sentence := Copy(sentence, I, Length(sentence));
    {delete trailing blanks}
    I := Length(sentence)+1;
    REPEAT
      I := I-1;
    UNTIL sentence[I] <> ' ';
    sentence := Copy(sentence, 1, I);
    {capitalize}
    FOR I := 1 TO Length(sentence) DO
      IF (sentence[I]IN['a'..'z'])
      THEN sentence[I] := Char(Integer(sentence[I])-32);
  END;                            {capitalize}

  { Function Value }
  {Given a string (of type s), returns the}
  {integer value of the first number }
  {in the string. }
  FUNCTION Value(sentence : S) : Integer;
  VAR I, J, k, L : Integer;
    St : String[255];
    CODE : Integer;
  BEGIN
    L := Length(sentence);
    I := 0;
    {find first numeric character}
    REPEAT
      I := I+1;
    UNTIL (I > L) OR((sentence[I] >= '0') AND(sentence[I] <= '9'));
    {i now points to first number}
    J := I;
    REPEAT
      J := J+1;
    UNTIL (J > L) OR(NOT(sentence[J]IN['0'..'9']));
    {j now points to one past last number}
    St := '';
    FOR k := I TO (J-1) DO
      St := St+sentence[k];
    Val(St, I, CODE);
    IF CODE <> 0 THEN I := 0;
    Value := I;
  END;                            {value}

  { Function First_Word }
  {Given a string (of type s), returns the }
  {first word (made up of ASCII characters }
  {or first number (made up of digits) }
  {Returns a '' if no first word. }
  FUNCTION first_word(sentence : S) : Words;
  VAR I, J, L : Integer;
    St : S;
  BEGIN
    L := Length(sentence);
    St := '';
    I := 0;
    REPEAT                        {find first character of word/number}
      I := I+1;
    UNTIL (sentence[I]IN['a'..'z', 'A'..'Z']) OR(I > L);
    J := 1;
    IF (I > L) THEN first_word := ''
    ELSE BEGIN
      REPEAT                      {find last character of word/number}
        St := St+sentence[I];
        I := I+1; J := J+1;
      UNTIL (NOT(sentence[I]IN['a'..'z', 'A'..'Z'])) OR(I > L);
      IF Length(St) > MaxWordLength THEN St := Copy(St, 1, MaxWordLength);
      first_word := St;
    END;
  END;                            {fn. first_word}

  { File_Exists }
  {Boolean function to check if a }
  {file exists before trying to open it}
  FUNCTION File_Exists(FileName : Words) : Boolean; {From the 3.0 manual,}
  VAR fil : FILE;                 {chapter 14, page 97 }
  BEGIN
    Assign(fil, FileName);
    {$I-}
    Reset(fil);
    {$I+}
    IF IoResult = 0
    THEN BEGIN
      File_Exists := True;
      Close(fil);
    END ELSE File_Exists := False;
  END;

  PROCEDURE Set_Config(I : Integer);
    {Set Robot I's configuration to the values indicated by the Options vector}
  BEGIN
    CASE Options[Radar] OF
      0 : Robot[I].ScannerRange := 200; {Maximum range for robot's scanner}
      1 : Robot[I].ScannerRange := 400;
      2 : Robot[I].ScannerRange := 600; {Default value}
      3 : Robot[I].ScannerRange := 800;
      4 : Robot[I].ScannerRange := 1000;
      5 : BEGIN
            Robot[I].ScannerRange := 1200;
            Robot[I].AntiCloakingDevice := TRUE;
          END;
    END;                          (* Case *)
    CASE Options[Fuel] OF
      0 : Robot[I].Fuel := 750;   {Maximum fuel for robot's}
      1 : Robot[I].Fuel := 1000;
      2 : Robot[I].Fuel := 1250;  {Default value}
      3 : Robot[I].Fuel := 1500;
      4 : Robot[I].Fuel := 1750;
      5 : Robot[I].Fuel := 2000;
    END;                          (* Case *)
    CASE Options[Engine] OF
      0 : BEGIN
            Robot[I].Engine := Economy; {Type of engine for robot}
            Robot[I].EngineJiggers := 4; {"Jiggers" of fuel used per 100 meters of travel}
            Robot[I].MaxSpeed := 60; {Maximum speed of robot -- based on engine size}
          END;
      1 : BEGIN
            Robot[I].Engine := Compact;
            Robot[I].EngineJiggers := 5;
            Robot[I].MaxSpeed := 80;
          END;
      2 : BEGIN
            Robot[I].Engine := Standard; (* Default Value *)
            Robot[I].EngineJiggers := 6;
            Robot[I].MaxSpeed := 100;
          END;
      3 : BEGIN
            Robot[I].Engine := Large;
            Robot[I].EngineJiggers := 7;
            Robot[I].MaxSpeed := 120;
          END;
      4 : BEGIN
            Robot[I].Engine := ExtraLarge;
            Robot[I].EngineJiggers := 8;
            Robot[I].MaxSpeed := 140;
          END;
    END;                          (* Case *)
    CASE Options[Armor] OF
      0 : BEGIN
            Robot[I].Armor := Light; {Type of Armor for robot}
            Robot[I].Direct_Hit_Damage := 16; (* Damage within 5 meters *)
            Robot[I].Near_Hit_Damage := 8; (* Damage within 20 meters *)
            Robot[I].Far_Hit_Damage := 4; (* Damage within 40 meters *)
            Robot[I].Collision_Damage := 3; (* Damage for collision *)
            Robot[I].MaxSpeed := Robot[I].MaxSpeed+25; {Maximum speed of robot -- adjusted}
          END;
      1 : BEGIN
            Robot[I].Armor := Medium; {Default}
            Robot[I].Direct_Hit_Damage := 8;
            Robot[I].Near_Hit_Damage := 4;
            Robot[I].Far_Hit_Damage := 2;
            Robot[I].Collision_Damage := 2;
            Robot[I].MaxSpeed := Robot[I].MaxSpeed+0;
          END;
      2 : BEGIN
            Robot[I].Armor := Heavy;
            Robot[I].Direct_Hit_Damage := 4;
            Robot[I].Near_Hit_Damage := 2;
            Robot[I].Far_Hit_Damage := 1;
            Robot[I].Collision_Damage := 1;
            Robot[I].MaxSpeed := Robot[I].MaxSpeed-25;
          END;
    END;                          (* Case *)
    CASE Options[Warheads] OF
      0 : BEGIN
            Robot[I].Warheads := Wimp; {Type of Missile for robot}
            Robot[I].MaxMissileRange := 350; (* Maximum range of Missiles -- based on missile type *)
            Robot[I].Direct_Hit_Damage_Bonus := 0; (* Additional damage due to missile type *)
            Robot[I].Near_Hit_Damage_Bonus := 0;
            Robot[I].Far_Hit_Damage_Bonus := 0;
          END;
      1 : BEGIN
            Robot[I].Warheads := NORMAL; {Default}
            Robot[I].MaxMissileRange := 700;
            Robot[I].Direct_Hit_Damage_Bonus := 3;
            Robot[I].Near_Hit_Damage_Bonus := 2;
            Robot[I].Far_Hit_Damage_Bonus := 1;
          END;
      2 : BEGIN
            Robot[I].Warheads := Premium;
            Robot[I].MaxMissileRange := 1500;
            Robot[I].Direct_Hit_Damage_Bonus := 6;
            Robot[I].Near_Hit_Damage_Bonus := 4;
            Robot[I].Far_Hit_Damage_Bonus := 2;
          END;
    END;                          (* Case *)
    CASE Options[Bombs] OF
      0 : Robot[I].Bombs := 0;    {Number of bombs carried by robot -- Default is none}
      1 : Robot[I].Bombs := 3;
      2 : Robot[I].Bombs := 6;
      3 : Robot[I].Bombs := 9;
      4 : Robot[I].Bombs := 12;
      5 : Robot[I].Bombs := 15;
    END;                          (* Case *)
    IF Options[Shielding] = 0
    THEN BEGIN
      Robot[I].HasShield := False;
      Robot[I].ShieldJiggers := 0;
    END
    ELSE BEGIN                    {Default}
      Robot[I].HasShield := True;
      IF Robot[I].Armor = Light   {Type of Armor for robot}
      THEN Robot[I].ShieldJiggers := 3;
      IF Robot[I].Armor = Medium
      THEN Robot[I].ShieldJiggers := 6;
      IF Robot[I].Armor = Heavy
      THEN Robot[I].ShieldJiggers := 9;
    END;
    IF Options[Cloaking] = 0
    THEN BEGIN                    {Default}
      Robot[I].HasCloak := False;
      Robot[I].CloakJiggers := 0;
    END
    ELSE BEGIN
      Robot[I].HasCloak := True;
      Robot[I].CloakJiggers := 5;
    END;
    IF Options[Repairing] = 0
    THEN BEGIN                    {Default}
      Robot[I].HasRepairKit := False;
      Robot[I].RepairJiggers := 0;
    END
    ELSE BEGIN
      Robot[I].HasRepairKit := True;
      Robot[I].RepairJiggers := 4;
    END;
  END;                            {Set_Config}

  PROCEDURE Set_Config_To_Default(I : Integer);
    {Set Robot I's configuration to the "Default"}
  BEGIN
    Options[Radar] := 2;          {Maximum range for robot's scanner}
    Options[Fuel] := 2;           {Maximum fuel for robot's}
    Options[Engine] := 2;         {Type of engine for robot}
    Options[Armor] := 1;          {Type of Armor for robot}
    Options[Warheads] := 1;       {Type of Missile for robot}
    Options[Bombs] := 0;          {Determines the number of Bombs for robot}
    Options[Shielding] := 2;      {Default -- Robot has Shielding}
    Options[Cloaking] := 0;       {Default -- Robot has NO Cloaking}
    Options[Repairing] := 0;      {Default -- Robot has NO Repair Kit}
    Robot[I].Bomb_X := 0;
    Robot[I].Bomb_Y := 0;
    Set_Config(I);
  END;                            {Set_Config_To_Default}

  PROCEDURE Init_Robot(I : Integer);
    {Initialize Robot I}
  VAR J, OptPoints : Integer;
    Line : S;
    Fword : Words;
    OptValue : Integer;
  BEGIN
    Meters[I] := 0;
    Cycles_Shield_Up[I] := 0;
    Cycles_Cloak_Up[I] := 0;
    Cycles_Repairing[I] := 0;
    CurrentRepairPoints[I] := 0;
    LastRepairPoints[I] := 0;
    Missiles_Fired[I] := 0;
    ObjectScanned[I] := Obstruction;
    Robot[I].X := 0;
    Robot[I].Y := 0;
    Robot[I].Beg_X := 0;
    Robot[I].Beg_Y := 0;
    Robot[I].Range := 0;
    Robot[I].Last_X := -1;
    Robot[I].Last_Y := -1;
    Robot[I].Speed := 0;
    Robot[I].Desired_Speed := 0;
    Robot[I].Heading := 0;
    Robot[I].Desired_Heading := 0;
    Robot[I].Damage := 0;
    Robot[I].Scan := 0;
    Robot[I].Reload := 0;
    Robot[I].AntiCloakingDevice := False;
    Robot[I].TeamMember := False;
    Robot[I].AllyNumber := I;
    Robot[I].Shield := Down;
    Robot[I].Cloak := Down;
    DamagedFuel[I] := 0;
    Set_Config_To_Default(I);
    IF File_Exists(Robot[I].NAME+'.CFG')
    THEN BEGIN
      Assign(ConfigFile, Robot[I].NAME+'.CFG');
      Reset(ConfigFile);
      REPEAT
        ReadLn(ConfigFile, Line);
        Capitalize(Line);
        Fword := first_word(Line);
        OptPoints := Value(Line);
        IF Fword = 'RADAR' THEN Options[Radar] := OptPoints;
        IF Fword = 'FUEL' THEN Options[Fuel] := OptPoints;
        IF Fword = 'ENGINE' THEN Options[Engine] := OptPoints;
        IF Fword = 'ARMOR' THEN Options[Armor] := OptPoints;
        IF Fword = 'WARHEADS' THEN Options[Warheads] := OptPoints;
        IF Fword = 'BOMBS' THEN Options[Bombs] := OptPoints;
        IF Fword = 'SHIELDING' THEN Options[Shielding] := OptPoints;
        IF Fword = 'CLOAKING' THEN Options[Cloaking] := OptPoints;
        IF Fword = 'REPAIRING' THEN Options[Repairing] := OptPoints;
      UNTIL EoF(ConfigFile);
      Close(ConfigFile);
      OptPoints :=
        Options[Radar]+           {Maximum range for robot's scanner}
        Options[Fuel]+            {Maximum fuel for robot's}
        Options[Engine]+          {Type of engine for robot}
        Options[Armor]+           {Type of Armor for robot}
        Options[Warheads]+        {Type of Missile for robot}
        Options[Bombs]+           {Determines the number of Bombs for robot}
        Options[Shielding]+       {Default -- Robot has Shielding}
        Options[Cloaking]+        {Default -- Robot has NO Cloaking}
        Options[Repairing];       {Default -- Robot has NO Repair Kit}
      IF OptPoints = TotalOptionPoints
      THEN Set_Config(I)
      ELSE Set_Config_To_Default(I);
    END;                          {.CFG file exists}
    StartingFuel[I] := Robot[I].Fuel;
    FOR J := 1 TO Missile_Per_Robot DO
      BEGIN
        Missile[I, J].Status := Available;
        Missile[I, J].Last_X := -1;
        Missile[I, J].Last_Y := -1;
        Missile[I, J].Count := Explosion_Count;
      END;
    CloakDisplayCount := 0;
  END;

  PROCEDURE EraseSpot(X, Y : Integer);
    {Erase screen spot at X,Y}
  BEGIN
    IF On_Field(X, Y) THEN
      BEGIN
        GotoXY(X, Y);
        Write(' ');
      END;
  END;

  PROCEDURE DisplayRobot(RobotNum : Integer);
  VAR X, Y : Integer;
  BEGIN
    TextColor(RobotColor[RobotNum]);
    GotoXY(Width+2, (RobotNum-1)*5+1); Write(RobotNum, ' ', Robot[RobotNum].NAME);
    EraseSpot(Robot[RobotNum].Last_X, Robot[RobotNum].Last_Y);
    X := MapX(Robot[RobotNum].X); Y := MapY(Robot[RobotNum].Y);
    {If Cloak is Up, show as blinking - and white}
    IF Robot[RobotNum].Cloak = Up
    THEN BEGIN
      TextColor(RobotColor[RobotNum]+Blink); {Cloaked Robot's NAME Blinks!}
      GotoXY(Width+2, (RobotNum-1)*5+1); Write(RobotNum, ' ', Robot[RobotNum].NAME);
      Inc(CloakDisplayCount);
      IF CloakDisplayCount > 16 THEN CloakDisplayCount := 1;
      IF On_Field(X, Y) THEN
        BEGIN
          GotoXY(X, Y);
          CASE CloakDisplayCount OF
            1..4 : Write(Chr(RobotNum+48)); {Show with Normal Robot Color}
            9..12 : BEGIN         {Show with White Color}
                      TextColor(MColor);
                      Write(Chr(RobotNum+48));
                    END;
            5..8, 13..16 : Write(' ');
          END;                    {CASE}
        END                       {On_Field}
    END                           {Cloak is UP}
    ELSE IF On_Field(X, Y)
    THEN BEGIN
      GotoXY(X, Y);
      Write(Chr(RobotNum+48));
    END;
    Robot[RobotNum].Last_X := X;
    Robot[RobotNum].Last_Y := Y;
  END;

  FUNCTION Conflict(X, Y : Integer) : Boolean;
    {Checks to see if there is a robot at screen location X,Y}
  VAR Ans : Boolean;
    I : Integer;
  BEGIN
    Ans := False;
    FOR I := 1 TO NumRobots DO
      IF (Robot[I].Last_X = X) AND(Robot[I].Last_Y = Y) THEN Ans := True;
    Conflict := Ans;
  END;

  PROCEDURE DisplayFlyingMissile(X, Y : Integer);
  BEGIN
    X := MapX(X); Y := MapY(Y);
    IF On_Field(X, Y) AND(NOT Conflict(X, Y)) THEN
      BEGIN
        GotoXY(X, Y);
        Write(MissileSym);
      END;
  END;

 {$I ROBOUNIT.INC }

END.                              {RoboUnit}
