export default class GameBoard {
  constructor(gameBoardContainerElement) {
    this.gameBoardContainerElement = gameBoardContainerElement;
    this.canvas = document.createElement("canvas");
    this.canvas.className = "game-board";
    this.canvas.width = 1000;
    this.canvas.height = 1000;
    gameBoardContainerElement.appendChild(this.canvas);
    this.context = this.canvas.getContext("2d");
  }

  /**
   * Repaint the canvas
   * @param {*} gameStat
   */
  Repaint(gameStat) {
    if (gameStat === null) {
      this._start = new Date();
      this._spinnerInterval = setInterval(() => {
        this._spinner();
      }, 1000 / 30);
      return;
    }
    this._clear();
    if (gameStat.GameBoard.Status === "Start") {
      this.canvas.width = gameStat.GameBoard.BoardWidth;
      this.canvas.height = gameStat.GameBoard.BoardHeight;
      return;
    }
    clearTimeout(this._spinnerInterval);
    if (gameStat.GameBoard.Status === "NextTurn") {
      this._bots(gameStat.Game, gameStat.GameBoard.Bots);
      this._missiles(gameStat.Game, gameStat.GameBoard.Missiles);
      return;
    }
    if (gameStat.GameBoard.Status === "Finished") {
      this._gameOver();
    }
  }

  _clear() {
    this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
  }

  _bots(game, bots) {
    this.context.save();
    this.context.font = "16px 'Press Start 2P'";
    this.context.fillStyle = "white";
    this.context.textAlign = "center";
    this.context.textBaseline = "middle";
    for (let index = 0; index < bots.length; index++) {
      const bot = bots[index];
      if(bot.Status === "Alive") {
        this.context.fillText(
          bot.Index,
          Math.round(bot.X),
          Math.round(this.canvas.height - bot.Y)
        );

        // draw circle around bot with collision radius
        // this.context.beginPath();
        // this.context.strokeStyle = "yellow";
        // this.context.arc(
        //   Math.round(bot.X),
        //   Math.round(bot.Y),
        //   Math.round(game.CollisionRange),
        //   0,
        //   2 * Math.PI
        // );
        // this.context.stroke();
      }
    }
    this.context.restore();
  }

  _missiles(game, missiles) {
    for (let index = 0; index < missiles.length; index++) {
      const missile = missiles[index];
      this.context.save();

      //(x,y) should start on the bottom left - https://stackoverflow.com/a/7707406/2414540
      this.context.translate(0, this.canvas.height);
      this.context.scale(1, -1);
      this.context.beginPath();
      switch (missile.Status) {
        case "Flying":
          this.context.moveTo(Math.round(missile.X), Math.round(missile.Y));
          const lineLength = 12;
          this.context.lineTo(
            Math.round(
              missile.X +
                Math.sin((missile.Heading * Math.PI) / 180) * lineLength
            ),
            Math.round(
              missile.Y +
                Math.cos((missile.Heading * Math.PI) / 180) * lineLength
            )
          );
          break;
        case "ExplodingDirect":
          this.context.arc(
            Math.round(missile.X),
            Math.round(missile.Y),
            Math.round(game.DirectHitRange),
            0,
            2 * Math.PI
          );
          break;
        case "ExplodingNear":
          this.context.arc(
            Math.round(missile.X),
            Math.round(missile.Y),
            Math.round(game.NearHitRange),
            0,
            2 * Math.PI
          );
          break;
        case "ExplodingFar":
          this.context.arc(
            Math.round(missile.X),
            Math.round(missile.Y),
            Math.round(game.FarHitRange),
            0,
            2 * Math.PI
          );
          break;
      }
      this.context.strokeStyle = "red";
      this.context.lineWidth = 2;
      this.context.stroke();
      this.context.restore();
    }
  }

  _spinner() {
    // https://codepen.io/reneras/pen/HFrmC
    const lines = 16;
    const ctx = this.canvas.getContext("2d");
    ctx.save();
    const rotation =
      parseInt(((new Date() - this._start) / 1000) * lines) / lines;
    ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    ctx.translate(this.canvas.width / 2, this.canvas.height / 2);
    ctx.rotate(Math.PI * 2 * rotation);
    for (let i = 0; i < lines; i++) {
      ctx.beginPath();
      ctx.rotate((Math.PI * 2) / lines);
      ctx.moveTo(this.canvas.width / 10, 0);
      ctx.lineTo(this.canvas.width / 4, 0);
      ctx.lineWidth = this.canvas.width / 30;
      ctx.strokeStyle = "rgba(255, 255, 255," + i / lines + ")";
      ctx.stroke();
    }
    ctx.restore();
  }

  _gameOver() {
    const ctx = this.canvas.getContext("2d");
    ctx.save();
    ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    ctx.font = "100px 'Press Start 2P'";

    // Create gradient
    var gradient = ctx.createLinearGradient(0, 0, this.canvas.width, 0);
    gradient.addColorStop("0", " green");
    gradient.addColorStop("0.5", "blue");
    gradient.addColorStop("1.0", "red");
    // Fill with gradient
    ctx.fillStyle = gradient;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText("Game Over", this.canvas.width / 2, this.canvas.height / 2);
    ctx.restore();
  }
}
