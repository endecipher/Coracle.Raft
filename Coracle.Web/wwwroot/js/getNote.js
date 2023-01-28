var getNote = function (e) {

    let header = $("#getNoteHeader").val();
    $("#getNoteResult").text(' ');
    $('#getNoteBtn').css('display', 'none');
    $('.getNoteSpinner').css('display', 'block');
    

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

        $('#getNoteBtn').css('display', 'block');
        $('.getNoteSpinner').css('display', 'none');

        $("#getNoteResult").text(resp);
    })
    .catch(function (err) {
        console.log(`GetNote fetch call had an error: ${err}`);

        $('#getNoteBtn').css('display', 'block');
        $('.getNoteSpinner').css('display', 'none');

        $("#getNoteResult").text(err);
    });
};