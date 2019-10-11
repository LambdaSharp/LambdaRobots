export default class GameBoard {
  constructor(gameBoardContainerElement) {
    this.gameBoardContainerElement = gameBoardContainerElement;
    this.canvas = document.createElement("canvas");
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
    if (gameStat.Game.State === "Start") {
      this.canvas.width = gameStat.Game.BoardWidth;
      this.canvas.height = gameStat.Game.BoardHeight;
      return;
    }
    clearTimeout(this._spinnerInterval);
    if (gameStat.Game.State === "NextTurn") {
      this._robots(gameStat.Game, gameStat.Game.Robots);
      this._missiles(gameStat.Game, gameStat.Game.Missiles);
      return;
    }
  }

  _clear() {
    this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
  }

  _robots(game, robots) {
    this.context.save();
    this.context.font = "16px Arial";
    this.context.fillStyle = "black";
    this.context.textAlign = "center";
    this.context.textBaseline = "middle";
    for (let index = 0; index < robots.length; index++) {
      const robot = robots[index];
      this.context.fillText(
        index + 1,
        Math.round(robot.X),
        Math.round(robot.Y)
      );

      // draw circle around robot with collision radius
      this.context.beginPath();
      this.context.strokeStyle = "blue";
      this.context.arc(Math.round(robot.X), Math.round(robot.Y), Math.round(game.CollisionRange), 0, 2 * Math.PI);
      this.context.stroke();
    }
    this.context.restore();
  }

  _missiles(game, missiles) {
    for (let index = 0; index < missiles.length; index++) {
      const missile = missiles[index];
      this.context.save();
      this.context.beginPath();
      switch(missile.State) {
      case "Flying":
        this.context.moveTo(Math.round(missile.X), Math.round(missile.Y));
        const lineLength = 12;
        this.context.lineTo(
          Math.round(
            missile.X + Math.sin((missile.Heading * Math.PI) / 180) * lineLength
          ),
          Math.round(
            missile.Y + Math.cos((missile.Heading * Math.PI) / 180) * lineLength
          )
        );
        break;
      case "ExplodingDirect":
        this.context.arc(Math.round(missile.X), Math.round(missile.Y), Math.round(game.DirectHitRange), 0, 2 * Math.PI);
        break;
      case "ExplodingNear":
        this.context.arc(Math.round(missile.X), Math.round(missile.Y), Math.round(game.NearHitRange), 0, 2 * Math.PI);
        break;
      case "ExplodingFar":
        this.context.arc(Math.round(missile.X), Math.round(missile.Y), Math.round(game.FarHitRange), 0, 2 * Math.PI);
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
      ctx.strokeStyle = "rgba(0, 0, 0," + i / lines + ")";
      ctx.stroke();
    }
    ctx.restore();
  }
}
