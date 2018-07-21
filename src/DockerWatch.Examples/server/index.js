const express = require('express');
const app = express();

app.get('/', (_, res) => res.send('Welcome.'));

app.listen(8080, '0.0.0.0', () => console.log('Server running on 0.0.0.0:8080.'));
