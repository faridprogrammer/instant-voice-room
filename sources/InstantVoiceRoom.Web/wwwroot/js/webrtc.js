"use strict";

let localStream;
let peers = {};        // { connectionId: RTCPeerConnection }
let connection;        // SignalR HubConnection

const leaveBtn = document.getElementById('leaveBtn');
const nameInput = document.getElementById('nameInput');
const statusEl  = document.getElementById('status');
const userList  = document.getElementById('userList');
const stunServer = document.getElementById('stunServer');

leaveBtn.addEventListener('click', leaveMeeting);

async function startMeeting() {
  const name = nameInput.value.trim();
  if (!name) { alert('Please enter your name.'); return; }

  nameInput.disabled = true;
  statusEl.textContent = 'Acquiring microphone…';

  // 1) Get audio
  try {
    localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
  } catch (err) {
    alert('Microphone access denied.');
    resetUI();
    return;
  }

  // 2) Setup SignalR
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/signalHub')
    .build();

  connection.on('UsersUpdated', updateUserList);
  connection.on('ReceiveSignal', onSignal);

  statusEl.textContent = 'Connecting to server…';
  await connection.start();

  // 3) Join
  await connection.invoke('Join', name);
  statusEl.textContent = 'Joined — exchanging audio…';
  leaveBtn.disabled = false;

  // 4) Delay, then offer to any existing peers
  setTimeout(initOffers, 500);
}

async function leaveMeeting() {
  leaveBtn.disabled = true;
  statusEl.textContent = 'Leaving…';

  // Tell server we’re leaving
  if (connection) {
    try { await connection.invoke('Leave'); }
    catch {}
    connection.stop();
  }

  // Tear down all peer connections
  for (let pc of Object.values(peers)) {
    pc.close();
  }
  peers = {};
  userList.innerHTML = '';
  resetUI();
  statusEl.textContent = 'Not connected';
}

function resetUI() {
  joinBtn.disabled = false;
  nameInput.disabled = false;
}

function updateUserList(names) {
  userList.innerHTML = '';
  names.forEach(n => {
    const li = document.createElement('li');
    li.textContent = n;
    userList.appendChild(li);
  });
}

async function initOffers() {
  for (const [id, pc] of Object.entries(peers)) {
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    connection.invoke('SendSignal', 'offer', JSON.stringify(offer));
  }
}

async function onSignal(fromId, type, data) {
  // 1) Ensure a peer connection exists
  let pc = peers[fromId];
  if (!pc) {
    pc = new RTCPeerConnection({
      iceServers: [{ urls: stunServer.value }]
    });

    // add our mic
    localStream.getTracks().forEach(t => pc.addTrack(t, localStream));

    // play incoming audio
    pc.ontrack = ev => {
      const audio = document.createElement('audio');
      audio.srcObject = ev.streams[0];
      audio.autoplay = true;
      document.body.appendChild(audio);
    };

    // send ICE candidates to server
    pc.onicecandidate = evt => {
      if (evt.candidate) {
        connection.invoke('SendSignal', 'ice', JSON.stringify(evt.candidate));
      }
    };

    peers[fromId] = pc;
  }

  const payload = JSON.parse(data);
  if (type === 'offer') {
    await pc.setRemoteDescription(payload);
    const ans = await pc.createAnswer();
    await pc.setLocalDescription(ans);
    connection.invoke('SendSignal', 'answer', JSON.stringify(pc.localDescription));
  }
  else if (type === 'answer') {
    await pc.setRemoteDescription(payload);
  }
  else if (type === 'ice') {
    await pc.addIceCandidate(payload);
  }
}

