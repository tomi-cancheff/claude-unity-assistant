# Claude Game Assistant for Unity
### Prototipá juegos con IA directamente en el Editor

---

## 📁 Estructura del proyecto

```
Assets/
└── Editor/
    └── ClaudeAssistant/
        ├── Core/
        │   ├── ClaudeApiClient.cs        ← Singleton HTTP client
        │   ├── ClaudeConfig.cs           ← ScriptableObject de configuración
        │   └── ConversationHistory.cs    ← Historial de chat (Repository)
        ├── Handlers/
        │   ├── IGenerationHandler.cs     ← Interfaz Strategy
        │   ├── ScriptGenerationHandler.cs← Genera scripts .cs
        │   └── SceneGenerationHandler.cs ← Construye escenas
        ├── Models/
        │   ├── ClaudeApiModels.cs        ← Modelos de serialización API
        │   ├── ChatMessage.cs            ← Modelo de mensaje
        │   └── GenerationResult.cs      ← Resultado de handlers
        ├── Utils/
        │   ├── IntentClassifier.cs       ← Clasifica el intent del prompt
        │   └── CodeExtractor.cs         ← Limpia markdown del response
        └── UI/
            └── ClaudeAssistantWindow.cs  ← EditorWindow principal
```

---

## ⚙️ Instalación

### 1. Copiar los archivos
Copiá toda la carpeta `ClaudeAssistant` dentro de `Assets/Editor/` en tu proyecto Unity.

### 2. Obtener tu API Key (gratis)
1. Creá cuenta en [console.anthropic.com](https://console.anthropic.com)
2. Andá a **API Keys → Create Key**
3. Copiá la key generada

### 3. Configurar el asset
Al abrir la ventana por primera vez, Unity crea automáticamente
`Assets/Editor/ClaudeAssistant/ClaudeConfig.asset`.

Hacé click en ese asset y pegá tu API key en el campo correspondiente.

### 4. Abrir la ventana
```
Tools → Claude Game Assistant   (o Ctrl+Shift+C)
```

---

## 🚀 Uso

### Modo automático (recomendado)
Dejá **Modo = Unknown** y escribí en lenguaje natural.
El `IntentClassifier` detecta automáticamente si querés:

| Intent detectado | Resultado |
|---|---|
| Crear/generar/armar objetos, niveles, layouts | 🏗️ **Scene Mode** |
| Scripts, controladores, sistemas, mecánicas  | 📄 **Script Mode** |

### Modo manual
Seleccioná explícitamente **Script** o **Scene** en el dropdown.

---

## 💬 Ejemplos de prompts

### Scripts
```
"Creá un controlador de plataformero 2D con salto, coyote time y buffer de salto"
"Sistema de vida con daño, invencibilidad temporal y evento OnDeath"  
"Cámara 3D que sigue al jugador con suavizado tipo SmoothDamp"
"Enemigo con 3 estados: patrulla, persecución y ataque usando una FSM"
"Singleton de GameManager con sistema de puntuación y pausa"
```

### Escenas
```
"Armá 8 plataformas en zigzag para un nivel 2D"
"Creá una habitación 3D de 12x8 con paredes, piso y techo"
"Generá un laberinto simple 5x5 con cubos"
"Poné un plano, 3 luces y una cámara centrada para una escena de prueba"
```

### Iteración (el chat recuerda el contexto)
```
Usuario: "Creá un nivel 2D con 6 plataformas"
Claude:  [genera el nivel]
Usuario: "Ahora agregale un enemigo al final"
Claude:  [agrega el enemigo al mismo nivel]
Usuario: "Hacé que el último enemigo sea más grande"
Claude:  [ajusta la escala]
```

---

## 🏗️ Patrones de diseño aplicados

| Patrón | Dónde | Para qué |
|---|---|---|
| **Strategy** | `IGenerationHandler` | Intercambiar modos sin tocar el resto |
| **Singleton** | `ClaudeApiClient` | Una sola instancia del cliente HTTP |
| **Repository** | `ConversationHistory` | Abstrae el manejo del historial |
| **Command + Undo** | `SceneGenerationHandler` | Escenas reversibles con Ctrl+Z |
| **ScriptableObject** | `ClaudeConfig` | Config persistente entre sesiones |
| **Factory** | `GenerationResult` | Construcción expresiva de resultados |

---

## 💰 Costo estimado

| Modelo | Por prompt complejo | 100 prompts |
|---|---|---|
| claude-haiku | ~$0.0003 | ~$0.03 |
| claude-sonnet | ~$0.015 | ~$1.50 |

Los créditos gratuitos iniciales de la API cubren **cientos de generaciones**.

---

## 🔧 Extender el sistema

Para agregar un nuevo modo de generación:
1. Implementá `IGenerationHandler` en una nueva clase
2. Registrala en el diccionario `Handlers` de `ClaudeAssistantWindow`
3. Agregá las keywords al `IntentClassifier`

¡Eso es todo! No hay que modificar ninguna otra clase.
