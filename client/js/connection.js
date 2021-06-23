const connectBtn = document.getElementById("connectBtn");
const connectionStatus = document.getElementById("connectionStatus");

const connectionUrl = "ws://localhost:5000";
var socket;

connectBtn.onclick = function() {
    connectionStatus.innerHTML = "Status: Connecting";

    socket = new WebSocket(connectionUrl);

    socket.onopen = function(event) {
        updateState();
    };

    socket.onclose = function(event) {
        updateState();
    };

    socket.onmessage = function(event) {
        updateState();
    };

    socket.onerror = function(event) {
        updateState();
    };
}

function updateState() {
    console.log(socket.readyState);
    if(!socket) {
        connectionStatus.innerHTML = "Status: Disconnected";
    } else {
        switch(socket.readyState) {
            case WebSocket.CLOSED:
                connectionStatus.innerHTML = "Status: Closed";
                break;

            case WebSocket.CLOSING:
                connectionStatus.innerHTML = "Status: Closing...";
                break;

            case WebSocket.OPEN:
                connectionStatus.innerHTML = "Status: Connected";
                break;

            case WebSocket.CONNECTING:
                connectionStatus.innerHTML = "Status: Connecting";
                break;

            default:
                connectionStatus = "Status: Failed to connect. Error message: " + htmlEscape(socket.readyState);
                break;
        }
    }
}
