var readyCoracle = function (e) {

    console.log('Starting and Initializing Coracle - Making things ready');

    $('#nodeIdDisplay').text('');
    $('.coracleSpinner').css('display', 'block');

    fetch('/home/ready', {
        method: 'get',
    })
        .then(function (response) {
            if (response.status !== 200) {
                console.log('ReadyCoracle fetch returned not ok' + response);
                $('#nodeIdDisplay').text(`Please retry as something went wrong`);
                $('.coracleSpinner').css('display', 'none');
            }
            else {

                response.text()
                    .then(function (data) {
                        $('.raftStateMachine').css('display', 'none');
                        $('#nodeIdDisplay').html(`<strong>${data}</strong>`);
                        $('#nodeIdDisplay').css('color', 'green');
                        $('.coracleSpinner').css('display', 'none');
                        $('.noteSection').css('display', 'block');
                        console.log('ReadyCoracle fetch returned ok. NodeId: ' + data);
                    })
                    .catch(function (err){
                        console.log('ReadyCoracle fetch returned ok. But response could not be text parsed: ' + err);
                        $('#nodeIdDisplay').text(`Please retry as ${err}`);
                        $('.coracleSpinner').css('display', 'none');
                    })
            }
        })
        .catch(function (err) {
            console.log(`error: ${err}`);
            $('#nodeIdDisplay').text(`Please retry as ${err}`);
            $('.coracleSpinner').css('display', 'none');
        });
};