"use strict";

let map;
let marcador = null;

let ocupado = false;
let ultimoAcceso = 0;
let ultimaSeleccion = 0;

const API = "https://nominatim.openstreetmap.org";

const $ = id => document.getElementById(id);

// ══════════════════════════════════════════════
// COMUNICACIÓN CON C#
// ══════════════════════════════════════════════
function enviar(mensaje) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(mensaje);
    }
}

function estado(texto) {
    $("status").textContent = texto;
    enviar("estado|" + texto);
}

// ══════════════════════════════════════════════
// CONTROL DE MODOS
// ══════════════════════════════════════════════
function cambiarModo(modo) {
    document.querySelectorAll(".tab").forEach(b => {
        b.classList.toggle("active", b.dataset.mode === modo);
    });

    document.querySelectorAll(".group").forEach(g => {
        g.classList.toggle("active", g.id === modo);
    });

    setTimeout(() => {
        if (map) map.invalidateSize();
    }, 100);
}

// ══════════════════════════════════════════════
// LIMITACIÓN DE SOLICITUDES
// ══════════════════════════════════════════════
const esperar = ms => new Promise(resolve => setTimeout(resolve, ms));

async function consultarAPI(url) {
    const transcurrido = Date.now() - ultimoAcceso;

    if (transcurrido < 1200) {
        await esperar(1200 - transcurrido);
    }

    ultimoAcceso = Date.now();

    const respuesta = await fetch(url, {
        headers: { "Accept": "application/json" }
    });

    if (!respuesta.ok) {
        throw new Error("HTTP " + respuesta.status);
    }

    return await respuesta.json();
}

// ══════════════════════════════════════════════
// EXTRAER LOCALIDAD DEL RESULTADO
// ══════════════════════════════════════════════
function extraerLocalidad(address = {}) {
    const ciudad =
        address.city ||
        address.town ||
        address.village ||
        address.municipality ||
        address.hamlet ||
        address.county ||
        "No identificada";

    const region = address.state || address.region || "";

    return [ciudad, region, address.country]
        .filter(Boolean)
        .join(", ");
}

// ══════════════════════════════════════════════
// COMPLETAR INFORMACIÓN
// ══════════════════════════════════════════════
function completarDatos(datos) {
    $("datoLugar").textContent =
        datos.lugar || "Lugar sin nombre identificado";

    $("datoDireccion").textContent =
        datos.direccion || "Sin dirección registrada";

    $("datoLocalidad").textContent =
        datos.localidad || "No identificada";

    $("datoCoordenadas").textContent =
        datos.lat.toFixed(6) + ", " + datos.lon.toFixed(6);

    // Enviar a C# los datos del destino
    const direccion = datos.direccion || "";
    const localidad = datos.localidad || "";
    const nombre = datos.lugar || "";

    if (direccion || localidad || nombre) {
        // Formato: "ubicacion|DIRECCION|LOCALIDAD|NOMBRE"
        enviar("ubicacion|" + direccion + "|" + localidad + "|" + nombre);
    } else {
        enviar("ubicacion|||");
        estado("No hay dirección identificada.");
    }
}

// ══════════════════════════════════════════════
// COLOCAR MARCADOR
// ══════════════════════════════════════════════
function marcar(lat, lon, titulo, zoom = 16) {
    const ubicacion = [lat, lon];

    if (marcador) {
        marcador.setLatLng(ubicacion);
    } else {
        marcador = L.marker(ubicacion).addTo(map);
    }

    marcador.bindPopup(titulo || "Destino seleccionado");
    map.setView(ubicacion, zoom);
    calcularRuta(lat, lon);
}

// ══════════════════════════════════════════════
// BÚSQUEDA POR TEXTO
// ══════════════════════════════════════════════
async function buscarTexto(texto, tipo) {
    texto = (texto || "").trim();

    if (!texto) {
        estado("Escribe el lugar que deseas buscar.");
        return;
    }

    if (ocupado) {
        estado("Espera a que finalice la búsqueda.");
        return;
    }

    ocupado = true;
    enviar("ubicacion|");
    const solicitud = ++ultimaSeleccion;

    estado("Buscando ubicación...");

    try {
        let consulta = texto;

        if (tipo === "localidad" && !/per[uú]/i.test(texto)) {
            consulta += ", Perú";
        }

        const params = new URLSearchParams({
            q: consulta,
            format: "jsonv2",
            addressdetails: "1",
            "accept-language": "es",
            limit: "1"
        });

        const resultados = await consultarAPI(
            API + "/search?" + params.toString()
        );

        if (solicitud !== ultimaSeleccion) return;

        if (!resultados.length) {
            estado("No se encontró el lugar indicado.");
            return;
        }

        const r = resultados[0];
        const lat = Number(r.lat);
        const lon = Number(r.lon);

        if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
            throw new Error("Coordenadas no válidas");
        }

        const addr = r.address || {};

        let nombre =
            r.name ||
            addr.amenity ||
            addr.tourism ||
            addr.road ||
            extraerLocalidad(addr);

        const direccion = r.display_name || nombre;

        marcar(lat, lon, nombre, tipo === "localidad" ? 13 : 17);

        completarDatos({
            lugar: nombre,
            direccion: direccion,
            localidad: extraerLocalidad(addr),
            lat: lat,
            lon: lon
        });

        estado("Ubicación encontrada. Revisa el marcador.");
    }
    catch (error) {
        estado("Error al consultar el lugar: " + error.message);
    }
    finally {
        ocupado = false;
    }
}

