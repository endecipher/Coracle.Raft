var readyCoracle = function (e) {

    console.log('Starting and Initializing Coracle - Making things ready');

    fetch('/home/ready', {
        method: 'get',
    })
        .then(function (response) {
            if (response.status !== 200) {
                console.log('ReadyCoracle fetch returned not ok' + response);
            }
            else {
                $('#readyBtn').toggleClass('btn-secondary');
                $('#readyBtn').prop('disabled', true);
                console.log('ReadyCoracle fetch returned ok');
            }
        })
        .catch(function (err) {
            console.log(`error: ${err}`);
            $("#readyResult").val() = err;
        });
};