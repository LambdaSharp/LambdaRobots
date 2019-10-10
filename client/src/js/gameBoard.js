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
    this._clear();
    if (typeof gameStat.Game !== "undefined" && gameStat.State === "Start") {
      this.canvas.width = gameStat.Game.BoardWidth;
      this.canvas.height = gameStat.Game.BoardHeight;
      return;
    }
    if (
      typeof gameStat.Robots !== "undefined" &&
      gameStat.State === "NextTurn"
    ) {
      this._Robots(gameStat.Robots);
      this._Missiles(gameStat.Missiles);
      return;
    }
  }

  _clear() {
    this.context.clearRect(0, 0, this.canvas.width, this.canvas.height);
  }

  _Robots(robots) {
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

  _Missiles(missiles) {
    for (let index = 0; index < missiles.length; index++) {
      const missile = missiles[index];
      this.context.fillStyle = "red";
      this.context.fillText(
        "|",
        Math.round(missile.X),
        Math.round(missile.Y)
      );
    }
  }
}
