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

Copia y ejecuta el siguiente script en tu cliente MySQL (como MySQL Workbench, phpMyAdmin o terminal) para levantar la base de datos:

```sql
-- 1. Creación de la base de datos
CREATE DATABASE IF NOT EXISTS artemusa_inventario;
USE artemusa_inventario;

-- 2. Tabla de roles de usuario
CREATE TABLE roles (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre_rol VARCHAR(50) NOT NULL UNIQUE
);

-- 3. Tabla de trabajadores (Autenticación)
CREATE TABLE trabajadores (
    id_trabajador INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL, -- Recomendado: Guardar hash (Bcrypt, Argon2, etc.)
    id_rol INT NOT NULL,
    FOREIGN KEY (id_rol) REFERENCES roles(id_rol) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 4. Inserción de datos iniciales (Roles)
INSERT INTO roles (nombre_rol) VALUES 
('Administrador'),
('Trabajador');

-- 5. Inserción de usuarios demo/iniciales
INSERT INTO trabajadores (nombre, correo, password, id_rol) VALUES 
('Juan Pérez', 'juan.admin@artemusa.com', 'admin123', 1),
('María López', 'maria.empleado@artemusa.com', 'user123', 2);

```

---

## 🔑 Cuentas de Acceso por Defecto (Entorno de Desarrollo)

| Nombre | Correo Electrónico | Contraseña | Rol | ID Rol |
| --- | --- | --- | --- | --- |
| **Juan Pérez** | `juan.admin@artemusa.com` | `admin123` | Administrador | `1` |
| **María López** | `maria.empleado@artemusa.com` | `user123` | Trabajador | `2` |

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
