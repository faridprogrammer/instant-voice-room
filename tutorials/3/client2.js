var websocketConnection = new WebSocket('ws://localhost:3000/ws'); 
var name = ""; 
 
var loginInput = document.querySelector('#loginInput'); 
var loginBtn = document.querySelector('#loginBtn'); 
var otherUsernameInput = document.querySelector('#otherUsernameInput'); 
var connectToOtherUsernameBtn = document.querySelector('#connectToOtherUsernameBtn'); 
var connectedUser, myConnection;
  
//when a user clicks the login button 
loginBtn.addEventListener("click", function(event){ 
   name = loginInput.value; 
	
   if(name.length > 0){ 
      send({ 
         type: "login", 
         name: name 
      }); 
   } 
	
});
  
//handle messages from the server 
websocketConnection.onmessage = function (message) { 
   console.log(`In name ${name} Got message`, message.data);
   var data = JSON.parse(message.data); 
	
   switch(data.type) { 
      case "login": 
         onLogin(data.success); 
         break; 
      case "offer": 
         onOffer(data.offer, data.name); 
         break; 
      case "answer": 
         onAnswer(data.answer); 
         break; 
      case "candidate": 
         onCandidate(data.candidate); 
         break; 
      default: 
         break; 
   } 
};
  
//when a user logs in 
function onLogin(success) { 

   if (success === false) { 
      alert("oops...try a different username"); 
   } else { 
      //creating our RTCPeerConnection object 
		
      var configuration = { 
         "iceServers": [{ "url": "stun:stun.1.google.com:19302" }] 
      }; 
		
      myConnection = new webkitRTCPeerConnection(configuration); 
      console.log("RTCPeerConnection object was created"); 
      console.log(myConnection); 
  
      //setup ice handling
      //when the browser finds an ice candidate we send it to another peer 
      myConnection.onicecandidate = function (event) { 
		
         if (event.candidate) { 
            send({ 
               type: "candidate", 
               candidate: event.candidate 
            }); 
         } 
      }; 
   } 
};
  
websocketConnection.onopen = function () { 
   console.log("Connected"); 
};
  
websocketConnection.onerror = function (err) { 
   console.log("Got error", err); 
};
  
// Alias for sending messages in JSON format 
function send(message) { 

   if (connectedUser) { 
      message.name = connectedUser; 
   } 
	
   websocketConnection.send(JSON.stringify(message)); 
};


//setup a peer connection with another user 
connectToOtherUsernameBtn.addEventListener("click", function () { 
 
   var otherUsername = otherUsernameInput.value; 
   connectedUser = otherUsername;
	
   if (otherUsername.length > 0) { 
      //make an offer 
      myConnection.createOffer(function (offer) { 
         console.log(); 
         send({ 
            type: "offer", 
            offer: offer 
         });
			
         myConnection.setLocalDescription(offer); 
      }, function (error) { 
         alert("An error has occurred."); 
      }); 
   } 
}); 
 
//when somebody wants to call us 
function onOffer(offer, name) { 
   console.log(`In name ${name} offer: ${offer} name: ${name}`)
   connectedUser = name; 
   myConnection.setRemoteDescription(new RTCSessionDescription(offer)); 
	
   myConnection.createAnswer(function (answer) { 
      myConnection.setLocalDescription(answer); 
		
      send({ 
         type: "answer", 
         answer: answer 
      }); 
		
   }, function (error) { 
      alert("oops...error"); 
   }); 
}
  
//when another user answers to our offer 
function onAnswer(answer) { 
   myConnection.setRemoteDescription(new RTCSessionDescription(answer)); 
} 
 
//when we got ice candidate from another user 
function onCandidate(candidate) { 
   myConnection.addIceCandidate(new RTCIceCandidate(candidate)); 
}	