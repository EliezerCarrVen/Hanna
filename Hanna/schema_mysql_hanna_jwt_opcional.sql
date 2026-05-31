USE hanna_relacional;

-- JWT de Hanna puede funcionar solo con telegram_chat_id.
-- Estas columnas son opcionales, pero útiles si luego quieres login por email/contraseña.
ALTER TABLE usuarios
ADD COLUMN email VARCHAR(150) UNIQUE NULL,
ADD COLUMN password_hash TEXT NULL;

-- Si MySQL dice que ya existen, ignora ese error.

UPDATE usuarios
SET email = 'eliezer@hanna.local'
WHERE telegram_chat_id = '5112232887'
  AND email IS NULL;

CREATE INDEX idx_usuarios_email ON usuarios(email);
CREATE INDEX idx_usuarios_telegram_chat_id ON usuarios(telegram_chat_id);
