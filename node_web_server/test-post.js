'use strict'
 
const express = require('express')
const bodyParser = require('body-parser')
 
// Create a new instance of express
const app = express()
 
// Tell express to use the body-parser middleware and to not parse extended bodies
app.use(bodyParser.urlencoded({ extended: false }))
 
// Route that receives a POST request to /sms
app.post('/workitem', function (req, res) {
  const body = req.body.Body
  res.set('Content-Type', 'text/plain')
  res.send(`You sent: ${body} to TFS Cookbook`)
})
 
// Tell our app to listen on port 8080
app.listen(8080, function (err) {
  if (err) {
    throw err
  }
 
  console.log('Server started on port 8080')
})