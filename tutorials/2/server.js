const express = require('express');
const app = express();
const port = 3000;

app.use(express.static(__dirname)); // Serves index.html and others

app.listen(port, () => {
  console.log(`Server at http://localhost:${port}`);
});
