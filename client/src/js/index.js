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
      if (typeof data.Messages !== "undefined") {
        messagesUi(data.Messages);
      }
      if (typeof data.State !== "undefined" && data.State === "Finished") {
        stopGameUi();
      }
      if (typeof data.Game !== "undefined" && data.Game.State === "Start") {
        startGameUi();
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
  const robotArns = JSON.parse(localStorage.getItem("robotArns")) || [];
  const robotArnsElements = [].slice.call(document.getElementsByName("robots"));
  for (let index = 0; index < robotArns.length; index++) {
    robotArnsElements[index].value = robotArns[index];
  }
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
  const robotArns = getRobotArns();
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
    RobotArns: getRobotArns()
  };
  wsClient.doSend(JSON.stringify(request));
}

function getRobotArns() {
  const robotArnsElements = [].slice.call(document.getElementsByName("robots"));
  return robotArnsElements
    .map(robotArn => robotArn.value)
    .filter(robotArn => robotArn.length > 10)
    .map(robotArn => robotArn.trim());
}

function startGameUi() {
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
  messages.forEach(message => {
    messagesElement.appendChild(
      document.createTextNode(`${message.Timestamp}:${message.Text}\n`)
    );
  });
}

window.addEventListener("load", init, false);
