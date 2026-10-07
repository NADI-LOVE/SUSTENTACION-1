# 📦 Artemusa Inventario - Base de Datos

Sistema de gestión de inventarios para **Artemusa**. Este módulo contiene la estructura e inicialización de la base de datos relacional MySQL, incluyendo la gestión de usuarios, roles y autenticación.

---

## 🛠️ Tecnologías Utilizadas

- **SGBD:** MySQL / MariaDB
- **Lenguaje:** SQL

---

## 📐 Modelo Relacional / Estructura

La base de datos consta de las siguientes tablas principales:

1. **`roles`**: Define los niveles de acceso dentro del sistema.
2. **`trabajadores`**: Almacena las credenciales de inicio de sesión y datos personales del personal, vinculados a su respectivo rol.

---

## 🚀 Script de Instalación e Inicialización

Copia y ejecuta el siguiente script en tu cliente MySQL (como MySQL Workbench, phpMyAdmin o terminal) para levantar la base de datos completa:

```sql
-- ============================================
-- ARTEMISA - Script de Base de Datos
-- Base de datos: artemusa_inventario
-- ============================================

-- 1. Crear la base de datos
CREATE DATABASE IF NOT EXISTS artemusa_inventario;
USE artemusa_inventario;

-- 2. Eliminar tablas en orden (por si existen con estructura vieja)
DROP TABLE IF EXISTS salida_equipos;
DROP TABLE IF EXISTS salidas;
DROP TABLE IF EXISTS eventos;
DROP TABLE IF EXISTS equipos;
DROP TABLE IF EXISTS trabajadores;
DROP TABLE IF EXISTS roles;

-- ============================================
-- 3. TABLA: roles
-- ============================================
CREATE TABLE roles (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre_rol VARCHAR(50) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 4. TABLA: trabajadores
-- ============================================
CREATE TABLE trabajadores (
    id_trabajador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    id_rol INT NOT NULL,
    FOREIGN KEY (id_rol) REFERENCES roles(id_rol) 
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 5. TABLA: equipos
-- ============================================
CREATE TABLE equipos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL,
    nombre VARCHAR(120) NOT NULL,
    categoria VARCHAR(80) DEFAULT 'General',
    ubicacion VARCHAR(80) DEFAULT 'Almacén Principal',
    stock INT DEFAULT 1,
    estado VARCHAR(50) DEFAULT 'Disponible',
    descripcion TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 6. TABLA: eventos
-- ============================================
CREATE TABLE eventos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    fecha DATE NOT NULL,
    titulo VARCHAR(150) NOT NULL,
    horario VARCHAR(100),
    detalles VARCHAR(255)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 7. TABLA: salidas (registro de salidas de equipos)
-- ============================================
CREATE TABLE salidas (
    id_salida INT AUTO_INCREMENT PRIMARY KEY,
    id_trabajador INT NOT NULL,
    encargado VARCHAR(100),
    fecha_salida DATETIME NOT NULL,
    ubicacion VARCHAR(255),
    latitud DECIMAL(10, 8),
    longitud DECIMAL(11, 8),
    detalles TEXT,
    estado VARCHAR(50) DEFAULT 'Programada',
    fecha_registro DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_trabajador) REFERENCES trabajadores(id_trabajador) 
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 8. TABLA: salida_equipos (relación N:M)
-- ============================================
CREATE TABLE salida_equipos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_salida INT NOT NULL,
    id_equipo INT NOT NULL,
    codigo_escaneado VARCHAR(100),
    FOREIGN KEY (id_salida) REFERENCES salidas(id_salida) 
        ON DELETE CASCADE,
    FOREIGN KEY (id_equipo) REFERENCES equipos(id) 
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 9. DATOS INICIALES
-- ============================================

-- Roles
INSERT INTO roles (nombre_rol) VALUES 
('Administrador'),
('Trabajador');

-- Trabajadores
INSERT INTO trabajadores (nombre, correo, password, id_rol) VALUES 
('Juan Pérez', 'juan.admin@artemusa.com', 'admin123', 1),
('Maria López', 'maria.empleado@artemusa.com', 'user123', 2);

-- Equipos
INSERT INTO equipos (codigo, nombre, categoria, ubicacion, stock, estado, descripcion) VALUES
('CAM-001', 'Cámara Sony FX3', 'Cámaras y Lentes', 'Rack A1', 2, 'Disponible', 'Cámara principal de estudio'),
('MIC-002', 'Micrófono Shure SM7B', 'Audio y Micrófonos', 'Estante B2', 5, 'Disponible', 'Micrófono para cabina de radio/TV');

-- Eventos
INSERT INTO eventos (fecha, titulo, horario, detalles) VALUES 
(CURDATE(), 'Mantenimiento Laptop Dell', '09:00 - 11:00 AM', 'EQ-001'),
(DATE_ADD(CURDATE(), INTERVAL 2 DAY), 'Salida de Monitores LG', '02:00 - 04:00 PM', 'EQ-002');
```

