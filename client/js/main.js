const userInputDivHtmlCopy = document.getElementById("usernameInput").innerHTML;
const connectionUrl = "ws://localhost:5000";

var socket;
var connectionId;
var username;

init();

function init() {

    initUsernameInputField();
    initConnectButton();
    updateState();
}

function sendUsername() {

    if(!socket || socket.readyState == WebSocket.OPEN) {
        
        username = document.getElementById('userName').value;
        socket.send(JSON.stringify({
            Username: username
        }));
    }   
}

function initConnectButton() {

    var connectBtn = document.getElementById("connectBtn");
    connectBtn.onclick = function() {

        document.getElementById('connectionStatus').innerHTML = "Status: Connecting";

        socket = new WebSocket(connectionUrl);
    
        socket.onopen = function(event) {

            sendUsername();
            updateState();
        };
    
        socket.onclose = function(event) {

            updateState();
        };
    
        socket.onmessage = function(event) {
    
            const receivedData = JSON.parse(event.data);
            // console.log("Received: ", event.data);
            if(receivedData["ConnectionId"]) {

                connectionId = receivedData["ConnectionId"];
                updateState();
            } else if(receivedData['Message']) {

                var card = document.createElement('div');
                card.setAttribute('class', 'card');
    
                var cardBody = document.createElement('div');
                cardBody.setAttribute('class', 'card-body');
    
                var user = document.createElement('h6');
                var userMessage = document.createElement('p');
    
                if(username == receivedData['Username']) {

                    user.setAttribute('class', 'card-subtitle mb-2 text-muted text-left')
                    userMessage.setAttribute('class', 'card-text float-left text-justify');
                } else {
                    if(receivedData['Personal']) {
                        user.setAttribute('class', 'card-subtitle mb-2 text-right font-weight-bold');
                        user.innerText = receivedData['Username'] + ' (private)';

                    } else {
                        user.setAttribute('class', 'card-subtitle mb-2 text-muted text-right');
                        user.innerText = receivedData['Username'];
                    }
                    userMessage.setAttribute('class', 'card-text float-right text-justify');
                }
    
                userMessage.innerText = receivedData['Message'];
    
                cardBody.appendChild(user);
                cardBody.appendChild(userMessage);
    
                card.appendChild(cardBody);
                
                var chatField = document.getElementById('chatField');
                chatField.appendChild(card);

                // scroll to new message
                chatField.scrollTop = chatField.scrollTopMax;
            }
        };
    
        socket.onerror = function(event) {
            updateState();
        };
    }
}



function updateState() {

    var connectionStatus = document.getElementById('connectionStatus');

    if(!socket) {

        connectionStatus.innerHTML = "Status: Disconnected";
        document.getElementById('chatWindow').setAttribute('class', 'disable');
        document.getElementById('chatInput').setAttribute('class', 'disable');

    } else {

        var idText = document.getElementById('connectionId');
        
        switch(socket.readyState) {
            case WebSocket.CLOSED:
                connectionStatus.innerHTML = "Status: Closed";
                document.getElementById("usernameInput").innerHTML = userInputDivHtmlCopy;
                idText.innerHTML = 'Id: N/a';
                //deconstructChatWindow();
                break;

            case WebSocket.CLOSING:
                connectionStatus.innerHTML = "Status: Closing...";
                idText.innerHTML = 'Id: N/a';
                break;

            case WebSocket.OPEN:
                connectionStatus.innerHTML = "Status: Connected";

                if(connectionId) {
                    idText.innerHTML = 'Id: ' + connectionId;
                    document.getElementById("usernameInput").innerHTML = '';
                    constructChatWindow();
                } else {
                    idText.innerHTML = "Id: Failed to receive an Id";
                    //deconstructChatWindow();
                }
                break;

            case WebSocket.CONNECTING:
                connectionStatus.innerHTML = "Status: Connecting";
                idText.innerHTML = 'Id: N/a';
                break;

            default:
                connectionStatus = "Status: Failed to connect. Error message: " + htmlEscape(socket.readyState);
                idText.innerHTML = 'Id: N/a';
                document.getElementById("usernameInput").innerHTML = userInputDivHtmlCopy;
                break;
        }
    }
}

function htmlEscape(str) {

    return str.toString()
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function constructChatWindow() {

    document.getElementById('messageInputField').value = '';

    document.getElementById('chatWindow').setAttribute('class', 'text-window');

    var chatInput = document.getElementById('chatInput');
    chatInput.setAttribute('class', 'input-group');

    var sendButton = document.getElementById('sendButton');
    sendButton.onclick = sendMessage;

    chatInput.addEventListener('keyup', function(event) {

        if(event.keyCode == 13) {

            sendButton.click();
        }
    });
    chatInput.focus();
}

function sendMessage(event) {

    if(!socket || socket.readyState != WebSocket.OPEN) {

        alert("Error: You're not connected.");
    }

    var recipient;
    var message = document.getElementById('messageInputField').value;
    // check if message has @
    if(message[0] == '@') {
        var split = message.split(' ');

        recipient = split[0].substring(1, split[0].length);
        message = message.substring(recipient.length + 1);
    }

    var data = constructJSONPayload(recipient, message);
    socket.send(data);
    
    if(recipient) {
        // render privately sent message
    }

    // clear message input field
    document.getElementById('messageInputField').value = '';
}

function constructJSONPayload(recipient, message) {
    
    var temp = JSON.stringify({
        "From" : connectionId,
        "To" : recipient,
        "Message" : message,
        "Username" : username
    });
    console.log(temp);

    return JSON.stringify({
        "From" : connectionId,
        "To" : recipient,
        "Message" : message,
        "Username" : username
    });
}



function initUsernameInputField() {

    var usernameInputField = document.getElementById('userName');

    usernameInputField.addEventListener('keyup', function(event) {

        if(event.keyCode == 13) {
    
            connectBtn.click();
        }
    })
}