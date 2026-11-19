using Litoral.Acars.Core.Map;

namespace Litoral.Acars.Map;

public static class LiveMapPage
{
    public static string Build(string? cartoKey) => Html
        .Replace("__LIGHT__", MapTiles.Light(cartoKey))
        .Replace("__DARK__", MapTiles.Dark(cartoKey))
        .Replace("__ATTRIBUTION__", MapTiles.AttributionFor(cartoKey));

    private const string Html = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
        <style>
          html, body, #map { height: 100%; margin: 0; }
          body { font-family: 'Segoe UI', sans-serif; background: #FDFCF7; }
          body.dark { background: #0A1F3C; }
          .plane { width: 28px; height: 28px; display: flex; align-items: center; justify-content: center; }
          .plane svg { width: 24px; height: 24px; filter: drop-shadow(0 1px 2px rgba(0,0,0,.45)); }
          .tag { position: absolute; left: 26px; top: 4px; white-space: nowrap; font: 600 11px 'Segoe UI'; padding: 1px 7px;
                 border-radius: 8px; background: #0A1F3C; color: #fff; }
          .me .tag { background: #EF476F; }
          .leaflet-popup-content { font: 13px 'Segoe UI'; margin: 10px 12px; color: #0A1F3C; }
          .pop b { font-size: 14px; }
          .muted { color: #4B5B73; }
          .bar { height: 4px; background: #D9E3F0; border-radius: 2px; margin: 6px 0; }
          .bar i { display: block; height: 4px; background: #2EC4B6; border-radius: 2px; }
          .empty { position: absolute; top: 12px; left: 50%; transform: translateX(-50%); z-index: 999; background: #fff; color: #0A1F3C;
                   padding: 6px 14px; border-radius: 16px; font: 600 12px 'Segoe UI'; box-shadow: 0 1px 4px rgba(0,0,0,.2); display: none; }
          body.dark .empty { background: #0F2A4F; color: #FDFCF7; }
        </style>
        </head>
        <body>
        <div id="map"></div>
        <div id="empty" class="empty">No Litoral flights in progress</div>
        <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
        <script>
          const tiles = {
            light: '__LIGHT__',
            dark: '__DARK__'
          };
          const map = L.map('map', { worldCopyJump: true }).setView([-15, -50], 4);
          const flightsLayer = L.layerGroup().addTo(map);
          const routeLayer = L.layerGroup().addTo(map);
          let tileLayer = null, dark = false, fitted = false, selected = null;

          function setTheme(value) {
            dark = value;
            document.body.classList.toggle('dark', dark);
            if (tileLayer) map.removeLayer(tileLayer);
            tileLayer = L.tileLayer(dark ? tiles.dark : tiles.light, {
              maxZoom: 18,
              attribution: '__ATTRIBUTION__'
            }).addTo(map);
            if (selected) showRoute(selected);
          }

          function esc(text) {
            return String(text ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
          }

          function plane(color) {
            return `<svg viewBox="0 0 24 24"><path fill="${color}" stroke="#fff" stroke-width="1" d="M12 2c.8 0 1.4.7 1.4 1.6v5.6l7.6 4.6v2l-7.6-2.3v4.6l2.2 1.6V21L12 20l-3.6 1v-1.3l2.2-1.6v-4.6L3 15.8v-2l7.6-4.6V3.6C10.6 2.7 11.2 2 12 2z"/></svg>`;
          }

          function showRoute(f) {
            selected = f;
            routeLayer.clearLayers();
            L.polyline([[f.dep.lat, f.dep.lon], [f.lat, f.lon], [f.arr.lat, f.arr.lon]],
              { color: dark ? '#7FB1F3' : '#1D6FD8', weight: 2, dashArray: '6 6', opacity: 0.85 }).addTo(routeLayer);
            [f.dep, f.arr].forEach((a, i) =>
              L.circleMarker([a.lat, a.lon], { radius: 5, color: i ? '#EF476F' : '#2EC4B6', fillOpacity: 1 })
                .bindTooltip(a.icao, { permanent: true, direction: 'top', offset: [0, -4] })
                .addTo(routeLayer));
          }

          function render(message) {
            flightsLayer.clearLayers();
            const list = message.flights || [];
            document.getElementById('empty').style.display = list.length ? 'none' : 'block';

            list.forEach(f => {
              const me = message.me && f.callsign === message.me;
              const color = me ? '#EF476F' : (f.cargo ? '#E09B1A' : '#1D6FD8');
              const icon = L.divIcon({
                className: me ? 'me' : '',
                iconSize: [28, 28],
                iconAnchor: [14, 14],
                html: `<div class="plane" style="transform: rotate(${f.hdg}deg)">${plane(color)}</div><div class="tag">${esc(f.callsign)}</div>`
              });
              const eta = f.eta != null ? ` · ETA ${f.eta} min` : '';
              L.marker([f.lat, f.lon], { icon, zIndexOffset: me ? 1000 : 0 })
                .bindPopup(`<div class="pop"><b>${esc(f.callsign)}</b> <span class="muted">${esc(f.network)}</span><br>` +
                  `${esc(f.dep.icao)} → ${esc(f.arr.icao)}<div class="bar"><i style="width:${f.progress}%"></i></div>` +
                  `${esc(f.pilot)}<br>${esc(f.aircraft)}<br>` +
                  `<span class="muted">${esc(f.phase)} · FL${Math.round(f.alt / 100)} · ${f.gs} kt${eta}</span></div>`)
                .on('click', () => showRoute(f))
                .addTo(flightsLayer);
            });

            const mine = list.find(f => message.me && f.callsign === message.me);
            const current = selected ? list.find(f => f.callsign === selected.callsign) : mine;
            if (current) showRoute(current); else { selected = null; routeLayer.clearLayers(); }

            if (!fitted && list.length) {
              fitted = true;
              if (mine) map.setView([mine.lat, mine.lon], 6);
              else if (list.length === 1) map.setView([list[0].lat, list[0].lon], 6);
              else map.fitBounds(list.map(f => [f.lat, f.lon]), { padding: [40, 40] });
            }
          }

          setTheme(false);
          window.chrome.webview.addEventListener('message', e => {
            const message = e.data;
            if (message.type === 'theme') setTheme(message.dark);
            else if (message.type === 'flights') render(message);
          });
        </script>
        </body>
        </html>
        """;
}