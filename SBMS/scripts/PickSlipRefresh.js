function refreshKanbanBoard() {
    PageMethods.GetUpdatedKanbanBoard(onSuccess, onError);
}

function onSuccess(result) {
    document.getElementById(kanbanboardClientID).innerHTML = result;
}

function onError(error) {
    console.error(error);
}

// Set interval to refresh every 60 seconds (60000 milliseconds)
setInterval(refreshKanbanBoard, 10000);

// Initial load
window.onload = refreshKanbanBoard;