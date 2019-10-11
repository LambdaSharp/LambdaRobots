# λ-Robots

In λ-Robots (pronounced _Lambda Robots_), you program a battle robot that participates on a square game field. Each turn, the server invokes your robot's Lambda function to get its action for the turn until either the robot wins or is destroyed.

λ-Robots is a port of the 90s [P-Robots](https://corewar.co.uk/probots.htm) game to AWS Serverless .NET using [LambdaSharp](https://lambdasharp.net).

## Step 1: Deploy LambdaRobots Server

**Option 1. Direct-Deploy**
```bash
lash deploy Challenge.LambdaRobots.Server@lambdasharp
```

**Option 2. Compile and Deploy**
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

        "arn:aws:lambda:us-west-2:254924790709:function:SteveBTest-Challenge-LambdaRobots-Yo-RobotFunction-75MJVEQWIWXG",
        "arn:aws:lambda:us-west-2:254924790709:function:SteveBTest-Challenge-LambdaRobots-Ta-RobotFunction-115OTP03MCCOM"
    ],
    "BoardWidth": 1000,
    "BoardHeight": 1000,
    "MaxTurns": 30,
    "RobotTimeoutSeconds": 30
}
```



## Robot SDK

### Properties

The most commonly needed properties are readily available as properties from the base class.

|Property Name      |Type       |Description |
|-------------------|-----------|------------|
|Damage             |double     |Robot damage. Value is always between 0 and `Robot.MaxDamage`. When the value is equal to `Robot.MaxDamage` the robot is considered killed. |
|GameBoardHeight    |double     |Height of the game board. |
|GameBoardWidth     |double     |Width of the game board. |
|GameId             |string     |Unique Game ID. |
|GameMaxTurns       |int        |Maximum number of turns before the game ends in a draw. |
|Heading            |double     |Robot heading. Value is always between `-180` and `180`. |
|Random             |Random     |Initialized random number generator. Instance of [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0). |
|ReloadCoolDown     |double     |Number of seconds until the missile launcher is ready again. |
|Robot              |Robot      |Robot data-structure (see below). |
|Speed              |double     |Robot speed. Value is between `0` and `Robot.MaxSpeed`. |
|X                  |double     |Horizontal position of robot. Value is between `0` and `GameBoardWidth`. |
|Y                  |double     |Vertical position of robot. Value is between `0` and `GameBoardHeight`. |

### Methods

#### `void SetHeading(double heading)`
#### `void SetSpeed(double speed)`
#### `Task<double?> ScanForEnemies(double heading, double resolution)`
#### `double AngleTo(double x, double y)`
#### `double DistanceTo(double x, double y)`

