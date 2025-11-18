async function fetchNewNotifications() {
    try {
        // Call to GetNewSalesOrders
        await $.ajax({
            type: "POST",
            url: "Notifications.aspx/GetNewSalesOrders",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        });

        // Call to GetNewNotifications
        const response = await $.ajax({
            type: "POST",
            url: "Notifications.aspx/GetNewNotifications",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        });

        if (response.d && Array.isArray(response.d)) {
            var notifications = response.d;
            for (var i = 0; i < notifications.length; i++) {
                displayNotification(notifications[i].Id, notifications[i].Message);
            }
        } else {
            console.error("Invalid response format: ", response);
        }
    } catch (error) {
        console.error("Error in fetchNewNotifications: ", error);
    }
}

function displayNotification(id, message) {
    toastr.options = {
        closeButton: true,
        debug: false,
        newestOnTop: false,
        progressBar: true,
        positionClass: "toast-top-right",
        preventDuplicates: false,
        onclick: function () {
            markMessageAsRead(id);
        },
        showDuration: "5000",
        hideDuration: "1000",
        timeOut: "5000",
        extendedTimeOut: "1000",
        showEasing: "swing",
        hideEasing: "linear",
        showMethod: "fadeIn",
        hideMethod: "fadeOut"
    };
    toastr.success(message, "Notification");
}

function markMessageAsRead(messageId) {
    $.ajax({
        type: "POST",
        url: "Notifications.aspx/MarkMessageAsRead",
        data: JSON.stringify({ messageId: messageId }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function () {
            console.log("Message marked as read: " + messageId);
        },
        error: function (xhr, status, error) {
            console.error("Error marking message as read: ", status, error);
            console.error("Response text: ", xhr.responseText);
        }
    });
}

// Set the interval to fetch new notifications every 60 seconds (60000 ms)
setInterval(fetchNewNotifications, 60000);

// Perform an initial check when the page loads
$(document).ready(function () {
    fetchNewNotifications(); // Initial check
});
