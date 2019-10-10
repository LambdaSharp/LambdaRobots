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
    if (
      typeof gameStat.Game !== "undefined" &&
      gameStat.Game.State === "Start"
    ) {
      this.canvas.width = gameStat.Game.BoardWidth;
      this.canvas.height = gameStat.Game.BoardHeight;
      return;
    }
    clearTimeout(this._spinnerInterval);
    if (
      typeof gameStat.Robots !== "undefined" &&
      gameStat.State === "NextTurn"
    ) {
      this._robots(gameStat.Robots);
      this._missiles(gameStat.Missiles);
      return;
    }
  }

  _clear() {
    this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
  }

  _robots(robots) {
    this.context.font = "16px Arial";
    this.context.fillStyle = "black";
    for (let index = 0; index < robots.length; index++) {
      const robot = robots[index];
      this.context.fillText(
        index + 1,
        Math.round(robot.X),
        Math.round(robot.Y)
      );
    }
  }

  _missiles(missiles) {
    for (let index = 0; index < missiles.length; index++) {
      const missile = missiles[index];
      this.context.fillStyle = "red";
      this.context.fillText("|", Math.round(missile.X), Math.round(missile.Y));
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
