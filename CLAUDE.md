# Playground Plaza — Convenciones del proyecto

Sandbox 3D de plaza para chicos (Unity 6000.x URP). Explora tooling con IA (Unity MCP + Blender MCP). Estas son las reglas que **siempre** aplican al construir; la doc viva detallada y el backlog viven en la vault Obsidian del usuario (no en git): `Documents/guiddo_dg/Proyectos/Playground Plaza/`.

## Estructura de carpetas
Todo el contenido propio va bajo `Assets/_Project/` (nunca en la raíz de `Assets/`):
- `Art/` — `Fonts/`, `Icons/`, `Materials/`, `Models/` (`Items/`, `Props/`), `Textures/`, `UI/` (una subcarpeta por pantalla: `Crafting/`, `Inventory/`; referencias en `_reference/`).
- `Scripts/` — una subcarpeta por sistema/feature: `Inventory/`, `Crafting/`, `Interaction/`, `Items/`, `UI/`. Scripts globales de jugador/mundo van en la raíz de `Scripts/` (`PlayerController`, `CameraOrbitInput`, `GameplayInputLock`, `Procedural*`).
- `Data/` — ScriptableObjects: `Recipes/`, `Items/` (`Equipables/`, `Consumibles/`, `Coleccionables/`).
- `Prefabs/` — `Environment/`, `Playground/`, `UI/`.
- `Scenes/Playground/Playground.unity` — escena principal. `Animations/`.

Al crear algo nuevo, ubicarlo en la subcarpeta correcta; si es un sistema nuevo, crear su subcarpeta en `Scripts/`.

## Jerarquía de escena
Contenedores raíz con prefijo `_`: `_Environment`, `_Lighting`, `_Gameplay`, `_Cameras`, `_Player`, `_UI`. Nada suelto en la raíz de la escena.

## Patrones de arquitectura
- **Datos en ScriptableObject** (`ItemData`, `RecipeData`), no hardcodear.
- **Interacción**: implementar `IInteractable` (`CanInteract`, `GetPrompt()`, `Interact(GameObject)`). `PlayerInteractor` raycastea desde el centro de cámara, tecla **F**, layer `Interactable` (9).
- **Lock de input**: `GameplayInputLock` atado a la visibilidad del panel (gate en `OnEnable`/`OnDisable`), no manual.
- **Singletons de runtime**: `PlayerInventory.Instance`, `CraftingSystem.Instance`.
- **Notificaciones**: `NotificationFeed.Post(string)` (API estática; no depender de la UI).

## Reglas duras (romperlas causó bugs reales)
- **Geometría procedural** (`[ExecuteAlways]` que regenera hijos): usar **`DontSaveInEditor | DontSaveInBuild`**, nunca `HideFlags.DontSave` pelado. Distinción:
  - **Mallas generadas** (`new Mesh()`, p. ej. `ProceduralPlazaShape`/`SnakePath`): es **crítico** — `DontSave` incluye `DontUnloadUnusedAsset` → la malla **leakea**. Además liberarla en `OnDisable`.
  - **GameObjects hijos** (primitivas/prefabs, p. ej. `ProceduralFence`/`TreeRing`): no leakean assets, pero usar los mismos flags por **consistencia** (se limpian en `ClearChildren` al regenerar). Evita el churn no-determinístico de la escena.
- **Nunca** `while(container.childCount > max) Destroy(...)`: `Destroy` es diferido, `childCount` no baja en el frame → loop infinito → OOM. Usar loop de cuenta fija.
- **Con Unity abierto**: no cambiar de rama git ni hacer reimport force/all (reserializa la escena entera). No abusar de `Refresh(ForceUpdate)`/reloads (infla la RAM → OOM).
- **`execute_code` (CodeDom C#6)**: sin local functions (usar `System.Func`), tipos full-qualified, evitar string interpolation.
- Modelos 3D reales (`.glb`/`.fbx`) sobre primitivas hechas a mano.
- **Sprites/fuentes/imágenes de UI**: generarlos con PIL/Windows o tools de imagen, escribiendo al **disco real** (Write/PowerShell). El tool Bash está sandboxeado y NO llega al disco que lee Unity.

## URP / glTF
Importar `.glb` con glTFast (materiales `Shader Graphs/glTF-*`, sin magenta). Tree Creator legacy: asignar la mesh sub-asset al `MeshFilter`.

## Git
- Flujo: `feature/N-nombre` | `fix/N-nombre` | `chore/N-nombre` → PR **squash** a `develop` (integración) → `master`. Mensajes con "Closes #N". CI: EditorConfig Lint + SonarCloud.
- **git solo por PowerShell** (Bash opera sobre un sandbox, no el repo real).
- **Antes de cada `git push`**: correr la skill **`push-docs-sync`** (sincroniza la vault; un hook bloquea el push si no se corrió).

## Skills / verificación
- Operar Unity por MCP: skill **`unity-mcp-skill`** (compilación, `batch_execute`, screenshots, consola, `editor_state`).
- Scaffoldear/auditar features según estas convenciones: skill **`unity-conventions`**.
- Verificar visualmente en Play: `ScreenCapture` (con `Application.runInBackground=true`) o `manage_camera` screenshot.
