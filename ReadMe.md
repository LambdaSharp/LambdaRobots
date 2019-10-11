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

## Configuration

8 points allocated by default as follows.

### Radar

|Radar Type      |Radar Range |Radar Resolution|Cost|
|----------------|------------|----------------|----|
|UltraShortRange |200 meters  |45 degrees      |0   |
|ShortRange      |400 meters  |20 degrees      |1   |
|**MidRange (*)**|600 meters  |10 degrees      |2   |
|LongRange       |800 meters  |8 degrees       |3   |
|UltraLongRange  |1,000 meters|5 degrees       |4   |

### Engine

|Engine Type     |Max. Speed |Acceleration|Cost|
|----------------|-----------|------------|----|
|Economy         |60 m/s     |7 m/s^2     |0   |
|Compact         |80 m/s     |8 m/s^2     |1   |
|**Standard (*)**|100 m/s    |10 m/s^2    |2   |
|Large           |120 m/s    |12 m/s^2    |3   |
|ExtraLarge      |140 m/s    |13 m/s^2    |4   |

### Armor

|Armor Type    |Direct Hit|Near Hit|Far Hit|Collision|Max. Speed |Deceleration  |Cost|
|--------------|----------|--------|-------|---------|-----------|--------------|----|
|UltraLight    |50        |25      |12     |10       |+35 m/s    |30 m/s^2      |0   |
|Light         |16        |8       |4      |3        |+25 m/s    |25 m/s^2      |1   |
|**Medium (*)**|8         |4       |2      |2        |-          |20 m/s^2      |2   |
|Heavy         |4         |2       |1      |1        |-25 m/s    |15 m/s^2      |3   |
|UltraHeavy    |2         |1       |0      |1        |-45 m/s    |10 m/s^2      |4   |

### Missile

|Missile Type   |Max. Range  |Speed  |Direct Hit Bonus|Near Hit Bonus|Far Hit Bonus|Cooldown|Cost|
|---------------|------------|-------|----------------|--------------|-------------|--------|----|
|Dart           |350 meters  |250 m/s|0               |0             |0            |0 sec   |0   |
|Arrow          |500 meters  |200 m/s|1               |1             |0            |1 sec   |1   |
|**Javelin (*)**|700 meters  |150 m/s|3               |2             |1            |2 sec   |2   |
|Cannon         |900 meters  |100 m/s|6               |4             |2            |3 sec   |3   |
|BFG            |1,200 meters|75 m/s |12              |8             |4            |5 sec   |4   |

