"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/underwritingHub").build();

//Disable the send button until connection is established.
//document.getElementById("sendButton").disabled = true;

connection.on("ManualApproval", function (manualApprovalRequired) {
    var data = JSON.parse(manualApprovalRequired);
    console.log(data);

    const allTds = document.querySelectorAll('td')
    const dateTd = Array.from(allTds).find(td => td.textContent === data.id);
    if (dateTd) {
        console.log("Decision already exists");
        return;
    }

    var tr = document.createElement("tr");
    var idCell = document.createElement("td");
    var decisionTypeCell = document.createElement("td");
    var responsibleCell = document.createElement("td");
    var reasonCell = document.createElement("td");
    var timestampCell = document.createElement("td");
    tr.appendChild(idCell);
    tr.appendChild(decisionTypeCell);
    tr.appendChild(responsibleCell);
    tr.appendChild(reasonCell);
    tr.appendChild(timestampCell);

    idCell.appendChild(document.createTextNode(data.id));
    decisionTypeCell.appendChild(document.createTextNode(data.decisionType));
    responsibleCell.appendChild(document.createTextNode(data.responsible));
    reasonCell.appendChild(document.createTextNode(data.reason));
    timestampCell.appendChild(document.createTextNode(data.timestamp));

    document.getElementById("tableBody").appendChild(tr);
});

connection.start().then(function () {
    console.log("connection started.");
    //document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var user = document.getElementById("userInput").value;
//    var message = document.getElementById("messageInput").value;
//    connection.invoke("SendMessage", user, message).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});