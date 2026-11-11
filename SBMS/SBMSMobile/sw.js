// Data Fusion mobile service worker (PWA).
// Network only: live stock and orders must never come from a cache. Its one job is a clear
// "no connection" page instead of the browser's error page when a screen is opened offline.
// Scope = /SBMSMobile/ only - the desktop site is never touched.
self.addEventListener('install', function () { self.skipWaiting(); });
self.addEventListener('activate', function (e) { e.waitUntil(self.clients.claim()); });
self.addEventListener('fetch', function (e) {
    if (e.request.mode !== 'navigate') return;   // scripts, images, AJAX: browser default
    e.respondWith(fetch(e.request).catch(function () {
        return new Response(
            '<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">' +
            '<title>No connection</title>' +
            '<body style="font-family:sans-serif;text-align:center;padding:3em 1em;color:#333">' +
            '<h2>No connection</h2><p>Data Fusion needs a connection. Reconnect and try again.</p>' +
            '<button onclick="location.reload()" style="padding:.8em 1.6em;font-size:1em">Try again</button></body>',
            { headers: { 'Content-Type': 'text/html; charset=utf-8' } });
    }));
});
