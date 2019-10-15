import WebSocketClient from "./webSocketClient.js";
import GameBoard from "./gameBoard.js";
import { html, render } from "https://unpkg.com/lit-html?module";

const mainMenu = document.getElementById("mainMenuContainer");
const gameBoardStatsContainer = document.getElementById(
  "gameBoardStatsContainer"
);
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
    setTimeout(() => startGame(), 500);
  });
  document
    .getElementById("btnStopGame")
    .addEventListener("click", () => stopGame());
  document.getElementById("btnClear").addEventListener("click", () => {
    localStorage.clear();
    window.location.href = "/";
  });
  document.getElementById("btnReset").addEventListener("click", () => {
    stopGame();
    resetGameUi();
  });
  restoreRobotArns();
  restoreAdvanceConfig();
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
    RobotArns: robotArns
  };
  const requestWithConfig = Object.assign(request, getAdvanceConfig());
  wsClient.doSend(JSON.stringify(requestWithConfig));
}

function stopGame() {
  try {
    const request = {
      Action: "stop",
      GameId: sessionStorage.getItem("gameId")
    };
    wsClient.doSend(JSON.stringify(request));
  } catch (error) {
    console.warn("unable to stop the game: " + error);
  }
}

function restoreRobotArns() {
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
  gameBoardStatsContainer.style.display = "block";
  document.getElementById("btnStopGame").disabled = false;
}

function stopGameUi() {
  document.getElementById("btnReset").disabled = false;
  document.getElementById("btnStopGame").disabled = true;
}

function resetGameUi() {
  mainMenu.style.display = "block";
  gameBoardStatsContainer.style.display = "none";
  document.getElementById("btnStopGame").disabled = false;
}

function messagesUi(messages) {
  const messagesElement = document.getElementById("statsBoxMessages");
  messagesElement.innerText = "";
  messages.reverse().forEach(message => {
    messagesElement.appendChild(
      document.createTextNode(`#${message.GameTurn} ${message.Text}\n`)
    );
  });
}

function updateRobotStats(robots) {
  // https://lit-html.polymer-project.org/guide/template-reference
  const robotsStats = document.getElementById("robotsStats");
  let robotTemplates = [];
  robots = robots.map(function(robot) {
    robot.Index = Number(robot.Id.split(":R")[1]);
    return robot;
  });
  let currentRobotPositions = determineRobotLeadingPosition(robots);
  robots
    .sort((a, b) => a.Index - b.Index)
    .forEach(robot => {
      const currentPosition = currentRobotPositions.find(
        x => x.Id === robot.Id
      );
      let robotTemplate = html`
        <details ?open="${robot.State === "Alive"}" class="${robot.State !== "Alive" ? "robot-dead" : ""}">
          <summary>
            <h4>
              ${currentPosition.Medal} ${robot.Name} (R${robot.Index})
              <span class="tooltip">
                🛈
                <pre class="tooltiptext">${JSON.stringify(robot, null, 2)}</pre>
              </span>
            </h4>
          </summary>
          <table>
            <tr>
              <td>Health: ${Math.round(robot.MaxDamage - robot.Damage)}</td>
              <td>Collisions: ${robot.TotalCollisions}</td>
              <td>Inflicted: ${Math.round(robot.TotalDamageDealt)}</td>
            </tr>
            <tr>
              <td>Shots: ${robot.TotalMissileFiredCount}</td>
              <td>Hits: ${robot.TotalMissileHitCount}</td>
              <td>Kills: ${robot.TotalKills}</td>
            </tr>
            <tr>
              <td>Speed: ${Math.round(robot.Speed)}</td>
              <td>Heading: ${Math.round(robot.Heading)}</td>
              <td>Odometer: ${Math.round(robot.TotalTravelDistance)}</td>
            </tr>
            <tr>
              <td>X: ${Math.round(robot.X)}</td>
              <td>Y: ${Math.round(robot.Y)}</td>
              <td>Reload: ${robot.ReloadCoolDown}</td>
            </tr>
          </table>
        </details>
      `;
      robotTemplates.push(robotTemplate);
    });
  render(
    html`
      ${robotTemplates}
    `,
    robotsStats
  );
}

function getAdvanceConfig() {
  var config = {
    BoardWidth: Number(document.getElementById("BoardWidth").value),
    BoardHeight: Number(document.getElementById("BoardHeight").value),
    SecondsPerTurn: Number(document.getElementById("SecondsPerTurn").value),
    MaxTurns: Number(document.getElementById("MaxTurns").value),
    DirectHitRange: Number(document.getElementById("DirectHitRange").value),
    NearHitRange: Number(document.getElementById("NearHitRange").value),
    FarHitRange: Number(document.getElementById("FarHitRange").value),
    CollisionRange: Number(document.getElementById("CollisionRange").value),
    RobotTimeoutSeconds: Number(
      document.getElementById("RobotTimeoutSeconds").value
    ),
    GameLoopType: document.getElementById("GameLoopType").value
  };

  // remove properties with zero value
  Object.keys(config).forEach(key => config[key] === 0 && delete config[key]);
  localStorage.setItem("advanceConfig", JSON.stringify(config));
  return config;
}

function restoreAdvanceConfig() {
  const config = JSON.parse(localStorage.getItem("advanceConfig"));
  if (config) {
    Object.keys(config).forEach(key => {
      document.getElementById(key).value = config[key];
    });
  }
}

function determineRobotLeadingPosition(robots) {
  const leadingRobots = robots.sort((a, b) => {
    if (a.State !== "Alive") {
      return 1;
    }
    if (a.TotalKills < b.TotalKills) {
      return -1;
    } else if (a.TotalKills > b.TotalKills) {
      return 1;
    }
    if (a.Damage < b.Damage) {
      return -1;
    } else if (a.Damage > b.Damage) {
      return 1;
    } else {
      // nothing to split them
      return 0;
    }
  });
  let botMedals = [];
  for (let index = 0; index < leadingRobots.length; index++) {
    const robot = leadingRobots[index];
    botMedals.push({ Id: robot.Id, Medal: giveMedal(index), Position: index });
  }
  return botMedals;
}

function giveMedal(position) {
  switch (position) {
    case 0:
      return "🥇";
    case 1:
      return "🥈";
    case 2:
      return "🥉";
    default:
      return "🏅";
  }
}

window.addEventListener("load", init, false);
