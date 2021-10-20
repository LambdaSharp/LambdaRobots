# λ-Robots

In λ-Robots (pronounced _Lambda Robots_), you program a battle bot that participates on a square game field. Each turn, the server invokes your bot's Lambda function to get its action for the turn until either the bot wins or is destroyed.

λ-Robots is a port of the 90s [P-Robots](https://corewar.co.uk/probots.htm) game to AWS Serverless .NET using [LambdaSharp](https://lambdasharp.net).

## Level 0: Prerequisites

### Install SDK & Tools
Make sure the following tools are installed.
* [Download and install the .NET Core SDK](https://dotnet.microsoft.com/download)
* [Download and install the AWS Command Line Interface](https://aws.amazon.com/cli/)
* [Download and install Git Command Line Interface](https://git-scm.com/downloads)

### Setup AWS Account and CLI
The challenge requires an AWS account. AWS provides a [*Free Tier*](https://aws.amazon.com/free/), which is sufficient for most challenges.
* [Create an AWS Account](https://aws.amazon.com)
* [Configure your AWS profile with the AWS CLI for us-west-2 (Oregon)](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-configure.html#cli-quick-configuration)

### Setup LambdaSharp Deployment Tier

The following command uses the `dotnet` CLI to install the LambdaSharp CLI.
```bash
dotnet tool install --global LambdaSharp.Tool
```
**NOTE:** if you have installed LambdaSharp.Tool in the past, you will need to remove it first by running `dotnet tool uninstall -g LambdaSharp.Tool` first.

The following command uses the LambdaSharp CLI to create a new deployment tier on the default AWS account. Specify another account using `--aws-profile ACCOUNT_NAME`.
```bash
lash init --quick-start
```

### Clone Git Challenge Repository

Open a command line and navigate to your projects folder. Run the following command to clone the λ-Robots challenge directory into a `LambdaRobots` sub-folder.
```bash
git clone https://github.com/LambdaSharp/LambdaRobots.git
cd LambdaRobots
```

## Level 1: Run λ-Robots Server

The following command deploys the `LambdaRobots.Server` module from the `lambdasharp` repository into the AWS account using [CloudFormation](https://aws.amazon.com/cloudformation/).
```bash
lash deploy LambdaRobots.Server:2.0@lambdasharp
```

Once the command has finished running, the output will show the website URL for the λ-Robots server.
```
Stack output values:
=> LambdaRobotsServerUrl: URL for the Lambda-Robots web-server = http://lambdarobots-server-websitebucket-g54w2llfcjhq.s3-website-us-west-2.amazonaws.com
```

Finally, build and deploy the `BringYourOwnBot` module, which will be the project you will be working on
The following command builds and deploys the AWS Lambda function for your bot:

```bash
lash deploy src/Bots/BringYourOwnBot
```

**NOTE:** Open `src/Bots/BringYourOwnBot/BotFunction/Function.cs` and customize the `Name` of your bot to distinguish it from other bots.

You can add the bot lambda function ARN to the game board client in the browser.  You can add the ARN multiple times.

![Game configuration](screenShotConfigure.png)

Use the **Advance Configuration** to change any default settings.  Use **Clear Saved Config** to reset all settings to default.

## Level 2: Create an Attack Strategy

Deploy `TargetBbot` to your account and add its ARN three times to the λ-Robots server to create three targets.

```bash
lash deploy LambdaRobots.TargetBot:2.0@lambdasharp
```

Now update the behavior of `BringYourOwnBot` to shoot down the target bots. For example, you can use luck, like `YosemiteSamBot`, which shoots in random directions, or targeting like `HotShotBot`. The latter uses the `ScanAsync()` method to find enemies and aim missiles at them. Remember that other bots may be out of radar range, requiring your bot to move periodically. Also, your bot can be damaged by its own missiles. Check `Game.FarHitRange` to make sure your target is beyond the damage range. If you don't mind a bit of self-inflicted pain, you can also use `Game.NearHitRange` or even `Game.DirectHitRange` instead.

## Level 3: Create an Evasion Strategy

Deploy `YosemiteSamBot` to your account and its ARN twice to the λ-Robots server to create two attackers.
```bash
lash deploy LambdaRobots.YosemiteSamBot:2.0@lambdasharp
```

Now update the behavior of `BringYourOwnBot` to avoid getting shot. For example, you can continuous motion, like `YosemiteSamBot`, which zig-zags across the board, or reacting to damage like `HotShotBot`. Beware that a bot cannot change heading without suddenly stopping if its speed exceeds `Bot.MaxSpeed`.

## Level 4: Take on the Champ

Deploy `HotShotBot` to your account and its ARN once to the λ-Robots server to create one formidable foe.
```bash
lash deploy LambdaRobots.HotShotBot:2.0@lambdasharp
```

Consider modifying your bot build by tuning the engine, armor, missile, and radar to suit your attack and evasion strategies. Remember that your build cannot exceed 8 points, or your bot will be disqualified from the competition.

## BOSS LEVEL: Enter the Multi-Team Deathmatch Competition

For the boss level, your opponent is every other team! Submit your bot ARN to final showdown and see how well it fares.

**May the odds be ever in your favor!**

## Programming Reference

### Pre-Build Lambda-Robots

The `src/Bots` folder contains additional bots to deploy that have different behaviors.
Next, we need a few bots to battle it out. The cloned git repository contains a few that are ready to be deployed.
* `TargetBot`: This is a stationary bot for other bots to practice on.
* `YosemiteSamBot`: This bot runs around shooting in random directions as fast as it can.
* `HotShotBot`: This bot uses its radar to find other bots and fire at them. When hit, this bot moves around the board.

### LambdaRobots SDK

Derive your Lambda-Robot from the `ALambdaRobotFunction` provided by the SDK.

#### Abstract Methods
The base class requires two methods to be implemented:

|Method             |Description|
|-------------------|-----------|
|`public Task<LambdaRobotBuild> GetBuildAsync()`|This method returns the bot build information, including its name, armor, engine, missile type, and radar. Note that by default, a build cannot exceed 8 points or the bot will be disqualified at the beginning of the match.|
|`public Task GetActionAsync()`|This method returns the actions taken by the bot during the turn|

#### Properties
The most commonly needed properties are readily available as properties from the base class. Additional information about the game or the bot is available via the `Game` and `Bot` properties, respectively.

|Property           |Type    |Description |
|-------------------|--------|------------|
|`BreakingDistance` |`float` |Distance required to stop at the current speed.|
|`Damage`           |`float` |Bot damage. Value is always between 0 and `Bot.MaxDamage`. When the value is equal to `Bot.MaxDamage` the bot is considered killed.|
|`Game`             |`Game`  |Game information data structure. _See below._|
|`Heading`          |`float` |Bot heading. Value is always between `-180` and `180`.|
|`Random`           |`Random`|Initialized random number generator. Instance of [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0).|
|`ReloadCoolDown`   |`float` |Number of seconds until the missile launcher is ready again.|
|`Bot`              |`LambdaBot` |Bot information data structure. _See below._|
|`Speed`            |`float` |Bot speed. Value is between `0` and `Bot.MaxSpeed`.|
|`State`            |`TState`|Bot state based on custom type. Used to store information between turns.|
|`X`                |`float` |Horizontal position of bot. Value is between `0` and `Game.BoardWidth`.|
|`Y`                |`float` |Vertical position of bot. Value is between `0` and `Game.BoardHeight`.|

##### `Bot` Properties
|Property           |Type    |Description |
|-------------------|--------|------------|
|`Acceleration`|`float`|Acceleration when speeding up. (m/s^2)|
|`CollisionDamage`|`float`|Amount of damage the bot receives from a collision.|
|`Damage`|`float`|Accumulated bot damage. Between `0` and `MaxDamage`.|
|`Deceleration`|`float`|Deceleration when speeding up. (m/s^2)|
|`DirectHitDamage`|`float`|Amount of damage the bot receives from a direct hit.|
|`FarHitDamage`|`float`|Amount of damage the bot receives from a far hit.|
|`Heading`|`float`|Bot heading. Between `0` and `360`. (degrees)|
|`Id`|`string`|Globally unique bot ID.|
|`Index`|`int`|Index position of bot. Starts at `0`.|
|`LastStatusUpdate`|`string`|Timestamp of when the bot status was last updated.|
|`MaxDamage`|`float`|Maximum damage before the bot is destroyed.|
|`MaxSpeed`|`float`|Maximum speed for bot. (m/s)|
|`MaxTurnSpeed`|`float`|Maximum speed at which the bot can change heading without a sudden stop. (m/s)|
|`MissileDirectHitDamageBonus`|`float`|Bonus damage on target for a direct hit.|
|`MissileFarHitDamageBonus`|`float`|Bonus damage on target for a far hit.|
|`MissileNearHitDamageBonus`|`float`|Bonus damage on target for a near hit.|
|`MissileRange`|`float`|Maximum range for missile. (m)|
|`MissileReloadCooldown`|`float`|Number of seconds between each missile launch. (s)|
|`MissileVelocity`|`float`|Travel velocity for missile. (m/s)|
|`Name`|`string`|Bot display name.|
|`NearHitDamage`|`float`|Amount of damage the bot receives from a near hit.|
|`RadarMaxResolution`|`float`|Maximum degrees the radar can scan beyond the selected heading. (degrees)|
|`RadarRange`|`float`|Maximum range at which the radar can detect an opponent. (m)|
|`ReloadCoolDown`|`float`|Number of seconds before the bot can fire another missile. (s)|
|`Speed`|`float`|Bot speed. Between `0` and `MaxSpeed`. (m/s)|
|`Status`|`LambdaRobotStatus`|Bot status. Either `Alive` or `Dead`.|
|`TargetHeading`|`float`|Desired heading for bot. The heading will be adjusted accordingly every turn. (degrees)|
|`TargetSpeed`|`float`|Desired speed for bot. The current speed will be adjusted accordingly every turn. (m/s)|
|`TimeOfDeathGameTurn`|`int`|Game turn during which the bot died. `-1` if bot is alive.|
|`TotalCollisions`|`int`|Number of collisions with walls or other bots during match.|
|`TotalDamageDealt`|`float`|Damage dealt by missiles during match.|
|`TotalKills`|`int`|Number of confirmed kills during match.|
|`TotalMissileFiredCount`|`int`|Number of missiles fired by bot during match.|
|`TotalMissileHitCount`|`int`|Number of missiles that hit a target during match.|
|`TotalTravelDistance`|`float`|Total distance traveled by bot during the match. (m)|
|`X`|`float`|Bot horizontal position.|
|`Y`|`float`|Bot vertical position.|

##### `Game` Properties
|Property           |Type    |Description |
|-------------------|--------|------------|
|`ApiUrl`           |`string`|URL for game server API.|
|`BoardHeight`      |`float` |Height of the game board.|
|`BoardWidth`       |`float` |Width of the game board.|
|`CollisionRange`   |`float` |Distance between bots to count as a collision.|
|`DirectHitRange`   |`float` |Distance for missile impact to count as direct hit.|
|`FarHitRange`      |`float` |Distance for missile impact to count as far hit.|
|`GameTurn`         |`int`   |Current game turn. Starts at `1`.|
|`Id`               |`string`|Unique Game ID.|
|`LastStatusUpdate` |`string`|Timestamp of when the board status was last updated.|
|`MaxBuildPoints`   |`int`   |Maximum number of build points a bot can use.|
|`MaxGameTurns`     |`int`   |Maximum number of turns before the game ends in a draw.|
|`NearHitRange`     |`float` |Distance for missile impact to count as near hit.|
|`SecondsPerTurn`   |`float` |Number of seconds elapsed per game turn.|

#### Primary Methods
The following methods represent the core capabilities of the bot. They are used to move, fire missiles, and scan its surroundings.

|Method             |Description|
|-------------------|-----------|
|`void FireMissile(float heading, float distance)`|Fire a missile in a given direction with impact at a given distance. A missile can only be fired if `Bot.ReloadCoolDown` is `0`.|
|`Task<float?> ScanAsync(float heading, float resolution)`|Scan the game board in a given heading and resolution. The resolution specifies in the scan arc centered on `heading` with +/- `resolution` tolerance. The max resolution is limited to `Bot.RadarMaxResolution`.|
|`void SetHeading(float heading)`|Set heading in which the bot is moving. Current speed must be below `Bot.MaxTurnSpeed` to avoid a sudden stop.|
|`void SetSpeed(float speed)`|Set the speed for the bot. Speed is adjusted according to `Bot.Acceleration` and `Bot.Deceleration` characteristics.|

#### Support Methods
The following methods are provided to make some common operations easier, but do not introduce n

|Method             |Description|
|-------------------|-----------|
|`float AngleToXY(float x, float y)`|Determine angle in degrees relative to current bot position. Return value range from `-180` to `180` degrees.|
|`float DistanceToXY(float x, float y)`|Determine distance relative to current bot position.|
|`void FireMissileToXY(float x, float y)`|Fire a missile in at the given position. A missile can only be fired if `Bot.ReloadCoolDown` is `0`.|
|`bool MoveToXY(float x, float y)`|Adjust speed and heading to move bot to specified coordinates. Call this method on every turn to keep adjusting the speed and heading until the destination is reached.|
|`float NormalizeAngle(float angle)`|Normalize angle to be between `-180` and `180` degrees.|

### Bot Build

By default, 8 build points are available to allocate in any fashion. The bot is disqualified if its build exceeds the maximum number of build points.

The default configuration for each is shown in bold font and an asterisk (*).

#### Radar

|Radar Type      |Radar Range |Radar Resolution|Points|
|----------------|------------|----------------|------|
|UltraShortRange |200 meters  |45 degrees      |0     |
|ShortRange      |400 meters  |20 degrees      |1     |
|**MidRange (*)**|600 meters  |10 degrees      |2     |
|LongRange       |800 meters  |8 degrees       |3     |
|UltraLongRange  |1,000 meters|5 degrees       |4     |

#### Engine

|Engine Type     |Max. Speed |Acceleration|Points|
|----------------|-----------|------------|------|
|Economy         |60 m/s     |7 m/s^2     |0     |
|Compact         |80 m/s     |8 m/s^2     |1     |
|**Standard (*)**|100 m/s    |10 m/s^2    |2     |
|Large           |120 m/s    |12 m/s^2    |3     |
|ExtraLarge      |140 m/s    |13 m/s^2    |4     |

#### Armor

|Armor Type    |Direct Hit|Near Hit|Far Hit|Collision|Max. Speed |Deceleration  |Points|
|--------------|----------|--------|-------|---------|-----------|--------------|------|
|UltraLight    |50        |25      |12     |10       |+35 m/s    |30 m/s^2      |0     |
|Light         |16        |8       |4      |3        |+25 m/s    |25 m/s^2      |1     |
|**Medium (*)**|8         |4       |2      |2        |-          |20 m/s^2      |2     |
|Heavy         |4         |2       |1      |1        |-25 m/s    |15 m/s^2      |3     |
|UltraHeavy    |2         |1       |0      |1        |-45 m/s    |10 m/s^2      |4     |

#### Missile

|Missile Type   |Max. Range  |Velocity|Direct Hit Bonus|Near Hit Bonus|Far Hit Bonus|Cooldown|Points|
|---------------|------------|--------|----------------|--------------|-------------|--------|------|
|Dart           |1,200 meters|250 m/s |0               |0             |0            |0 sec   |0     |
|Arrow          |900 meters  |200 m/s |1               |1             |0            |1 sec   |1     |
|**Javelin (*)**|700 meters  |150 m/s |3               |2             |1            |2 sec   |2     |
|Cannon         |500 meters  |100 m/s |6               |4             |2            |3 sec   |3     |
|BFG            |350 meters  |75 m/s  |12              |8             |4            |5 sec   |4     |

