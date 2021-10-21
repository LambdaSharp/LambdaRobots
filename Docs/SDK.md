## LambdaRobots SDK

Derive your Lambda-Robot from the `ABotFunction<TState>` found in `src/LambdaRobots/LambdaRobots.csproj`.

## Abstract Methods
The base class requires two methods to be implemented:

|Method             |Description|
|-------------------|-----------|
|`public Task<GetBuildResponse> GetBuildAsync()`|This method returns the bot build information, including its name, armor, engine, missile type, and radar. Note that by default, a build cannot exceed 8 points or the bot will be disqualified at the beginning of the match.|
|`public Task GetActionAsync()`|This method returns the actions taken by the bot during the turn|

## Properties
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

### `Bot` Properties
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

### `Game` Properties
|Property               |Type    |Description |
|-----------------------|--------|------------|
|`ApiUrl`               |`string`|URL for game server API.|
|`BoardHeight`          |`float` |Height of the game board.|
|`BoardWidth`           |`float` |Width of the game board.|
|`CollisionRange`       |`float` |Distance between bots to count as a collision.|
|`DirectHitRange`       |`float` |Distance for missile impact to count as direct hit.|
|`FarHitRange`          |`float` |Distance for missile impact to count as far hit.|
|`GameTurn`             |`int`   |Current game turn. Starts at `1`.|
|`Id`                   |`string`|Unique Game ID.|
|`LastStatusUpdate`     |`string`|Timestamp of when the board status was last updated.|
|`MaxBuildPoints`       |`int`   |Maximum number of build points a bot can use.|
|`MaxGameTurns`         |`int`   |Maximum number of turns before the game ends in a draw.|
|`MinimumSecondsPerTurn`|`float` |Number of seconds elapsed between game turns.|
|`NearHitRange`         |`float` |Distance for missile impact to count as near hit.|
|`SecondsSinceLastTurn` |`float` |Number of seconds elapsed since the last game turn.|

## Primary Methods
The following methods represent the core capabilities of the bot. They are used to move, fire missiles, and scan its surroundings.

|Method             |Description|
|-------------------|-----------|
|`void FireMissile(float heading, float distance)`|Fire a missile in a given direction with impact at a given distance. A missile can only be fired if `Bot.ReloadCoolDown` is `0`.|
|`Task<float?> ScanAsync(float heading, float resolution)`|Scan the game board in a given heading and resolution. The resolution specifies in the scan arc centered on `heading` with +/- `resolution` tolerance. The max resolution is limited to `Bot.RadarMaxResolution`.|
|`void SetHeading(float heading)`|Set heading in which the bot is moving. Current speed must be below `Bot.MaxTurnSpeed` to avoid a sudden stop.|
|`void SetSpeed(float speed)`|Set the speed for the bot. Speed is adjusted according to `Bot.Acceleration` and `Bot.Deceleration` characteristics.|

## Support Methods
The following methods are provided to make some common operations easier, but do not introduce n

|Method             |Description|
|-------------------|-----------|
|`float AngleToXY(float x, float y)`|Determine angle in degrees relative to current bot position. Return value range from `-180` to `180` degrees.|
|`float DistanceToXY(float x, float y)`|Determine distance relative to current bot position.|
|`void FireMissileToXY(float x, float y)`|Fire a missile in at the given position. A missile can only be fired if `Bot.ReloadCoolDown` is `0`.|
|`bool MoveToXY(float x, float y)`|Adjust speed and heading to move bot to specified coordinates. Call this method on every turn to keep adjusting the speed and heading until the destination is reached.|
|`float NormalizeAngle(float angle)`|Normalize angle to be between `-180` and `180` degrees.|

# Bot Build

By default, 8 build points are available to allocate in any fashion. The bot is disqualified if its build exceeds the maximum number of build points.

The default configuration for each is shown in bold font and an asterisk (*).

## Radar

|Radar Type      |Radar Range |Radar Resolution|Points|
|----------------|------------|----------------|------|
|UltraShortRange |200 meters  |45 degrees      |0     |
|ShortRange      |400 meters  |20 degrees      |1     |
|**MidRange (*)**|600 meters  |10 degrees      |2     |
|LongRange       |800 meters  |8 degrees       |3     |
|UltraLongRange  |1,000 meters|5 degrees       |4     |

## Engine

|Engine Type     |Max. Speed |Acceleration|Points|
|----------------|-----------|------------|------|
|Economy         |60 m/s     |7 m/s^2     |0     |
|Compact         |80 m/s     |8 m/s^2     |1     |
|**Standard (*)**|100 m/s    |10 m/s^2    |2     |
|Large           |120 m/s    |12 m/s^2    |3     |
|ExtraLarge      |140 m/s    |13 m/s^2    |4     |

## Armor

|Armor Type    |Direct Hit|Near Hit|Far Hit|Collision|Max. Speed |Deceleration  |Points|
|--------------|----------|--------|-------|---------|-----------|--------------|------|
|UltraLight    |50        |25      |12     |10       |+35 m/s    |30 m/s^2      |0     |
|Light         |16        |8       |4      |3        |+25 m/s    |25 m/s^2      |1     |
|**Medium (*)**|8         |4       |2      |2        |-          |20 m/s^2      |2     |
|Heavy         |4         |2       |1      |1        |-25 m/s    |15 m/s^2      |3     |
|UltraHeavy    |2         |1       |0      |1        |-45 m/s    |10 m/s^2      |4     |

## Missile

|Missile Type   |Max. Range  |Velocity|Direct Hit Bonus|Near Hit Bonus|Far Hit Bonus|Cooldown|Points|
|---------------|------------|--------|----------------|--------------|-------------|--------|------|
|Dart           |1,200 meters|250 m/s |0               |0             |0            |0 sec   |0     |
|Arrow          |900 meters  |200 m/s |1               |1             |0            |1 sec   |1     |
|**Javelin (*)**|700 meters  |150 m/s |3               |2             |1            |2 sec   |2     |
|Cannon         |500 meters  |100 m/s |6               |4             |2            |3 sec   |3     |
|BFG            |350 meters  |75 m/s  |12              |8             |4            |5 sec   |4     |

