var addNote = function (e) {

    let header = $("#addNoteHeader").val();
    let body = $("#addNoteText").val();

    $("#addNoteResult").text(' ');

    $('#addNoteBtn').css('display', 'none');
    
    $('.addNoteSpinner').css('display', 'block');

    console.log('AddNote with header ' + header + ' and body ' + body);

    fetch('/command/addnote', {
        method: 'post',
        body: JSON.stringify({ UniqueHeader: header, Text: body }),
        headers: { 'content-type': 'application/json' },
    })
        .then(function (response) {

            response.text().then(function (str)
            {
                $("#addNoteResult").text(str);
                console.log('AddNote fetch response returned data: ' + str);

            }).catch(function (err)
            {
                console.log('AddNote response returned error: ' + err);
            });

            $('#addNoteBtn').css('display', 'block');
            $('.addNoteSpinner').css('display', 'none');

            $("#addNoteResult").text(resp);
        })
        .catch(function (err) {
            console.log(`AddNote fetch call had an error: ${err}`);

            $('#addNoteBtn').css('display', 'block');
            $('.addNoteSpinner').css('display', 'none');

            $("#addNoteResult").text(err);
        });
};
