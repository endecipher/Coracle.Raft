"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/logHub").build();

connection.on("ReceiveLog", function (log) {
    var li = document.createElement("li");
    document.getElementById("logList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `${log}`;
});

connection.start().then(function () {
    // document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var log = document.getElementById("logInput").value;
//    connection.invoke("SendLog", log).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});