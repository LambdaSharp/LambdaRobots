export default class WebSocketClient {
  constructor(wss, element, autoReconnectInterval, onMessage) {
    this.autoReconnectInterval = autoReconnectInterval;
    this.wss = wss;
    this.output = element;
    this.websocket = new WebSocket(wss);
    const self = this;
    this.websocket.onopen = function(evt) {
      self._onOpen(evt);
    };
    this.websocket.onclose = function(evt) {
      self._onClose(evt);
    };
    this.websocket.onmessage = function(evt) {
      self._onMessage(evt, onMessage);
    };
    this.websocket.onerror = function(evt) {
      self._onError(evt);
    };
  }

  doSend(message) {
    if (this.websocket.readyState !== WebSocket.OPEN) {
      setTimeout(() => {
        this.doSend(message);
      }, 500);
    } else {
      this._writeToScreen("SENT: " + message);
      this.websocket.send(message);
    }
  }

  _onOpen(evt) {
    this._writeToScreen("CONNECTED");
  }

  _onClose(evt) {
    switch (evt.code) {
      case 1000: // CLOSE_NORMAL
        this._writeToScreen("DISCONNECTED");
        break;
      default:
        // Abnormal closure
        this._reconnect(evt);
        break;
    }
    this.onclose(evt);
  }

  _onMessage(evt, onMessage) {
    this._writeToScreen(
      '<span style="color: blue;">RESPONSE: ' + evt.data + "</span>"
    );
    try {
      if (onMessage) {
        const jsonResult = JSON.parse(evt.data);
        onMessage(jsonResult);
      }
    } catch (error) {
      console.warn("Could not run function with parsed JSON data");
    }
  }

  _onError(evt) {
    this._writeToScreen('<span style="color: red;">ERROR:</span> ' + evt.data);
  }

  _reconnect = function(evt) {
    this._writeToScreen(
      `WebSocketClient: retry in ${this.autoReconnectInterval}ms`,
      evt
    );
    var that = this;
    setTimeout(function() {
      console.log("WebSocketClient: reconnecting...");
      that.open(that.wss);
    }, this.autoReconnectInterval);
  };

  _writeToScreen(message) {
    var pre = document.createElement("p");
    pre.style.wordWrap = "break-word";
    pre.innerHTML = message;
    this.output.appendChild(pre);
  }
}
