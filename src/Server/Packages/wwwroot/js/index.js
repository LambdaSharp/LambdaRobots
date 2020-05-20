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
      if (data.Game === null) {
        return;
      }
      gameBoardClient.Repaint(data);
      updateRobotStats(data.Game.Robots);
      if (typeof data.Game.Messages !== "undefined") {
        messagesUi(data.Game.Messages);
      }
      if (data.Game.Status === "Start") {
        sessionStorage.setItem("gameId", data.Game.Id);
        startGameUi();
      }
      if (data.Game.Status === "Finished") {
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
  assignRobotMedals(robots);
  robots
    .forEach(robot => {
      let robotTemplate = html`
        <details ?open="${robot.Status === "Alive"}" class="${robot.Status !== "Alive" ? "robot-dead" : ""}">
          <summary>
            <h4>
              ${robot.Medal} ${robot.Name} (R${robot.Index})
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
    )
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
      const elem = document.getElementById(key);
      if(elem != null) {
        elem.value = config[key];
      }
    });
  }
}

function assignRobotMedals(robots) {
  robots.forEach(robot => {
    robot.Score = robot.TotalKills * 1E6 + robot.TotalDamageDealt;
  });
  robots.sort((a, b) => {

    // the higher the score, the closer to the top of the leaderboard
    const deltaScore = b.Score - a.Score;
    if(deltaScore !== 0) {
      return deltaScore;
    }

    // the longer alive the robot has been, the closer to the top of the leaderboard
    const timeOfDeathA = (a.TimeOfDeathGameTurn === -1) ? 1E9 : a.TimeOfDeathGameTurn;
    const timeOfDeathB = (b.TimeOfDeathGameTurn === -1) ? 1E9 : b.TimeOfDeathGameTurn;
    const deltaTimeOfDeath = timeOfDeathB - timeOfDeathA;
    if(deltaTimeOfDeath !== 0) {
      return deltaTimeOfDeath;
    }

    // if all fails, just sort by increasing index
    return a.Index - b.Index;
  });
  for (let index = 0; index < robots.length; index++) {
    const robot = robots[index];
    robot.Medal = giveMedal(index, robot.Score);
  }
}

function giveMedal(position, score) {
  if (score === 0) {
    return "🤖";
  }
  switch (position) {
    case 0:
      return "🥇";
    case 1:
      return "🥈";
    case 2:
      return "🥉";
    default:
      return "🤖";
  }
}

window.addEventListener("load", init, false);
