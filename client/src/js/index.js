import WebSocketClient from "./webSocketClient.js";
import GameBoard from "./gameBoard.js";

const mainMenu = document.getElementById("mainMenuContainer");
const gameBoardStatsContainer = document.getElementById(
  "gameBoardStatsContainer"
);
const leaderBoard = document.getElementById("leaderBoardContainer");
let wsClient;
async function init() {
  const config = await getConfig();
  const gameBoardClient = new GameBoard(
    document.getElementById("gameBoardContainer")
  );
  wsClient = new WebSocketClient(
    config.wss,
    document.getElementById("output"),
    5000,
    data => {
      gameBoardClient.Repaint(data);
      updateRobotStats(data.Game.Robots);
      if (typeof data.Game.Messages !== "undefined") {
        messagesUi(data.Game.Messages);
      }
      if (data.Game.State === "Start") {
        sessionStorage.setItem("gameId", data.Game.Id);
        startGameUi();
      }
      if (data.Game.State === "Finished") {
        stopGameUi();
      }
    }
  );
  document.getElementById("btnStartGame").addEventListener("click", () => {
    startGameUi();
    gameBoardClient.Repaint(null);
    setTimeout(() => {
      startGame();
    }, 500);
  });
  document.getElementById("btnStopGame").addEventListener("click", () => {
    stopGame();
  });
  document.getElementById("btnClear").addEventListener("click", () => {
    localStorage.clear();
    window.location.href = "/";
  });
  fillRobotArnsFromLocalStorage();
}

async function getConfig() {
  try {
    const response = await fetch("/config.json");
    return await response.json();
  } catch (error) {
    console.error(error);
    throw error;
  }
}

function startGame() {
  const robotArns = getRobotArnsFromInputs();
  localStorage.setItem("robotArns", JSON.stringify(robotArns));
  const request = {
    Action: "start",
    RobotArns: robotArns,
    BoardWidth: 1000,
    BoardHeight: 1000,
    MaxTurns: 150
  };
  wsClient.doSend(JSON.stringify(request));
}

function stopGame() {
  const request = {
    Action: "stop",
    GameId: sessionStorage.getItem("gameId")
  };
  wsClient.doSend(JSON.stringify(request));
}

function fillRobotArnsFromLocalStorage() {
  const robotArns = JSON.parse(localStorage.getItem("robotArns")) || [];
  const robotArnsElements = [].slice.call(document.getElementsByName("robots"));
  for (let index = 0; index < robotArns.length; index++) {
    robotArnsElements[index].value = robotArns[index];
  }
}

function getRobotArnsFromInputs() {
  const robotArnsElements = [].slice.call(document.getElementsByName("robots"));
  return robotArnsElements
    .map(robotArn => robotArn.value)
    .filter(robotArn => robotArn.length > 10)
    .map(robotArn => robotArn.trim());
}

function startGameUi() {
  messagesUi([]);
  mainMenu.style.display = "none";
  leaderBoard.style.display = "none";
  gameBoardStatsContainer.style.display = "block";
}

function stopGameUi() {
  mainMenu.style.display = "block";
  gameBoardStatsContainer.style.display = "none";
  mainMenu.style.display = "block";
  leaderBoard.style.display = "block";
}

function messagesUi(messages) {
  const messagesElement = document.getElementById("statsBoxMessages");
  messagesElement.innerText = "";
  messages.reverse().forEach(message => {
    messagesElement.appendChild(document.createTextNode(`${message.Text}\n`));
  });
}

function updateRobotStats(robots) {
  const robotsStats = document.getElementById("robotsStats");
  robotsStats.innerText = "";
  for (let index = 0; index < robots.length; index++) {
    const robot = robots[index];
    const robotContainer = createElement("div");
    robotContainer.appendChild(createElement("h3", `${robot.Name} (R${index}) (${robot.State})`));
    const robotPrimaryStatsContainer = createElement("table");
    const robotTr1 = createElement("tr");
    robotTr1.appendChild(createElement("td", `Damage: ${robot.Damage}`));
    robotTr1.appendChild(createElement("td", `Collision Damage: ${robot.CollisionDamage}`));
    robotTr1.appendChild(createElement("td", `Total Damage Dealt: ${robot.TotalDamageDealt}`));
    robotPrimaryStatsContainer.appendChild(robotTr1);
    const robotTr2 = createElement("tr");
    robotTr2.appendChild(createElement("td", `Missile Fire #: ${robot.TotalMissileFiredCount}`));
    robotTr2.appendChild(createElement("td", `Missile Hit #: ${robot.TotalMissileHitCount}`));
    robotTr2.appendChild(createElement("td", `Total Kills: ${robot.TotalKills}`));
    robotPrimaryStatsContainer.appendChild(robotTr2);
    const robotTr3 = createElement("tr");
    robotTr3.appendChild(createElement("td", `X: ${Math.round(robot.X)}`));
    robotTr3.appendChild(createElement("td", `Y: ${Math.round(robot.Y)}`));
    robotTr3.appendChild(createElement("td", `Total Travel Distance: ${Math.round(robot.TotalTravelDistance)}`));
    robotPrimaryStatsContainer.appendChild(robotTr3);
    const robotPrimaryAllStats = createElement("pre");
    robotPrimaryAllStats.innerText = JSON.stringify(robot, null, 2);
    robotContainer.appendChild(robotPrimaryStatsContainer);
    //robotContainer.appendChild(robotPrimaryAllStats);
    robotsStats.appendChild(robotContainer);
  }
}

function createElement(tag, text = "") {
  const element = document.createElement(tag);
  if (text.length > 0) {
    element.appendChild(document.createTextNode(text));
  }
  return element;
}

window.addEventListener("load", init, false);
