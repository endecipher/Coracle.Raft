"use strict";

var connectionRaft = new signalR.HubConnectionBuilder().withUrl("/raftHub").build();

connectionRaft.on("ReceiveEntries", function (log) {
    var li = document.createElement("li");
    document.getElementById("raftEntries").appendChild(li);
    li.textContent = `${log}`;
});

connectionRaft.start().then(function () {
    console.log('Connection Started');
}).catch(function (err) {
    return console.error(err.toString());
});
