  PROCEDURE KillThemOff;
  VAR I, NumberLeft : Integer;
  BEGIN
    FOR I := 1 TO NumRobots DO
      IF ((Robot[I].Damage >= 100) AND(NOT Dead[I])) THEN
        BEGIN
          Dead[I] := True;
          LosingGame[I] := LosingGame[I]+OCNT; {CPU cycles for loss}
          PTAB[I].ACTIVE := False; {this process will no longer be executed}
          Robot[I].Damage := 100;
          IF NOT MatchPlay THEN
            BEGIN
              EraseSpot(Robot[I].Last_X, Robot[I].Last_Y);
              UpdateDamage(I);
            END;
          Robot[I].Last_X := -1;  {off the field}
        END;
    NumberLeft := 0;
    FOR I := 1 TO NumRobots DO
      IF NOT Dead[I] THEN NumberLeft := NumberLeft+1;
    IF NumberLeft <= 1 THEN
      BEGIN
        Winner := True;
        PFLAG := False;
        PTAB[0].ACTIVE := True;   {i.e., execute "Main" process}
      END;
    IF NumberLeft = 0 THEN EndItNow := True;
  END;

  PROCEDURE MoveMissiles;
  VAR
    CollisionNum, CollisionX, CollisionY, CollisionDist,
    I, n, R, DX, dy, Dist, DeltaMeters : Integer;
    DummyBoolean, InsideObstacle : Boolean;
    NewX, NewY, NewDist : Integer;
  BEGIN
    KillThemOff;                  {make sure any robots that should be dead -- are dead}

    FOR R := 1 TO NumRobots DO
      BEGIN
        FOR I := 1 TO Missile_Per_Robot DO

          IF Missile[R, I].Status = Flying THEN
            BEGIN                 {It is flying -- so move it}

              {update current position}
              DeltaMeters := Missile_Speed DIV Speed_Adjustment; {Normal flight distance}
              IF ((Missile[R, I].Curr_Dist+DeltaMeters) > Missile[R, I].Range)
              THEN DeltaMeters := Missile[R, I].Range-Missile[R, I].Curr_Dist;
              {limit distance traveled to missile's range - if appropriate}

              {Now check for possible collision with Obstacles - if any}
              IF GameHasObstacles
              THEN BEGIN
                NewDist := Missile[R, I].Curr_Dist+DeltaMeters;
                IF NewDist > Missile[R, I].Range
                THEN NewDist := Missile[R, I].Range;

                NewX := Missile[R, I].Beg_X+Trunc(NewDist*Cos(AngToRad*Missile[R, I].Heading));
                NewY := Missile[R, I].Beg_Y+Trunc(NewDist*Sin(AngToRad*Missile[R, I].Heading));

                CollisionX := 0;
                CollisionY := 0;
                CollisionDist := NoGood;
                DummyBoolean := Hits_Obstruction(Robot[R].Beg_X, Robot[R].Beg_Y, NewX, NewY,
                                                 CollisionX, CollisionY, CollisionDist);
                IF CollisionDist <= DeltaMeters
                THEN BEGIN
                  DeltaMeters := CollisionDist;
                  Missile[R, I].Status := Exploding; {Collision causes explosion}
                END;              {CollisionDist <= DeltaMeters}
              END;                {GameHasObstacles}

              Missile[R, I].Curr_Dist := Missile[R, I].Curr_Dist+DeltaMeters;
              IF Missile[R, I].Curr_Dist > Missile[R, I].Range
              THEN Missile[R, I].Curr_Dist := Missile[R, I].Range;

              Missile[R, I].Cur_X := Missile[R, I].Beg_X+Trunc(Missile[R, I].Curr_Dist*Cos(AngToRad*Missile[R, I].Heading));
              Missile[R, I].Cur_Y := Missile[R, I].Beg_Y+Trunc(Missile[R, I].Curr_Dist*Sin(AngToRad*Missile[R, I].Heading));

              IF GameHasObstacles
              THEN BEGIN
                MoveItToEdge(Missile[R, I].Cur_X, Missile[R, I].Cur_Y, InsideObstacle);
                IF InsideObstacle THEN Missile[R, I].Status := Exploding;
              END;

              {check for collisions with boundaries}

              IF (Missile[R, I].Cur_X < 0) THEN
                BEGIN             {collision with left wall}
                  Missile[R, I].Cur_X := 0;
                  Missile[R, I].Status := Exploding;
              END ELSE
                IF (Missile[R, I].Cur_X > 999) THEN
                  BEGIN           {collision with right wall}
                    Missile[R, I].Cur_X := 999;
                    Missile[R, I].Status := Exploding;
                  END;

              IF (Missile[R, I].Cur_Y < 0) THEN
                BEGIN             {collision with bottom wall}
                  Missile[R, I].Cur_Y := 0;
                  Missile[R, I].Status := Exploding;
              END ELSE
                IF (Missile[R, I].Cur_Y > 999) THEN
                  BEGIN           {collision with top wall}
                    Missile[R, I].Cur_Y := 999;
                    Missile[R, I].Status := Exploding;
                  END;

              IF Missile[R, I].Curr_Dist = Missile[R, I].Range
              THEN Missile[R, I].Status := Exploding;

              IF Missile[R, I].Status = Exploding THEN
                {inflict damage on nearby robots -- if any}
                FOR n := 1 TO NumRobots DO
                  BEGIN
                    Dist := Distance(Robot[n].X, Robot[n].Y, Missile[R, I].Cur_X, Missile[R, I].Cur_Y);
                    IF (Robot[n].Shield = Down)
                    THEN BEGIN
                      IF (Dist <= Direct_Hit_Range)
                      THEN Robot[n].Damage := Robot[n].Damage+Robot[n].Direct_Hit_Damage+Robot[R].Direct_Hit_Damage_Bonus
                      ELSE IF (Dist <= Near_Hit_Range)
                      THEN Robot[n].Damage := Robot[n].Damage+Robot[n].Near_Hit_Damage+Robot[R].Near_Hit_Damage_Bonus
                      ELSE IF (Dist <= Far_Hit_Range)
                      THEN Robot[n].Damage := Robot[n].Damage+Robot[n].Far_Hit_Damage+Robot[R].Far_Hit_Damage_Bonus;
                      IF Dist <= Far_Hit_Range THEN {some damage done -- so update}
                        BEGIN
                          IF Robot[n].Damage > 100 THEN Robot[n].Damage := 100;
                          IF NOT MatchPlay THEN UpdateDamage(n);
                        END;
                    END;          {Shield is Down}
                    IF (Robot[n].Shield = Up)
                    THEN BEGIN
                      IF (Dist <= Direct_Hit_Range)
                      THEN DamagedFuel[n] := DamagedFuel[n]+Robot[n].Direct_Hit_Damage+Robot[R].Direct_Hit_Damage_Bonus
                      ELSE IF (Dist <= Near_Hit_Range)
                      THEN DamagedFuel[n] := DamagedFuel[n]+Robot[n].Near_Hit_Damage+Robot[R].Near_Hit_Damage_Bonus
                      ELSE IF (Dist <= Far_Hit_Range)
                      THEN DamagedFuel[n] := DamagedFuel[n]+Robot[n].Far_Hit_Damage+Robot[R].Far_Hit_Damage_Bonus;
                    END;          {Shield is Up}
                  END;            {for Robot n}
            END;                  {for Flying Missile i}

        {plot each missile}
        FOR I := 1 TO Missile_Per_Robot DO
          CASE Missile[R, I].Status OF
            Flying : BEGIN
                       IF NOT MatchPlay THEN
                         BEGIN
                           EraseSpot(Missile[R, I].Last_X, Missile[R, I].Last_Y);
                           TextColor(RobotColor[R]);
                           DisplayFlyingMissile(Missile[R, I].Cur_X, Missile[R, I].Cur_Y);
                         END;
                       Missile[R, I].Last_X := MapX(Missile[R, I].Cur_X);
                       Missile[R, I].Last_Y := MapY(Missile[R, I].Cur_Y);
                     END;
            Exploding : BEGIN
                          IF (Missile[R, I].Count = Explosion_Count) AND(NOT MatchPlay) THEN
                            BEGIN {show explosion}
                              EraseSpot(Missile[R, I].Last_X, Missile[R, I].Last_Y);
                              TextColor(RobotColor[R]);
                              Explosion(Missile[R, I].Cur_X, Missile[R, I].Cur_Y, ShowIt);
                            END;
                          Missile[R, I].Count := Missile[R, I].Count-1;
                          IF Missile[R, I].Count = 0 THEN
                            BEGIN {erase explosion}
                              IF NOT MatchPlay
                              THEN Explosion(Missile[R, I].Cur_X, Missile[R, I].Cur_Y, HideIt);
                              Missile[R, I].Status := Available;
                              Missile[R, I].Last_X := -1;
                              Missile[R, I].Last_Y := -1;
                              Missile[R, I].Count := Explosion_Count;
                            END;
                        END;
          END;                    {Case}

      END;                        {for Robot r}
    KillThemOff;
  END;                            {MoveMissiles}


  PROCEDURE MoveRobots;
  VAR
    I, n, DeltaMeters,
    CollisionX, CollisionY, CollisionDist, CollisionNum : Integer;
    NewX, NewY : LongInt;
    CyclesThisMove : LongInt;
    DummyBoolean, InsideObstacle : Boolean;
  BEGIN
    CurrentCycles := OCNT;
    CyclesThisMove := CurrentCycles-LastCycles;
    FOR I := 1 TO NumRobots DO
      IF NOT Dead[I] THEN
        BEGIN
          DisplayBomb(I);         {If Robot has a bomb, show it!}
          {It is alive so move it}
          {display at old position first}
          IF NOT MatchPlay THEN DisplayRobot(I);

          {update cannon loader count}
          IF Robot[I].Reload > 0 THEN Robot[I].Reload := Robot[I].Reload-1;

          {update speed}
          IF Robot[I].Speed <> Robot[I].Desired_Speed THEN
            BEGIN                 {Need to change speed}
              IF Robot[I].Speed > Robot[I].Desired_Speed THEN {Slowing}
                BEGIN
                  Robot[I].Speed := Robot[I].Speed-Acceleration;
                  IF Robot[I].Speed < Robot[I].Desired_Speed
                  THEN Robot[I].Speed := Robot[I].Desired_Speed;
                END
              ELSE IF Robot[I].Fuel > 0 THEN
                BEGIN             {OK to speed up}
                  Robot[I].Speed := Robot[I].Speed+Acceleration;
                  IF Robot[I].Speed > Robot[I].Desired_Speed
                  THEN Robot[I].Speed := Robot[I].Desired_Speed;
                END
              ELSE Robot[I].Desired_Speed := 0; {No fuel -- so stop}
            END;                  {Need to change speed}

          {update heading}
          IF Robot[I].Heading <> Robot[I].Desired_Heading THEN
            {Need to change heading -- if speed is slow enough to turn}
            IF Robot[I].Speed <= Max_Turn_Speed THEN {OK to turn}
              BEGIN
                Robot[I].Heading := Robot[I].Desired_Heading;
                Robot[I].Range := 0;
                Robot[I].Beg_X := Robot[I].X;
                Robot[I].Beg_Y := Robot[I].Y;
              END
            ELSE                  {Going too fast to turn -- so slow down to zero}
              Robot[I].Desired_Speed := 0;

          {update distance traveled on this heading}
          IF Robot[I].Speed > 0 THEN
            BEGIN                 {need to move}
              DeltaMeters := Robot[I].Speed DIV Speed_Adjustment;
              IF DeltaMeters < 1 THEN DeltaMeters := 1;
              {must move at least 1 meter -- if moving at all}

              Robot[I].Range := Robot[I].Range+DeltaMeters;
              NewX := Robot[I].Beg_X+Trunc(Robot[I].Range*Cos(AngToRad*Robot[I].Heading));
              NewY := Robot[I].Beg_Y+Trunc(Robot[I].Range*Sin(AngToRad*Robot[I].Heading));

              IF GameHasObstacles
              THEN BEGIN
                CollisionX := 0;
                CollisionY := 0;
                CollisionDist := NoGood;
                DummyBoolean := Hits_Obstruction(Robot[I].X, Robot[I].Y, NewX, NewY,
                                                 CollisionX, CollisionY, CollisionDist);

                IF CollisionDist < DeltaMeters
                THEN BEGIN
                  DeltaMeters := CollisionDist;
                  MoveItToEdge(NewX, NewY, InsideObstacle);
                  NewX := CollisionX;
                  NewY := CollisionY;
                  Robot[I].Range := 0;
                  Robot[I].Beg_X := NewX;
                  Robot[I].Beg_Y := NewY;
                  Robot[I].Speed := 0; {Collision causes stop}
                  Robot[I].Desired_Speed := 0;
                  IF Robot[I].Shield = Down
                  THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                  ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
                END;              {CollisionDist <= DeltaMeters}
              END;                {GameHasObstacles}

              Robot[I].X := NewX;
              Robot[I].Y := NewY;
              Meters[I] := Meters[I]+DeltaMeters;

              {check for collisions with other Robots and playing field walls}
              FOR n := 1 TO NumRobots DO
                IF ((n <> I) AND(NOT Dead[n])) THEN
                  IF (Abs(Robot[I].X-Robot[n].X) < 2) AND
                  (Abs(Robot[I].Y-Robot[n].Y) < 2) THEN
                    BEGIN         {collision with another robot}
                      Robot[I].Speed := 0;
                      Robot[I].Desired_Speed := 0;
                      IF Robot[I].Shield = Down
                      THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                      ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
                      {both involved in collision}
                      Robot[n].Speed := 0;
                      Robot[n].Desired_Speed := 0;
                      IF Robot[n].Shield = Down
                      THEN Robot[n].Damage := Robot[n].Damage+Robot[n].Collision_Damage
                      ELSE DamagedFuel[n] := DamagedFuel[n]+Robot[n].Collision_Damage;
                    END;

              IF (Robot[I].X < 0) THEN
                BEGIN             {collision with left wall}
                  Robot[I].X := 0;
                  Robot[I].Speed := 0;
                  Robot[I].Desired_Speed := 0;
                  IF Robot[I].Shield = Down
                  THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                  ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
              END ELSE
                IF (Robot[I].X > 999) THEN
                  BEGIN           {collision with right wall}
                    Robot[I].X := 999;
                    Robot[I].Speed := 0;
                    Robot[I].Desired_Speed := 0;
                    IF Robot[I].Shield = Down
                    THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                    ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
                  END;

              IF (Robot[I].Y < 0) THEN
                BEGIN             {collision with bottom wall}
                  Robot[I].Y := 0;
                  Robot[I].Speed := 0;
                  Robot[I].Desired_Speed := 0;
                  IF Robot[I].Shield = Down
                  THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                  ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
              END ELSE
                IF (Robot[I].Y > 999) THEN
                  BEGIN           {collision with top wall}
                    Robot[I].Y := 999;
                    Robot[I].Speed := 0;
                    Robot[I].Desired_Speed := 0;
                    IF Robot[I].Shield = Down
                    THEN Robot[I].Damage := Robot[I].Damage+Robot[I].Collision_Damage
                    ELSE DamagedFuel[I] := DamagedFuel[I]+Robot[I].Collision_Damage;
                  END;

              IF Robot[I].Damage > 100 THEN Robot[I].Damage := 100;

              IF NOT MatchPlay THEN
                BEGIN
                  UpdateXLoc(I);
                  UpdateYLoc(I);
                  UpdateHeading(I);
                  UpdateDamage(I);
                  DisplayRobot(I);
                END;
            END;                  {need to move}

          IF NOT MatchPlay THEN UpdateSpeed(I); {Display speed - even for Robots that are not moving}

          {update damage -- if repairing}
          IF Robot[I].MakingRepairs AND(Robot[I].Speed = 0)
          THEN BEGIN
            Cycles_Repairing[I] := Cycles_Repairing[I]+CyclesThisMove;
            CurrentRepairPoints[I] := Cycles_Repairing[I]DIV Cycles_Per_Jigger;
            Robot[I].Damage := Robot[I].Damage-CurrentRepairPoints[I]+LastRepairPoints[I];
            LastRepairPoints[I] := CurrentRepairPoints[I];
            IF Robot[I].Damage < 0
            THEN BEGIN
              Robot[I].MakingRepairs := False;
              Robot[I].Damage := 0;
            END;
          END;

          {update fuel -- on a cummulative basis}
          IF Robot[I].Shield = Up
          THEN Cycles_Shield_Up[I] := Cycles_Shield_Up[I]+CyclesThisMove;
          IF Robot[I].Cloak = Up
          THEN Cycles_Cloak_Up[I] := Cycles_Cloak_Up[I]+CyclesThisMove;
          Robot[I].Fuel := StartingFuel[I]-(Meters[I]DIV Meters_Per_Jigger)*Robot[I].EngineJiggers
                           -Jiggers_Per_Missile*Missiles_Fired[I]
                           -(Cycles_Shield_Up[I]DIV Cycles_Per_Jigger)*Robot[I].ShieldJiggers
                           -(Cycles_Cloak_Up[I]DIV Cycles_Per_Jigger)*Robot[I].CloakJiggers
                           -(Cycles_Repairing[I]DIV Cycles_Per_Jigger)*Robot[I].RepairJiggers
                           -(DamagedFuel[I]*Fuel_Per_Damage);
          IF UnLimited_Fuel THEN Robot[I].Fuel := 999;
          IF Robot[I].Fuel < 1 THEN
            BEGIN
              Robot[I].Fuel := 0;
              Robot[I].Desired_Speed := 0; (* Coast to Stop *)
              Robot[I].Shield := Down; (* No more fuel to power Shield *)
              Robot[I].Cloak := Down; (* No more fuel to power Cloak *)
            END;
          IF NOT MatchPlay THEN UpdateFuel(I); (* Display new fuel level *)

        END;                      {for live Robot I}
    LastCycles := CurrentCycles;
  END;


  PROCEDURE DrawFrame;
  VAR I, J : Integer;
  BEGIN
    {Top line of playing field}
    TextColor(MColor);
    GotoXY(1, 1); Write(UL);
    FOR I := 2 TO Width-1 DO
      BEGIN
        GotoXY(I, 1);
        Write(HL);
      END;
    Write(UR);
    {Middle edges of field}
    FOR I := 2 TO Height-1 DO
      BEGIN
        GotoXY(1, I);
        Write(VL);
        GotoXY(Width, I);
        Write(VL);
      END;
    {Bottom row}
    GotoXY(1, Height); Write(LL);
    FOR I := 2 TO Width-1 DO
      BEGIN
        GotoXY(I, Height);
        Write(HL);
      END;
    Write(LR);
    {Now - show Robot status windows}
    FOR J := 1 TO NumRobots DO
      BEGIN
        TextColor(RobotColor[J]);
        GotoXY(Width+2, (J-1)*5+1); Write(J, ' ', Robot[J].NAME);
        IF NOT UnLimited_Fuel THEN
          BEGIN
            GotoXY(Width+13, (J-1)*5+1);
            Write('F');
          END;
        GotoXY(Width+2, (J-1)*5+2); Write('  D%       Sc');
        GotoXY(Width+2, (J-1)*5+3); Write('  Sp       Hd');
        GotoXY(Width+2, (J-1)*5+4); Write('  X=       Y=');
        GotoXY(Width+1, (J-1)*5+5); FOR I := Width+1 TO 79 DO Write(HL);
      END;
    TextColor(MColor);
    GotoXY(Width+2, Height-2); Write('CPU');
    GotoXY(Width+2, Height-1); Write('Cycle:');
  END;

  PROCEDURE GenerateObstacles;
  VAR I, X, Y, StartX, FinalX, StartY, FinalY : Integer;
  BEGIN
    IF NumberOfObstacles > 0 THEN
      BEGIN
        TextColor(MColor);
        FOR I := 1 TO NumberOfObstacles DO
          BEGIN
            StartX := 4+Random(Width-12); {Screen Left column of Obstacle}
            FinalX := StartX+2+Random(6); {Screen Right column of Obstacle}
            StartY := 4+Random(Height-9); {Screen Top row of Obstacle}
            FinalY := StartY+2+Random(2); {Screen Bottom row of Obstacle}
            IF NOT MatchPlay THEN
              FOR X := StartX TO FinalX DO
                FOR Y := StartY TO FinalY DO
                  BEGIN
                    GotoXY(X, Y);
                    Write(ObstacleSym); {Draw Obstacles on playing field}
                  END;
            Obstacle[I].ScreenLeft := StartX; {Set Screen Obstacle values}
            Obstacle[I].ScreenRight := FinalX;
            Obstacle[I].ScreenTop := FinalY;
            Obstacle[I].ScreenBottom := StartY;
            X := MapScreenX(StartX);
            REPEAT
              X := X-1;
            UNTIL (MapX(X) = (StartX-1));
            Obstacle[I].Left := X-3;
            X := MapScreenX(FinalX);
            REPEAT
              X := X+1;
            UNTIL (MapX(X) = (FinalX+1));
            Obstacle[I].Right := X+3;
            Y := MapScreenY(StartY);
            REPEAT
              Y := Y+1;
            UNTIL (MapY(Y) = (StartY-1));
            Obstacle[I].Top := Y+3;
            Y := MapScreenY(FinalY);
            REPEAT
              Y := Y-1;
            UNTIL (MapY(Y) = (FinalY+1));
            Obstacle[I].Bottom := Y-3;
          END;
      END;                        {IF NumberOfObstacles > 0}
  END;


  PROCEDURE Initialize_Robots;
  VAR I, J, X, Y : Integer;
  BEGIN
    IQOption := 5;                {Quite smart}
    {Initialize speed parameters}
    Cycles_Per_Motion := 5*IQOption;
    Speed_Adjustment := 30;
    Acceleration := 5;
    Reload_Cycles := 10;

    LastCycles := 0;
    EndItNow := False;
    IF NumRobots = 1 THEN
      BEGIN
        NumRobots := 2;
        FileName[2] := 'TARGET';
      END;
    MovementCounter := Cycles_Per_Motion;
    ShowIt := '\^/<<'+MissileSym+'>>/v\'; {Missile Explosion Characters}
    HideIt := '           ';      {Hide Explosion}
    IF Match = 1 THEN
      FOR I := 1 TO NumRobots DO
        BEGIN
          NumberOfWins[I] := 0;
          WinningDamage[I] := 0;
          AverageDamage[I] := 0;
          AverageGame[I] := 0;
          LongestGame[I] := 0;
          LosingGame[I] := 0;
          ShortestGame[I] := 400000;
        END;
    FOR I := 1 TO NumRobots DO
      BEGIN
        Robot[I].NAME := FileName[I];
        Init_Robot(I);
      END;

    FOR I := 1 TO NumRobots DO
      BEGIN
        IF FriendlyName[I] <> Robot[I].NAME THEN
          FOR J := 1 TO NumRobots DO
            IF FriendlyName[I] = Robot[J].NAME THEN
              BEGIN
                Robot[I].AllyNumber := J;
                Robot[I].TeamMember := True;
              END;
        Dead[I] := False;
      END;
    Winner := False;
    Ticks := 0;
    IF NOT MatchPlay THEN
      BEGIN
        ClrScr;
        DrawFrame;
      END;
    GenerateObstacles;            {If any}
    FOR I := 1 TO NumRobots DO
      BEGIN
        REPEAT
          X := 2+Random(Width-4); {Position robot on field/screen}
          Y := 2+Random(Height-4);
        UNTIL (On_Field(X, Y) AND(NOT Conflict(X, Y)));
        {Avoid conflict with obstacles (if any) and other robots}
        Robot[I].X := MapScreenX(X); (* 0 to 999 *)
        Robot[I].Y := MapScreenY(Y);
        Robot[I].Beg_X := Robot[I].X;
        Robot[I].Beg_Y := Robot[I].Y;
        Robot[I].Last_X := X;     (* screen coordinates *)
        Robot[I].Last_Y := Y;

        IF NOT MatchPlay
        THEN BEGIN
          DisplayRobot(I);
          UpdateXLoc(I);
          UpdateYLoc(I);
          UpdateHeading(I);
          UpdateDamage(I);
          UpdateSpeed(I);
          UpdateScanDir(I);
        END;
      END;
    IF MatchPlay THEN
      BEGIN
        WriteLn;
        WriteLn('MATCH NUMBER ', Match);
        ScreenLine := WhereY;
        IF ScreenLine = Height THEN
          BEGIN
            WriteLn;
            WriteLn;
            ScreenLine := Height-2;
          END;
      END;
  END;

  PROCEDURE Update_Robots;
  VAR I, NumberAlive, LastAlive : Integer;
    SomeOnesAlive : Boolean;
  BEGIN
    Ticks := Ticks+1;
    Time := Ticks MOD MaxInt;
    IF NOT MatchPlay THEN
      BEGIN
        TextColor(MColor);
        GotoXY(Width+9, Height-1); Write('          ');
        GotoXY(Width+9, Height-1); Write(OCNT:9);
      END;
    MovementCounter := MovementCounter-1;
    IF MovementCounter = 0 THEN
      BEGIN
        IF UnLimited_Fuel THEN
          FOR I := 1 TO NumRobots DO Robot[I].Fuel := 999;
        MovementCounter := Cycles_Per_Motion;
        KillThemOff;
        MoveRobots;
        MoveMissiles;
        IF MatchPlay THEN
          BEGIN
            GotoXY(1, ScreenLine);
            ClrEol; Write('DAMAGE:  ');
            FOR I := 1 TO NumRobots DO Write(Robot[I].NAME, ' ', Robot[I].Damage:3, '   ');
            WriteLn; ClrEol;
            Write('CPU Cycles:', OCNT:7);
          END;
      END;
    SomeOnesAlive := False;
    NumberAlive := 0;
    FOR I := 1 TO NumRobots DO
      IF (Robot[I].Damage < 100) THEN
        BEGIN
          NumberAlive := NumberAlive+1;
          LastAlive := I;
        END;
    IF (NumberAlive = 2) AND(Robot[LastAlive].AllyNumber <> LastAlive)
    AND(Robot[Robot[LastAlive].AllyNumber].Damage < 100)
    THEN SomeOnesAlive := False   {Only two team members are alive}
    ELSE FOR I := 1 TO NumRobots DO
      IF ((Robot[I].Damage < 100) AND(Robot[I].Fuel > 0))
      THEN SomeOnesAlive := True;
    IF (OCNT >= MaxCycles) OR(NOT SomeOnesAlive) THEN {Let's end contest now!}
      BEGIN
        EndItNow := True;
        PFLAG := False;
        PTAB[0].ACTIVE := True;   {execute "main" program}
      END;
  END;

  PROCEDURE Finalize_Robots;
  VAR WinningRobot, I, J, k, LeastDamage, NumberOfLoses, PercentWins : Integer;
    StartRow, EndRow, StartCol, EndCol : Integer;
  BEGIN
    StartRow := 10;
    EndRow := 16;
    StartCol := 10;
    EndCol := 44;
    WinningRobot := 0;
    LeastDamage := 100;
    FOR I := 1 TO NumRobots DO
      IF (Robot[I].Damage <= LeastDamage) THEN
        BEGIN
          WinningRobot := I;      (* Robot with least damage wins -- there are no ties *)
          LeastDamage := Robot[I].Damage;
        END;
    k := Robot[WinningRobot].AllyNumber;
    IF k <> WinningRobot THEN EndRow := EndRow+2;
    IF NOT MatchPlay THEN
      BEGIN
        {Top line}
        TextColor(MColor);
        GotoXY(StartCol, StartRow); Write(UL);
        FOR I := StartCol+1 TO EndCol-1 DO
          BEGIN
            GotoXY(I, StartRow);
            Write(HL);
          END;
        Write(UR);
        {Middle edges}
        FOR J := StartRow+1 TO EndRow-1 DO
          BEGIN
            GotoXY(StartCol, J);
            Write(VL);
            FOR I := StartCol+1 TO EndCol-1 DO
              BEGIN
                GotoXY(I, J);
                Write(' ');
              END;
            GotoXY(EndCol, J);
            Write(VL);
          END;
        {Bottom row}
        GotoXY(StartCol, EndRow); Write(LL);
        FOR I := StartCol+1 TO EndCol-1 DO
          BEGIN
            GotoXY(I, EndRow);
            Write(HL);
          END;
        Write(LR);
        TextColor(RobotColor[WinningRobot]);
        GotoXY(StartRow+2, StartCol+2); Write('The winner is ', Robot[WinningRobot].NAME);
        GotoXY(StartRow+2, StartCol+4);
        IF k <> WinningRobot
        THEN WriteLn(' -- part of team with ', FriendlyName[WinningRobot])
        ELSE WriteLn;
        TextColor(RobotColor[WinningRobot]+Blink);
        GotoXY(StartCol+2, EndRow-2); Write('Another Contest (Y/N) ? ');
        AnotherContest := ReadKey; AnotherContest := Upcase(AnotherContest);
      END
    ELSE BEGIN                    {match play}
      NumberOfWins[WinningRobot] := NumberOfWins[WinningRobot]+1;
      WinningDamage[WinningRobot] := WinningDamage[WinningRobot]
                                     +Robot[WinningRobot].Damage;
      FOR J := 1 TO NumRobots
               DO AverageDamage[J] := AverageDamage[J]+Robot[J].Damage;
      IF OCNT > LongestGame[WinningRobot]
      THEN LongestGame[WinningRobot] := OCNT;
      IF OCNT < ShortestGame[WinningRobot]
      THEN ShortestGame[WinningRobot] := OCNT;
      AverageGame[WinningRobot] := AverageGame[WinningRobot]+OCNT;
      Write('   The winner is ', Robot[WinningRobot].NAME);
      I := Robot[WinningRobot].AllyNumber;
      IF I <> WinningRobot
      THEN WriteLn(' -- part of team with ', FriendlyName[WinningRobot])
      ELSE WriteLn;
      Write('Cumulative wins:  ');
      FOR I := 1 TO NumRobots DO Write(Robot[I].NAME, ' ', NumberOfWins[I], '   ');
      WriteLn;
      IF DoingMatchStats THEN
        BEGIN
          WriteLn(MATCHSTATS, 'Match ', Match);
          WriteLn(MATCHSTATS, 'Winner Number ', WinningRobot, ' ', Robot[WinningRobot].NAME);
          WriteLn(MATCHSTATS, 'Winning Damage ', Robot[WinningRobot].Damage);
          WriteLn(MATCHSTATS, 'Length of Game ', OCNT, ' CPU cycles');
          WriteLn(MATCHSTATS);
        END;
      IF Match = MatchNumber THEN {Print summary statistics}
        FOR J := 1 TO NumRobots DO
          BEGIN
            NumberOfLoses := MatchNumber-NumberOfWins[J];
            IF NumberOfLoses > 0
            THEN LosingGame[J] := LosingGame[J]DIV NumberOfLoses
            ELSE LosingGame[J] := 0;
            IF NumberOfWins[J] > 0
            THEN BEGIN
              WinningDamage[J] := WinningDamage[J]DIV NumberOfWins[J];
              AverageGame[J] := AverageGame[J]DIV NumberOfWins[J];
            END;
            AverageDamage[J] := AverageDamage[J]DIV MatchNumber;
            PercentWins := (100*NumberOfWins[J]) DIV MatchNumber;
            WriteLn;
            Write('Statistics for number ', J, ' ', Robot[J].NAME);
            IF NumberOfWins[J] > 0
            THEN BEGIN
              WriteLn(' -- ', NumberOfWins[J], ' total wins -- ', PercentWins, '%');
              WriteLn('Average Winning Damage ', WinningDamage[J],
                      ' -- Average Overall Damage ', AverageDamage[J]);
              Write('Winning Game CPU cycles:  Longest: ', LongestGame[J]);
              Write('  Shortest: ', ShortestGame[J]);
              WriteLn('  Average: ', AverageGame[J]);
            END ELSE WriteLn(' -- No wins!!');
            WriteLn('Average CPU cycles to lose game ', LosingGame[J]);
            IF DoingMatchStats THEN
              BEGIN
                WriteLn(MATCHSTATS);
                Write(MATCHSTATS, 'Statistics for number ', J, ' ', Robot[J].NAME);
                IF NumberOfWins[J] > 0
                THEN BEGIN
                  WriteLn(MATCHSTATS, ' -- ', NumberOfWins[J], ' total wins -- ', PercentWins, '%');
                  WriteLn(MATCHSTATS, 'Average Winning Damage ', WinningDamage[J],
                          ' -- Average Overall Damage ', AverageDamage[J]);
                  Write(MATCHSTATS, 'Winning Game CPU cycles:  Longest: ', LongestGame[J]);
                  Write(MATCHSTATS, '  Shortest: ', ShortestGame[J]);
                  WriteLn(MATCHSTATS, '  Average: ', AverageGame[J]);
                END ELSE WriteLn(MATCHSTATS, ' -- No wins!!');
                WriteLn(MATCHSTATS, 'Average CPU cycles to lose game ', LosingGame[J]);
              END;
          END;
    END;
  END;                            {Finalize_Robots}
