"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/logHub").build();

connection.on("ReceiveLog", function (log) {
    var li = document.createElement("li");
    document.getElementById("logList").appendChild(li);
    li.textContent = `${log}`;
});

connection.start().then(function () {
    console.log('LogHub Connection Started');
}).catch(function (err) {
    return console.error(err.toString());
});