function buscarLocalidad() {
    buscarTexto($("inputLocalidad").value, "localidad");
}

function buscarDireccion() {
    buscarTexto($("inputDireccion").value, "direccion");
}

// ══════════════════════════════════════════════
// CONSULTA INVERSA: COORDENADAS -> DIRECCIÓN
// ══════════════════════════════════════════════
async function localizarPunto(lat, lon, zoom = 17) {
    if (ocupado) {
        estado("Espera a que termine la consulta.");
        return;
    }

    const solicitud = ++ultimaSeleccion;
    ocupado = true;

    marcar(lat, lon, "Destino seleccionado", zoom);
    estado("Consultando nombre del lugar...");

    try {
        const params = new URLSearchParams({
            lat: String(lat),
            lon: String(lon),
            format: "jsonv2",
            addressdetails: "1",
            "accept-language": "es",
            zoom: "18"
        });

        const r = await consultarAPI(
            API + "/reverse?" + params.toString()
        );

        if (solicitud !== ultimaSeleccion) return;

        if (r.error) {
            throw new Error(r.error);
        }

        const addr = r.address || {};

        const nombre =
            r.name ||
            addr.amenity ||
            addr.tourism ||
            addr.road ||
            addr.suburb ||
            extraerLocalidad(addr);

        const direccion = r.display_name || "";

        completarDatos({
            lugar: nombre,
            direccion: direccion,
            localidad: extraerLocalidad(addr),
            lat: lat,
            lon: lon
        });

        marcador.bindPopup(nombre);
        estado("Dirección cercana identificada.");
    }
    catch (error) {
        completarDatos({
            lugar: "Punto seleccionado en el mapa",
            direccion: "",
            localidad: "",
            lat: lat,
            lon: lon
        });

        estado("Punto marcado; no se obtuvo una dirección.");
    }
    finally {
        ocupado = false;
    }
}

// ══════════════════════════════════════════════
// BÚSQUEDA POR COORDENADAS
// ══════════════════════════════════════════════
function buscarCoordenadas() {
    const lat = Number($("inputLat").value.trim());
    const lon = Number($("inputLon").value.trim());

    if (
        !Number.isFinite(lat) ||
        !Number.isFinite(lon) ||
        $("inputLat").value.trim() === "" ||
        $("inputLon").value.trim() === "" ||
        lat < -90 || lat > 90 ||
        lon < -180 || lon > 180
    ) {
        estado("Ingresa coordenadas decimales válidas.");
        return;
    }

    localizarPunto(lat, lon);
}

// ══════════════════════════════════════════════
// SELECCIÓN MANUAL EN EL MAPA
// ══════════════════════════════════════════════
function seleccionarEnMapa(e) {
    if (ocupado) {
        estado("Espera a que finalice la consulta actual.");
        return;
    }

    $("inputLat").value = e.latlng.lat.toFixed(6);
    $("inputLon").value = e.latlng.lng.toFixed(6);

    localizarPunto(e.latlng.lat, e.latlng.lng, map.getZoom());
}

// ══════════════════════════════════════════════
// INICIALIZAR MAPA
// ══════════════════════════════════════════════
function iniciar() {
    if (typeof L === "undefined") {
        estado("No se pudo cargar Leaflet. Comprueba Internet.");
        return;
    }

    map = L.map("map", {
        zoomControl: false
    }).setView([-15.840886485384564, -70.02662221417776], 16);

    mostrarOrigen();

    L.tileLayer(
        "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
        {
            maxZoom: 19,
            referrerPolicy: "strict-origin-when-cross-origin",
            attribution:
                '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>'
        }
    ).addTo(map);

    L.control.zoom({
        position: "bottomright"
    }).addTo(map);

    map.on("click", seleccionarEnMapa);

    // Escuchar mensajes desde C#
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener(
            "message",
            e => buscarTexto(String(e.data || ""), "direccion")
        );
    }

    // Buscar con Enter
    ["inputLocalidad", "inputDireccion"].forEach(id => {
        $(id).addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();

                if (id === "inputLocalidad") {
                    buscarLocalidad();
                } else {
                    buscarDireccion();
                }
            }
        });
    });

    ["inputLat", "inputLon"].forEach(id => {
        $(id).addEventListener("keydown", e => {
            if (e.key === "Enter") {
                e.preventDefault();
                buscarCoordenadas();
            }
        });
    });

    estado("Mapa listo. Elige cómo buscar un destino.");
    enviar("mapa_listo");

    // Forzar el tamaño en varios intervalos (WebView2 tarda en medir)
    setTimeout(() => map.invalidateSize(), 100);
    setTimeout(() => map.invalidateSize(), 500);
    setTimeout(() => map.invalidateSize(), 1000);
    setTimeout(() => map.invalidateSize(), 2000);

    // Confirmar que el mapa está listo
    setTimeout(() => enviar("mapa_listo"), 800);
}

