# ARTEMUSA PERÚ S.R.L. — Sistema Integrado de Control Audiovisual

## Descripción del proyecto

Este repositorio contiene la versión integrada del sistema de control de equipos audiovisuales de **Artemusa Perú S.R.L.**, desarrollado en C# mediante Windows Forms y .NET.

Esta versión reúne las funcionalidades desarrolladas en las ramas `trabajador` y `administrador`, con el propósito de consolidarlas en una sola aplicación.

## Integración de ramas

La rama **`sistema-integrado`** ha sido creada para combinar el trabajo realizado en las siguientes ramas:

- **`trabajador`:** funcionalidades relacionadas con el panel del trabajador, la gestión de equipos audiovisuales, el registro de salidas y la visualización de ubicaciones mediante mapas interactivos.
- **`administrador`:** funcionalidades relacionadas con el panel administrativo, el control de inventario, los reportes y otros módulos de administración.

Las ramas originales se mantienen independientes para conservar el historial de desarrollo.

## Tecnologías utilizadas

- **C# y Windows Forms:** desarrollo de la aplicación de escritorio.
- **.NET 9:** plataforma de ejecución.
- **MySQL:** almacenamiento de la información.
- **WebView2:** integración del contenido web dentro de la aplicación.
- **HTML, CSS y JavaScript:** interfaz del mapa interactivo.
- **Leaflet y OpenStreetMap:** visualización y selección de ubicaciones.
- **Twilio:** integración de funcionalidades de mensajería.

## Base de datos

La estructura, configuración y los scripts SQL de la base de datos se encuentran documentados y administrados por separado.

Este README no incluye las instrucciones de creación de tablas ni los datos de inicialización de MySQL.

Para ejecutar correctamente el sistema, se requiere configurar previamente la conexión con la base de datos correspondiente.

## Estado de integración

La rama `sistema-integrado` está destinada a reunir los módulos de trabajador y administrador en una única aplicación.

La integración del código debe complementarse con pruebas de compilación, autenticación, acceso a los paneles, conexión a la base de datos y funcionamiento de los módulos.

## Empresa

**Artemusa Perú S.R.L.**

Sistema de control y administración de equipos audiovisuales.