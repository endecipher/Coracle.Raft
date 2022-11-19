var addNote = function (event) {
    console.log('adding');
    //event.preventDefault();
    //event.stopImmediatePropagation();

    fetch('/command/addnote', {
        method: 'post',
        body: JSON.stringify({ UniqueHeader: $("#noteHeader").val(), Text: $("#noteText").val() }),
        headers: { 'content-type': 'application/json' },
    })
        .then(function (response) {
            if (response.status !== 200) {
                console.log('fetch returned not ok' + response);
            }
            console.log('fetch returned ok');
            console.log(response.body);
            //response.body.getReader().read().then(function (val) {
            //    console.log(val);
            //    console.log(val.value.toString())
            //    $("#commandResult").val() = val.value.toString();
            //});
            response.
                text()
                .then(function (data) {
                console.log('fetch returned ok');
                console.log(data);
                $("#commandResult").text(data);
            });
        })
        .catch(function (err) {
            console.log(`error: ${err}`);
            $("#commandResult").val() = err;
        });
};