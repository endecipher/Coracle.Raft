"use strict";

var connectionRaft = new signalR.HubConnectionBuilder().withUrl("/raftHub").build();

function removeChildren(elementId) {
    let e = document.getElementById(elementId);

    let child = e.lastElementChild;

    while (child) {
        e.removeChild(child);
        child = e.lastElementChild;
    }
}

function createSpaceForNewEntry(elementId) {
    let e = document.getElementById(elementId);
    let clusterList = document.getElementById('coracleCluster');

    if (e.hasChildNodes() && clusterList.hasChildNodes()) {
        let clusterSize = clusterList.childElementCount;
        let currentListSize = e.childElementCount;

        // +2 because we want to create space for a new entry (+1) and the clusterSize would also include itself (+1)
        for (let i = 0; i < (currentListSize - clusterSize + 2); i++) {

            e.removeChild(e.children[i]);
        }
    }
}

connectionRaft.on("ReceiveEntries", function (log) {

    let propertyName = log.name;
    let value = log.value;

    if (propertyName == 'Term') {
        $('#coracleTerm').html(value);
    }

    if (propertyName == 'CommitIndex') {
        $('#coracleCommitIndex').html(value);
    }

    if (propertyName == 'LastApplied') {
        $('#coracleLastApplied').html(value);
    }

    if (propertyName == 'VotedFor') {
        $('#coracleVotedFor').html(value);
    }

    if (propertyName == 'Cluster') {

        removeChildren('coracleCluster');

        let arr = JSON.parse(value);

        for (let i = 0; i < arr.length; i++) {
            let objStr = JSON.stringify(arr[i]);

            let li = document.createElement("li");
            document.getElementById('coracleCluster').appendChild(li);
            li.textContent = `${objStr}`;
        };
    }

    if (propertyName == 'State') {
        $('#coracleState').html(value);
    }

    if (propertyName == 'NextIndices') {

        createSpaceForNewEntry('coracleNextIndices');

        let li = document.createElement("li");
        document.getElementById('coracleNextIndices').appendChild(li);
        li.textContent = `${value}`;
    }

    if (propertyName == 'MatchIndices') {

        createSpaceForNewEntry('coracleMatchIndices');

        let li = document.createElement("li");
        document.getElementById('coracleMatchIndices').appendChild(li);
        li.textContent = `${value}`;
    }

    if (propertyName == 'LogChain') {

        removeChildren('raftEntries');

        let arr = JSON.parse(value);

        for (let i = 0; i < arr.length; i++) {
            let objStr = JSON.stringify(arr[i]);

            let li = document.createElement("li");
            document.getElementById('raftEntries').appendChild(li);
            
            li.textContent = `${objStr}`;
        }
    }
});


connectionRaft.start().then(function () {
    console.log('RaftHub Connection Started');
}).catch(function (err) {
    return console.error(err.toString());
});
