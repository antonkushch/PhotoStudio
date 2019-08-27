function isSyncNeeded() {
    var url = "/Account/CheckSyncWithDropbox";

    $.ajax({
        type: "GET",
        url: url,
        success: function (data) {
            if (data.syncNeeded) {
                var sync = "Synchronization needed.";
                var execTime = data.execTime;
                $("#syncNeeded").text("Sync Dropbox ***");                
            }
            else {
                var sync = "Synchronization not needed.";
                var execTime = data.execTime;
                $("#syncNeeded").text("Sync Dropbox");
            }
        }
    });
}

$(function () {
    isSyncNeeded();
    window.setInterval("isSyncNeeded()", 10000);
});