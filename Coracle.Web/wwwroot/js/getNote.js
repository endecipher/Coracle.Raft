var getNote = function (e) {

    let header = $("#getNoteHeader").val();

    console.log('Getting Note with header ' + header);

    let url = '/command/getNote?' + new URLSearchParams({
        noteHeader: header,
    });

    fetch(url, {
        method: 'get',
    })
    .then(function (response) {
        response.text().then(function (str) {
            $("#getNoteResult").text(str);
            console.log('GetNote fetch response returned data: ' + str);

        }).catch(function (err) {
            console.log('GetNote response returned error: ' + err);
        });

        $("#getNoteResult").text(resp);
    })
    .catch(function (err) {
        console.log(`GetNote fetch call had an error: ${err}`);
        $("#getNoteResult").val() = err;
    });
};