window.addEventListener("load", iniciar);

// ══════════════════════════════════════════════
// ORIGEN FIJO - ARTEMUSA
// ══════════════════════════════════════════════
const ORIGEN = {
    lat: -15.840886485384564,
    lon: -70.02662221417776
};

let marcadorOrigen = null;
let lineaRuta = null;
let solicitudRuta = 0;

// ══════════════════════════════════════════════
// MARCADOR DEL PUNTO DE PARTIDA
// ══════════════════════════════════════════════
function mostrarOrigen() {
    const iconoOrigen = L.divIcon({
        className: "",
        html: `
            <div style="
                width:18px;
                height:18px;
                background:#17b981;
                border:4px solid white;
                border-radius:50%;
                box-shadow:0 0 0 4px rgba(23,185,129,.30),
                           0 3px 12px rgba(0,0,0,.4);
            "></div>
        `,
        iconSize: [18, 18],
        iconAnchor: [9, 9]
    });

    marcadorOrigen = L.marker(
        [ORIGEN.lat, ORIGEN.lon],
        { icon: iconoOrigen }
    ).addTo(map);

    marcadorOrigen.bindPopup(`
        <strong>Punto de partida</strong>
        <p>Origen del traslado de equipos audiovisuales</p>
    `);
}

// ══════════════════════════════════════════════
// DAR FORMATO AL TIEMPO
// ══════════════════════════════════════════════
function formatearTiempo(segundos) {
    const minutos = Math.round(segundos / 60);

    if (minutos < 60) {
        return minutos + " min";
    }

    const horas = Math.floor(minutos / 60);
    const resto = minutos % 60;

    if (resto === 0) {
        return horas + " h";
    }

    return horas + " h " + resto + " min";
}

// ══════════════════════════════════════════════
// CALCULAR RUTA POR CARRETERA
// ══════════════════════════════════════════════
async function calcularRuta(latDestino, lonDestino) {
    const solicitudActual = ++solicitudRuta;

    const distanciaElemento = document.getElementById("distanciaRuta");
    const tiempoElemento = document.getElementById("tiempoRuta");
    const estadoElemento = document.getElementById("estadoRuta");

    if (!distanciaElemento || !tiempoElemento || !estadoElemento) {
        return;
    }

    distanciaElemento.textContent = "-- km";
    tiempoElemento.textContent = "-- min";

    estadoElemento.textContent = "Calculando ruta por carretera...";

    if (lineaRuta) {
        map.removeLayer(lineaRuta);
        lineaRuta = null;
    }

    try {
        const coordenadas =
            ORIGEN.lon + "," + ORIGEN.lat + ";" +
            lonDestino + "," + latDestino;

        const url =
            "https://router.project-osrm.org/route/v1/driving/" +
            coordenadas +
            "?overview=full&geometries=geojson&steps=false";

        const respuesta = await fetch(url);

        if (!respuesta.ok) {
            throw new Error("Servicio no disponible: " + respuesta.status);
        }

        const datos = await respuesta.json();

        if (solicitudActual !== solicitudRuta) {
            return;
        }

        if (
            datos.code !== "Ok" ||
            !datos.routes ||
            datos.routes.length === 0
        ) {
            estadoElemento.textContent =
                "No se encontró una ruta por carretera.";
            return;
        }

        const ruta = datos.routes[0];

        lineaRuta = L.geoJSON(ruta.geometry, {
            style: {
                color: "#6458f5",
                weight: 6,
                opacity: 0.95,
                lineCap: "round",
                lineJoin: "round"
            }
        }).addTo(map);

        map.fitBounds(lineaRuta.getBounds(), {
            padding: [45, 45],
            maxZoom: 16
        });

        const kilometros = ruta.distance / 1000;

        distanciaElemento.textContent =
            kilometros.toLocaleString("es-PE", {
                minimumFractionDigits: 1,
                maximumFractionDigits: 1
            }) + " km";

        tiempoElemento.textContent = formatearTiempo(ruta.duration);

        estadoElemento.textContent =
            "Ruta calculada para traslado en vehículo. " +
            "Tiempo referencial, sin tráfico en tiempo real.";
    }
    catch (error) {
        if (solicitudActual !== solicitudRuta) {
            return;
        }

        console.error("Error de ruta:", error);

        estadoElemento.textContent =
            "No se pudo calcular el trayecto. " +
            "Comprueba Internet e inténtalo nuevamente.";
    }
}