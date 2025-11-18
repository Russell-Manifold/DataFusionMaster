function setupDragAndDrop() {
    const jobs = document.querySelectorAll('.job');
    const columns = document.querySelectorAll('.column');

    jobs.forEach(job => {
        job.addEventListener('dragstart', dragStart);
        job.setAttribute('draggable', 'true'); // Ensure draggable attribute is set
    });

    columns.forEach(column => {
        column.addEventListener('dragover', dragOver);
        column.addEventListener('drop', drop);
    });
}

function dragStart(event) {
    // Set data to be transferred
    event.dataTransfer.setData('text', event.target.id);
}

function dragOver(event) {
    event.preventDefault(); // Allow dropping
}

document.addEventListener('DOMContentLoaded', (event) => {
    setupDragAndDrop();
});

function openPickingSlip(Psid) {
    // Example: Open JobCard.aspx with jobId query parameter
    //window.location.href = '~/PickingSlip.aspx?docid=' + encodeURIComponent(Psid);
    //window.location.href = window.location.origin + '/PickingSlip.aspx?docid=' + encodeURIComponent(Psid);
    var basePath = '<%= ResolveUrl("~/") %>';  // This will resolve to /za/ in live environment
    window.location.href = basePath + 'PickingSlip.aspx?docid=' + encodeURIComponent(Psid);
    // Return false to prevent default behavior of the link button
    return false;
}

function closeModal() {
    $('#quantityModal').modal('hide'); // Close the modal
}

// Function to refresh the kanban board
function refreshKanbanBoard() {
    __doPostBack('<%= UpdatePanel1.ClientID %>', '');
}

// Handle modal close and cancel button
$('#quantityModal').on('hidden.bs.modal', function (e) {
    refreshKanbanBoard();
});
function drop(event) {
    event.preventDefault();
    const id = event.dataTransfer.getData('text');
    const job = document.getElementById(id);

    // Ensure we append to the column and not the header
    if (event.target.classList.contains('column')) {
        event.target.appendChild(job);
    } else if (event.target.parentElement.classList.contains('column')) {
        event.target.parentElement.appendChild(job);
    }

    // Get jobId, fromWsID, and newWsID
    const jobId = id.replace('job_', '');
    const newWsID = (event.target.classList.contains('column') ? event.target.id : event.target.parentElement.id).replace('column_', '');

    // Set hidden field values
    setHiddenJobId(jobId);
    setNewWsID(newWsID);

    // Show the modal
    $('#quantityModal').modal('show');

    // Optionally, call updateJobStatus(jobId, newWsID) if needed
}

function setHiddenJobId(jobId) {
    var hiddenField = document.getElementById('hiddenJobId');
    if (hiddenField) {
        hiddenField.value = jobId;
    } else {
        console.error('Hidden field for jobId not found.');
        // Handle the absence of hidden field if necessary
    }
}

function setNewWsID(newWsID) {
    var newWsField = document.getElementById('newWsID');
    if (newWsField) {
        newWsField.value = newWsID;
    } else {
        console.error('Hidden field for newWsID not found.');
        // Handle the absence of hidden field if necessary
    }
}

// Function to fetch updated data from the server
// Function to perform XMLHttpRequest (AJAX) POST request
function postRequest(url, data, callback) {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url, true);
    xhr.setRequestHeader('Content-Type', 'application/json; charset=UTF-8');
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            callback(JSON.parse(xhr.responseText));
        }
    };
    xhr.send(JSON.stringify(data));
}

// Function to update kanban board using native AJAX
function updateKanbanBoard() {
    postRequest('JobTracking.aspx/GetUpdatedData', {}, function (response) {
        // Clear existing kanban board content
        document.getElementById('kanbanboard').innerHTML = '';

        // Append updated jobs to kanban board
        response.d.forEach(function (item) {
            var jobDiv = document.createElement('div');
            jobDiv.className = 'job';
            jobDiv.id = 'job_' + item.id; // adjust based on your item id
            jobDiv.innerHTML = `
                <h3>${item.jobTitle}</h3>
                <p>${item.jobDescription}</p>
                <button onclick="openJobCard(${item.id})">View Job Card</button>
            `;
            document.getElementById('kanbanboard').appendChild(jobDiv);
        });
    });
}

// Update kanban board after job transaction is saved
function updateAfterSave() {
    updateKanbanBoard();
    closeModal(); // Close the modal (assuming you have this function implemented)
}



