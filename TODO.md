# TODO - List of Improvements

## DynamoDB Records

### User

* User ID (from Amazon Cognito)
* Username

### Registered Bots

* User ID
* Bot URL
* Bot description
* Number of matches played
* Number of matches won
* Total bots killed
* Total damage dealt

### User Session

* User ID
* Connection ID
* Game ID

### Games

* Game ID
* Bot URLs
* Game state

### DynamoDB Operations

* Login (User ID, Connection ID)
* Logout (User ID, Connection ID)
* WatchGame (User ID, Game ID)
* UnwatchGame (User ID, Game ID)
* CreateBot (User ID, Bot URL, Description)
* UpdateBot (User ID, Bot URL, Description)
* DeleteBot (User ID, Bot URL)
* CreateGame (User ID, Bot URLs, Game Settings)
* AbandonGame (User ID, Game ID)
