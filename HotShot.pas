PROCEDURE HotShot;
{
    Author: David Malmberg

    Strategy:  Stay in one place.  Find a foe.  Take a shot.
    Keep improving aim and shooting until foe is lost from sights.
    Then move sights (scanning) to adjacent target area.  If
    hit, then move to another random position on playing field.
    If the Robot scans two complete circles (720 degrees) without
    finding a foe in shooting range, move to another spot on the
    field.  (This will avoid "stand-offs" where opponents stay
    just out of range of one another.)

    This Robot should be VERY effective against foes which
    are stopped or are moving slowly.  It will be less effective
    against Robots traveling at high speeds.
}

    VAR { HotShot "Global" variables }
        Angle, { Scanning angle }
        Last_Damage, { Robot's Last damage value }
        Range, { Range/Distance to foe }
        Sweep, { "Sweep count" -- when = 36, Robot has scanned 720 degrees }
        Delta : Integer; { Scanning arc }

    PROCEDURE Aim(VAR Ang : Integer; VAR Arc : Integer);
    {
        Improve aim by doing a binary search of the target area,
        i.e., divide the target area in two equal pieces and redefine
        the target area to be the piece where the foe is found.
        If the foe is not found, expand the search area to the
        maximum arc of plus or minus 10 degrees.
    }
    BEGIN
        Arc := Arc DIV 2; { Divide search area in two. }
        IF scan(Ang-Arc, Arc) <> 0 { Check piece "below" target angle. }
        THEN Ang := Ang-Arc { If foe found, redefine target angle. }
        ELSE IF scan(Ang+Arc, Arc) <> 0 { Check piece "above" target angle.}
        THEN Ang := Ang+Arc { If foe found, redefine target angle. }
        ELSE Arc := 10;
        { Foe not found in either piece, expand search area to maximum arc. }
    END; {Aim}

    PROCEDURE BlastThem;
    BEGIN
        Angle := 10;
        REPEAT
            Delta := 10; { Start with widest scanning arc. }
            Range := scan(Angle, Delta);
            WHILE (Range > 40) AND (ObjectScanned = Enemy)
                AND (Range < MaxMissileRange) DO
                { Must be far enough away to avoid self-damage. }
            BEGIN
                Aim(Angle, Delta); { Improve aim. }
                Cannon(Angle, Range); { Fire!! }
                Range := scan(Angle, Delta); { Is foe still in sights? }
            END;
            Angle := Angle+20; { Look in adjacent target area. }
        UNTIL Angle > 360;
    END;

    PROCEDURE GOTO(x, y : Integer);
    { Go to location X,Y on playing field. }
    VAR
        Heading : Integer;
    BEGIN
        { Find the heading we need to get to the desired spot. }
        Heading := Angle_To(x, y);

        { Keep traveling at top speed until we are within 150 meters }
        WHILE (distance(loc_x, loc_y, x, y) > 150) DO
        BEGIN
            Drive(Heading, MaxSpeed);
            BlastThem;
        END;

        { Cut speed, and creep the rest of the way. }
        WHILE (distance(loc_x, loc_y, x, y) > 20) DO
        BEGIN
            Drive(Heading, 20);
            BlastThem;
        END;

        { Stop driving, should coast to a stop. }
        Drive(Heading, 0); {i.e., Stop}
    END; {GoTo(X,Y)}

    FUNCTION Hurt  : Boolean;
    { Checks if Robot has incurred any new damage. }
    VAR
        Curr_Damage : Integer;
        Answer: Boolean;
    BEGIN
        Curr_Damage := damage;
        Answer := (Curr_Damage > Last_Damage);
        Last_Damage := Curr_Damage;
        Hurt := Answer;
    END; {Hurt}

    PROCEDURE Move;
    { Move to a random spot on the playing field. }
    VAR
        x, y : Integer;
    BEGIN
        Sweep := 0; { Reset Sweep counter to zero. }
        x := Random(900)+50;
        y := Random(900)+50;
        GOTO(x, y);
    END; {Move}

BEGIN {HotShot Main}
    Angle := Angle_To(500, 500);
    { Start scanning for foes in center of field. }
    Sweep := 0; { Initialize Sweep counter to zero. }
    REPEAT { Until Dead or Winner }
        Delta := 10; { Start with widest scanning arc. }
        Range := scan(Angle, Delta);
        WHILE (Range > 40) AND (Range < 700) DO
        { Must be far enough away to avoid self-damage. }
        BEGIN
            Sweep := 0; { Found foe, so reset Sweep to zero }
            Aim(Angle, Delta); { Improve aim. }
            cannon(Angle, Range); { Fire!! }
            Range := scan(Angle, Delta); { Is foe still in sights? }
        END;
        Angle := Angle+20; { Look in adjacent target area. }
        Sweep := Sweep+1;
        IF Hurt OR (Sweep = 36) THEN Move;
        { If hit or have scanned two full circles, move elsewhere. }
    UNTIL Dead OR Winner;
END; (* HotShot Main *)
