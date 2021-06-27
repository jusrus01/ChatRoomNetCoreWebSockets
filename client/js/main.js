const userInputDivHtmlCopy = document.getElementById("usernameInput").innerHTML;
const connectionUrl = "ws://localhost:5000";
const maxMessageCount = 50;

var removeLastMessage = false;
var socket;
var connectionId;
var username;
var messageCount;
var nextMessage;

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

            if(receivedData["ConnectionId"]) {
                connectionId = receivedData["ConnectionId"];
                updateState();

            } else if(receivedData['Message']) {
                if(receivedData['Personal']) {
                    renderMessage(receivedData['Username'], receivedData['Message'],
                        true, false);
                } else if(receivedData['Username'] == username) {
                    renderMessage(username, receivedData['Message'], false, true);
                } else {
                    renderMessage(receivedData['Username'], receivedData['Message'],
                        false, false);
                }
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

                socket = null;
                initConnectButton();
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

                socket = null;
                initConnectButton();
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
        renderMessage(recipient, message, true, true);
    }

    // clear message input field
    document.getElementById('messageInputField').value = '';
}

function constructJSONPayload(recipient, message) {

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

function removeFirstMessage() {

    const chatField = document.getElementById('chatField');
    const childNodes = chatField.childNodes;

    if(childNodes.length <= 1 || childNodes == null) {

        console.error("removeFirstMessage(): There aren't any available messages for removal.");
        return;
    }

    if(nextMessage == null) {
        const node = childNodes[0];
        nextMessage = node.nextSibling;
        chatField.removeChild(node);
    } else {

        const temp = nextMessage;
        if(nextMessage.nextSibling != null) {

            nextMessage = nextMessage.nextSibling;
        }

        chatField.removeChild(temp);
    }
}

function renderMessage(recipient, message, privateMessage, renderToSelf) {

    if(!removeLastMessage) {

        if(messageCount == null) {
            messageCount = 0;
        } else {
            messageCount++;
        }

        if(messageCount >= maxMessageCount) {
            removeLastMessage = true;
            removeFirstMessage();
        }
    } else {
        removeFirstMessage();
    }

    var card = document.createElement('div');
    card.setAttribute('class', 'card');
    
    var cardBody = document.createElement('div');
    cardBody.setAttribute('class', 'card-body');
    
    var user = document.createElement('h6');
    var userMessage = document.createElement('p');

    if(renderToSelf && privateMessage) {
        user.setAttribute('class', 'card-subtitle mb-2 font-weight-bold text-left');
        userMessage.setAttribute('class', 'card-text float-left text-justify text-break');
        user.innerText ='From Me To ' + recipient + ' (private)';
    } else if(renderToSelf) {
        user.setAttribute('class', 'card-subtitle mb-2 text-muted text-left');
        userMessage.setAttribute('class', 'card-text float-left text-justify text-break');
        user.innerText = username;
    } else if(privateMessage) {
        user.setAttribute('class', 'card-subtitle mb-2 font-weight-bold text-right');
        userMessage.setAttribute('class', 'card-text float-right text-justify text-break');
        user.innerText = 'From ' + recipient + ' To Me (private)';
    } else {
        user.setAttribute('class', 'card-subtitle mb-2 text-muted text-right');
        userMessage.setAttribute('class', 'card-text float-right text-justify text-break');
        user.innerText = recipient;
    }

    userMessage.innerText = message;

    cardBody.appendChild(user);
    cardBody.appendChild(userMessage);

    card.appendChild(cardBody);

    var chat = chatField = document.getElementById('chatField');
    chat.appendChild(card);
    // scroll to new message
    chatField.scrollTop = chatField.scrollTopMax;
}