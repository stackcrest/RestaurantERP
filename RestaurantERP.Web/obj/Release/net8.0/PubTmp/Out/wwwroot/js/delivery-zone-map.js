(function () {
    const latInput = document.getElementById('latitude');
    const lngInput = document.getElementById('longitude');
    const radiusInput = document.getElementById('radiusKm');
    const radiusSlider = document.getElementById('radiusSlider');
    const radiusLabel = document.getElementById('radiusLabel');
    const addressInput = document.getElementById('restaurantAddress');
    const searchInput = document.getElementById('mapSearch');
    const searchBtn = document.getElementById('mapSearchBtn');
    const mapEl = document.getElementById('deliveryMap');
    const modeLocationBtn = document.getElementById('mapModeLocation');
    const modeRadiusBtn = document.getElementById('mapModeRadius');
    const mapModeHint = document.getElementById('mapModeHint');

    if (!mapEl || typeof L === 'undefined') return;

    let lat = parseFloat(latInput?.value) || 28.6139;
    let lng = parseFloat(lngInput?.value) || 77.2090;
    let radiusKm = parseFloat(radiusInput?.value) || 5;
    let mapMode = 'location';

    const map = L.map('deliveryMap', { scrollWheelZoom: true }).setView([lat, lng], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    const marker = L.marker([lat, lng], { draggable: true, autoPan: true }).addTo(map);

    const circle = L.circle([lat, lng], {
        radius: radiusKm * 1000,
        color: '#6366f1',
        weight: 2,
        fillColor: '#6366f1',
        fillOpacity: 0.18
    }).addTo(map);

    const radiusHandleIcon = L.divIcon({
        className: 'radius-handle-wrapper',
        html: '<div class="radius-handle" title="Drag to set radius"><i class="bi bi-arrows-angle-expand"></i></div>',
        iconSize: [28, 28],
        iconAnchor: [14, 14]
    });

    const radiusHandle = L.marker(getEdgePoint(lat, lng, radiusKm * 1000), {
        draggable: true,
        autoPan: true,
        icon: radiusHandleIcon
    }).addTo(map);

    const coordsDisplay = document.getElementById('mapCoords');
    const areaDisplay = document.getElementById('mapArea');
    const summaryCoords = document.getElementById('summaryCoords');
    const summaryRadius = document.getElementById('summaryRadius');
    const summaryArea = document.getElementById('summaryArea');

    function notify(msg, type) {
        if (window.RestaurantNotify) window.RestaurantNotify.show(msg, type, { duration: 3000 });
    }

    function distanceKm(lat1, lng1, lat2, lng2) {
        return map.distance(L.latLng(lat1, lng1), L.latLng(lat2, lng2)) / 1000;
    }

    function getEdgePoint(centerLat, centerLng, radiusMeters, bearingDeg = 90) {
        const earth = 6371000;
        const bearing = bearingDeg * Math.PI / 180;
        const lat1 = centerLat * Math.PI / 180;
        const lng1 = centerLng * Math.PI / 180;
        const angDist = radiusMeters / earth;

        const lat2 = Math.asin(
            Math.sin(lat1) * Math.cos(angDist) +
            Math.cos(lat1) * Math.sin(angDist) * Math.cos(bearing)
        );
        const lng2 = lng1 + Math.atan2(
            Math.sin(bearing) * Math.sin(angDist) * Math.cos(lat1),
            Math.cos(angDist) - Math.sin(lat1) * Math.sin(lat2)
        );

        return L.latLng(lat2 * 180 / Math.PI, lng2 * 180 / Math.PI);
    }

    function updateDisplays() {
        const area = (Math.PI * radiusKm * radiusKm).toFixed(1);
        if (coordsDisplay) coordsDisplay.textContent = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
        if (areaDisplay) areaDisplay.textContent = `${radiusKm.toFixed(1)} km radius (~${area} km²)`;
        if (radiusLabel) radiusLabel.textContent = `${radiusKm.toFixed(1)} km`;
        if (summaryCoords) summaryCoords.textContent = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
        if (summaryRadius) summaryRadius.textContent = radiusKm.toFixed(1);
        if (summaryArea) summaryArea.textContent = `~${area} km²`;
    }

    function syncInputs() {
        if (latInput) latInput.value = lat.toFixed(6);
        if (lngInput) lngInput.value = lng.toFixed(6);
        if (radiusInput) radiusInput.value = radiusKm.toFixed(1);
        if (radiusSlider) {
            radiusSlider.value = Math.min(parseFloat(radiusSlider.max) || 30, radiusKm);
        }
        updateDisplays();
    }

    function positionRadiusHandle() {
        radiusHandle.setLatLng(getEdgePoint(lat, lng, radiusKm * 1000));
    }

    function setLocation(newLat, newLng, pan = true) {
        lat = parseFloat(newLat);
        lng = parseFloat(newLng);
        marker.setLatLng([lat, lng]);
        circle.setLatLng([lat, lng]);
        positionRadiusHandle();
        syncInputs();
        if (pan) map.panTo([lat, lng]);
        fitCircleView();
    }

    function setRadius(km, snapHandle = true) {
        radiusKm = Math.max(0.5, Math.min(100, parseFloat(km) || 5));
        radiusKm = Math.round(radiusKm * 10) / 10;
        circle.setRadius(radiusKm * 1000);
        if (snapHandle) positionRadiusHandle();
        syncInputs();
        fitCircleView();
    }

    function setMapMode(mode) {
        mapMode = mode;
        if (modeLocationBtn && modeRadiusBtn) {
            modeLocationBtn.classList.toggle('active', mode === 'location');
            modeRadiusBtn.classList.toggle('active', mode === 'radius');
        }
        if (mapEl) {
            mapEl.classList.toggle('map-mode-radius', mode === 'radius');
            mapEl.classList.toggle('map-mode-location', mode === 'location');
        }
        if (mapModeHint) {
            mapModeHint.textContent = mode === 'radius'
                ? 'Click anywhere on the map to set delivery radius from the pin.'
                : 'Click the map or drag the pin to set restaurant location.';
        }
    }

    function fitCircleView() {
        try {
            map.fitBounds(circle.getBounds(), { padding: [48, 48], maxZoom: 15 });
        } catch (_) { /* ignore */ }
    }

    marker.on('drag', function () {
        const pos = marker.getLatLng();
        lat = pos.lat;
        lng = pos.lng;
        circle.setLatLng([lat, lng]);
        positionRadiusHandle();
        syncInputs();
    });

    marker.on('dragend', function () {
        const pos = marker.getLatLng();
        setLocation(pos.lat, pos.lng, false);
    });

    radiusHandle.on('drag', function () {
        const pos = radiusHandle.getLatLng();
        const km = distanceKm(lat, lng, pos.lat, pos.lng);
        if (km >= 0.5) {
            radiusKm = Math.round(Math.min(100, km) * 10) / 10;
            circle.setRadius(radiusKm * 1000);
            syncInputs();
        }
    });

    radiusHandle.on('dragend', function () {
        positionRadiusHandle();
        fitCircleView();
        notify(`Delivery radius set to ${radiusKm.toFixed(1)} km`, 'info');
    });

    map.on('click', function (e) {
        if (mapMode === 'radius') {
            const km = distanceKm(lat, lng, e.latlng.lat, e.latlng.lng);
            if (km < 0.5) {
                notify('Radius must be at least 0.5 km from the restaurant pin.', 'warning');
                return;
            }
            setRadius(km);
            notify(`Delivery radius set to ${radiusKm.toFixed(1)} km`, 'success');
            return;
        }
        setLocation(e.latlng.lat, e.latlng.lng, false);
    });

    if (radiusSlider) {
        radiusSlider.addEventListener('input', function () {
            setRadius(this.value);
        });
    }

    if (radiusInput) {
        radiusInput.addEventListener('input', function () {
            setRadius(this.value);
        });
    }

    if (latInput) {
        latInput.addEventListener('change', function () {
            setLocation(this.value, lng);
        });
    }

    if (lngInput) {
        lngInput.addEventListener('change', function () {
            setLocation(lat, this.value);
        });
    }

    if (modeLocationBtn) modeLocationBtn.addEventListener('click', () => setMapMode('location'));
    if (modeRadiusBtn) modeRadiusBtn.addEventListener('click', () => setMapMode('radius'));

    async function searchLocation() {
        const query = (searchInput?.value || addressInput?.value || '').trim();
        if (!query) return;

        if (searchBtn) {
            searchBtn.disabled = true;
            searchBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
        }

        try {
            const res = await fetch(
                `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`,
                { headers: { 'Accept-Language': 'en' } }
            );
            const results = await res.json();
            if (results?.length) {
                setLocation(results[0].lat, results[0].lon);
                if (addressInput && !addressInput.value.trim())
                    addressInput.value = results[0].display_name;
                notify('Restaurant location updated on map', 'success');
            } else {
                notify('Location not found. Try a different search or click on the map.', 'warning');
            }
        } catch {
            notify('Could not search location. Check your internet connection.', 'error');
        } finally {
            if (searchBtn) {
                searchBtn.disabled = false;
                searchBtn.innerHTML = '<i class="bi bi-search"></i> Find';
            }
        }
    }

    if (searchBtn) searchBtn.addEventListener('click', searchLocation);
    if (searchInput) {
        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchLocation();
            }
        });
    }

    const locateBtn = document.getElementById('mapLocateBtn');
    if (locateBtn) {
        locateBtn.addEventListener('click', function () {
            if (!navigator.geolocation) {
                notify('Geolocation is not supported by your browser.', 'error');
                return;
            }
            locateBtn.disabled = true;
            navigator.geolocation.getCurrentPosition(
                function (pos) {
                    setLocation(pos.coords.latitude, pos.coords.longitude);
                    locateBtn.disabled = false;
                    notify('Location set from GPS', 'success');
                },
                function () {
                    notify('Could not get your location.', 'error');
                    locateBtn.disabled = false;
                },
                { enableHighAccuracy: true, timeout: 10000 }
            );
        });
    }

    setMapMode('location');
    syncInputs();
    setTimeout(function () {
        map.invalidateSize();
        fitCircleView();
    }, 200);
})();
