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
      updateBotStats(data.Game.Bots);
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
  restoreBotArns();
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
  const botArns = getBotArnsFromInputs();
  localStorage.setItem("botArns", JSON.stringify(botArns));
  const request = {
    Action: "start",
    BotArns: botArns
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

function restoreBotArns() {
  const botArns = JSON.parse(localStorage.getItem("botArns")) || [];
  const botArnsElements = [].slice.call(document.getElementsByName("bots"));
  for (let index = 0; index < botArns.length; index++) {
    botArnsElements[index].value = botArns[index];
  }
}

function getBotArnsFromInputs() {
  const botArnsElements = [].slice.call(document.getElementsByName("bots"));
  return botArnsElements
    .map(botArn => botArn.value)
    .filter(botArn => botArn.length > 10 && botArn[0] != '#')
    .map(botArn => botArn.trim());
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

function updateBotStats(bots) {
  // https://lit-html.polymer-project.org/guide/template-reference
  const botsStats = document.getElementById("botsStats");
  let botTemplates = [];
  assignBotMedals(bots);
  bots
    .forEach(bot => {
      let botTemplate = html`
        <details ?open="${bot.Status === "Alive"}" class="${bot.Status !== "Alive" ? "bot-dead" : ""}">
          <summary>
            <h4>
              ${bot.Medal} ${bot.Name} (R${bot.Index})
              <span class="tooltip">
                🛈
                <pre class="tooltiptext">${JSON.stringify(bot, null, 2)}</pre>
              </span>
            </h4>
          </summary>
          <table>
            <tr>
              <td>Health: ${Math.round(bot.MaxDamage - bot.Damage)}</td>
              <td>Collisions: ${bot.TotalCollisions}</td>
              <td>Inflicted: ${Math.round(bot.TotalDamageDealt)}</td>
            </tr>
            <tr>
              <td>Shots: ${bot.TotalMissileFiredCount}</td>
              <td>Hits: ${bot.TotalMissileHitCount}</td>
              <td>Kills: ${bot.TotalKills}</td>
            </tr>
            <tr>
              <td>Speed: ${Math.round(bot.Speed)}</td>
              <td>Heading: ${Math.round(bot.Heading)}</td>
              <td>Odometer: ${Math.round(bot.TotalTravelDistance)}</td>
            </tr>
            <tr>
              <td>X: ${Math.round(bot.X)}</td>
              <td>Y: ${Math.round(bot.Y)}</td>
              <td>Reload: ${bot.ReloadCoolDown}</td>
            </tr>
          </table>
        </details>
      `;
      botTemplates.push(botTemplate);
    });
  render(
    html`
      ${botTemplates}
    `,
    botsStats
  );
}

function getAdvanceConfig() {
  var config = {
    BoardWidth: Number(document.getElementById("BoardWidth").value),
    BoardHeight: Number(document.getElementById("BoardHeight").value),
    MinimumSecondsPerTurn: Number(document.getElementById("MinimumSecondsPerTurn").value),
    MaxTurns: Number(document.getElementById("MaxTurns").value),
    DirectHitRange: Number(document.getElementById("DirectHitRange").value),
    NearHitRange: Number(document.getElementById("NearHitRange").value),
    FarHitRange: Number(document.getElementById("FarHitRange").value),
    CollisionRange: Number(document.getElementById("CollisionRange").value),
    BotTimeoutSeconds: Number(
      document.getElementById("BotTimeoutSeconds").value
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

function assignBotMedals(bots) {
  bots.forEach(bot => {
    bot.Score = bot.TotalKills * 1E6 + bot.TotalDamageDealt;
  });
  bots.sort((a, b) => {

    // the higher the score, the closer to the top of the leaderboard
    const deltaScore = b.Score - a.Score;
    if(deltaScore !== 0) {
      return deltaScore;
    }

    // the longer alive the bot has been, the closer to the top of the leaderboard
    const timeOfDeathA = (a.TimeOfDeathGameTurn === -1) ? 1E9 : a.TimeOfDeathGameTurn;
    const timeOfDeathB = (b.TimeOfDeathGameTurn === -1) ? 1E9 : b.TimeOfDeathGameTurn;
    const deltaTimeOfDeath = timeOfDeathB - timeOfDeathA;
    if(deltaTimeOfDeath !== 0) {
      return deltaTimeOfDeath;
    }

    // if all fails, just sort by increasing index
    return a.Index - b.Index;
  });
  for (let index = 0; index < bots.length; index++) {
    const bot = bots[index];
    bot.Medal = giveMedal(index, bot.Score);
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
