const express = require('express');
const expressWs = require('express-ws');

const app = express();

const wsInstance = expressWs(app); // Initialize express-ws

app.ws('/ws', (ws, req) => {
    console.log('Client connected');

    ws.on('message', function(msg) {
      // When a message is received, broadcast it to all connected clients
      wsInstance.getWss().clients.forEach(function each(client) {
        if (client.readyState === 1) {
          client.send(msg); // Send the message to each open client
        }
      });
    });

    ws.on('close', () => {
        console.log('Client disconnected');
    });

    ws.on('error', (error) => {
        console.error('WebSocket error:', error);
    });
});


const port = 3000;

app.use(express.static(__dirname)); // Serves index.html and others

app.listen(port, () => {
  console.log(`Server at http://localhost:${port}`);
});