---

## 🔑 Cuentas de Acceso por Defecto (Entorno de Desarrollo)

| Nombre | Correo Electrónico | Contraseña | Rol | ID Rol |
|--------|---------------------|------------|-----|--------|
| **Juan Pérez** | `juan.admin@artemusa.com` | `admin123` | Administrador | 1 |
| **María López** | `maria.empleado@artemusa.com` | `user123` | Trabajador | 2 |

---

## 🗄️ Estructura de la Base de Datos

### Tabla `roles`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id_rol | INT (PK) | ID del rol |
| nombre_rol | VARCHAR(50) | Nombre del rol (único) |

### Tabla `trabajadores`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id_trabajador | INT (PK) | ID del trabajador |
| nombre | VARCHAR(100) | Nombre completo |
| correo | VARCHAR(100) | Correo (único) |
| password | VARCHAR(255) | Contraseña |
| id_rol | INT (FK) | Rol asignado |

### Tabla `equipos`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | INT (PK) | ID del equipo |
| codigo | VARCHAR(50) | Código de barras / SKU |
| nombre | VARCHAR(120) | Nombre del equipo |
| categoria | VARCHAR(80) | Categoría |
| ubicacion | VARCHAR(80) | Ubicación en almacén |
| stock | INT | Cantidad disponible |
| estado | VARCHAR(50) | Disponible / En Evento / etc. |
| descripcion | TEXT | Descripción detallada |

### Tabla `eventos`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | INT (PK) | ID del evento |
| fecha | DATE | Fecha del evento |
| titulo | VARCHAR(150) | Título del evento |
| horario | VARCHAR(100) | Horario |
| detalles | VARCHAR(255) | Detalles adicionales |

### Tabla `salidas`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id_salida | INT (PK) | ID de la salida |
| id_trabajador | INT (FK) | Trabajador que registra |
| encargado | VARCHAR(100) | Nombre del encargado |
| fecha_salida | DATETIME | Fecha y hora de salida |
| ubicacion | VARCHAR(255) | Ubicación del evento |
| latitud | DECIMAL(10,8) | Latitud (Google Maps) |
| longitud | DECIMAL(11,8) | Longitud (Google Maps) |
| detalles | TEXT | Detalles adicionales |
| estado | VARCHAR(50) | Programada / En Curso / Finalizada |
| fecha_registro | DATETIME | Fecha de registro |

### Tabla `salida_equipos`
| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | INT (PK) | ID de la relación |
| id_salida | INT (FK) | ID de la salida |
| id_equipo | INT (FK) | ID del equipo |
| codigo_escaneado | VARCHAR(100) | Código escaneado con pistola |

> ⚠️ **Nota de Seguridad:** Las contraseñas incluidas en los datos iniciales se muestran en texto plano únicamente con fines de prueba y desarrollo. En un entorno de producción, asegúrate de almacenar las contraseñas procesadas mediante un algoritmo de hashing seguro.

---

## ⚙️ Pasos para la Configuración Local

1. Clona este repositorio:
```bash
git clone [https://github.com/tu-usuario/artemusa-inventario.git](https://github.com/tu-usuario/artemusa-inventario.git)

```


2. Inicia tu servidor MySQL local (XAMPP, WAMP, Docker o servicio independiente).
3. Importa el archivo `.sql` o ejecuta los comandos en tu gestor de base de datos preferido.

```

***

### 💡 Mejoras clave aplicadas para GitHub:
1. **Formato Markdown limpio:** Uso de encabezados, listas, negritas y enlaces de código.
2. **Tabla de datos:** Muestra de forma visual las cuentas predeterminadas de prueba.
3. **Bloque SQL resaltado:** Facilita la lectura del código mediante sintaxis nativa.
4. **Buenas prácticas:** Se añadieron las cláusulas `ON DELETE CASCADE ON UPDATE CASCADE` a la clave foránea para evitar inconsistencias si en un futuro se actualizan o borran datos.

```
