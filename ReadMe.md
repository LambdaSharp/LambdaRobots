# LambdaRobots

Port of the old school C/Pascal Robots game to AWS serverless .NET

## Step 1: Deploy LambdaRobots Server

```bash
lash deploy src/Server
```

## Step 2: Deploy LambdaRobot

```bash
lash deploy src/Robot
```

## Step 3: Kick off game

```json
{
    "Action": "start",
    "RobotArns": [
        "arn:aws:lambda:us-west-2:254924790709:function:SteveBTest-Challenge-LambdaRobots-Ro-RobotFunction-1CVA9K57UVTMV",
        "arn:aws:lambda:us-west-2:254924790709:function:SteveBTest-Challenge-LambdaRobots-Ro-RobotFunction-1CVA9K57UVTMV"
    ],
    "BoardWidth": 1000,
    "BoardHeight": 1000,
    "MaxTurns": 30
}
```


```json
{
    "Id": "BDLMAcP9vHcCHww=",
    "State": "Finished",
    "TotalTurns": 1,
    "Missiles": [],
    "Robots": [
        {
            "Id": "BDLMAcP9vHcCHww=:R0",
            "Name": "Bob The Fighter",
            "State": "Alive",
            "X": 121.71907537696839,
            "Y": 640.6289438440599,
            "Speed": 0.0,
            "Heading": 0.0,
            "Damage": 0.0,
            "ReloadDelay": 0.0,
            "TargetSpeed": 0.0,
            "TargetHeading": 0.0,
            "TotalTravelDistance": 0.0,
            "TotalMissileFiredCount": 0,
            "TotalMissileHitCount": 0,
            "TotalScanCount": 0,
            "TotalKills": 0,
            "TotalDamageDealt": 0.0,
            "TimeOfDeath": null,
            "MaxSpeed": 100.0,
            "Acceleration": 10.0,
            "Deceleration": -20.0,
            "MaxTurnSpeed": 50.0,
            "ScannerRange": 600.0,
            "ScannerResolution": 10.0,
            "MaxDamage": 100.0,
            "CollisionDamage": 2.0,
            "DirectHitDamage": 8.0,
            "NearHitDamage": 4.0,
            "FarHitDamage": 2.0,
            "MissileReloadDelay": 2.0,
            "MissileSpeed": 50.0,
            "MissileRange": 700.0,
            "MissileDirectHitDamageBonus": 3.0,
            "MissileNearHitDamageBonus": 2.1,
            "MissileFarHitDamageBonus": 1.0
        }
    ],
    "Messages": [
        {
            "Timestamp": "2019-10-04T18:25:53.414849Z",
            "Text": "Bob The Fighter is victorious! Game Over."
        }
    ],
    "BoardWidth": 1000.0,
    "BoardHeight": 1000.0,
    "SecondsPerTurn": 1.0,
    "DirectHitRange": 5.0,
    "NearHitRange": 20.0,
    "FarHitRange": 40.0,
    "CollisionRange": 2.0,
    "MinRobotStartDistance": 100.0,
    "RobotTimeoutSeconds": 10.0,
    "MaxTurns": 300
}
```

## Robot SDK

### Properties

|Property Name      |Type       |Description |
|-------------------|-----------|------------|
|Random             |Random     |Initialized random number generate. See [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0). |
|X                  |double     |Horizontal position of robot. Value is between `0` and `GameBoardWidth`. |
|Y                  |double     |Vertical position of robot. Value is between `0` and `GameBoardHeight`. |
|Speed              |double     |Robot speed. Value is between `0` and `Robot.MaxSpeed`. |
|Heading            |double     |Robot heading. Value is always between `-180` and `180`. |
|Damage             |double     |Robot damage. Value is always between 0 and `Robot.MaxDamage`. When the value is equal to `Robot.MaxDamage` the robot is considered killed. |
|ReloadCoolDown     |double     |Number of seconds until the missile launcher is ready again. |
|GameId             |string     |Unique Game ID. |
|GameBoardWidth     |double     |Maximum width of the game board. |
|GameBoardHeight    |double     |Maximum height of the game board. |
|GameMaxTurns       |int        |Maximum number of turns before the game ends in a draw. |
|Robot              |Robot      |Robot data-structure (see below). |

### Methods

#### `void SetHeading(double heading)`

#### `void SetSpeed(double speed)`

#### `Task<double?> Scan(double heading, double resolution)`
