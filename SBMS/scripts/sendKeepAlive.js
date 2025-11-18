// scripts/shared.js

// Function to send keep-alive requests to the server
function sendKeepAlive() {
    setInterval(function () {
        $.ajax({
            url: '/KeepAlive.ashx',
            success: function (data) {
                console.log('Keep-alive sent');
            }
        });
    }, 60000); // Send every minute
}

// Call the sendKeepAlive function
sendKeepAlive();