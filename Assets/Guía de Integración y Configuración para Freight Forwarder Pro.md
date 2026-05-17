# Guía de Integración y Configuración para Freight Forwarder Pro

Este documento detalla los pasos necesarios para integrar y configurar los scripts proporcionados en su proyecto Unity 6, asegurando un sistema de mapa 3D, tiempo e iluminación solar realista y funcional.

## 1. Estructura del Proyecto

Para una organización óptima, se recomienda la siguiente estructura de carpetas en su proyecto Unity:

```
Assets/
├── Art/
│   └── Map/
│       └── Resources/
│           └── Map/
│               └── Textures/ (Aquí deben ir 01.jpg a 12.jpg)
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── CityMarker.cs
│   ├── GameBootstrapper.cs
│   ├── MapCameraController.cs
│   ├── SunController.cs
│   ├── TimeManager.cs
│   └── WorldMap.cs
└── UI/
```

**Scripts:** Coloque todos los archivos `.cs` proporcionados en la carpeta `Assets/Scripts/`.

**Texturas:** Las texturas mensuales de la Tierra (`01.jpg` a `12.jpg`) deben ubicarse en `Assets/Art/Map/Resources/Map/Textures/`. Es crucial que estén dentro de una carpeta `Resources` para que `WorldMap.cs` pueda cargarlas dinámicamente.

## 2. Configuración Automática de la Escena

Gracias a las modificaciones en `GameBootstrapper.cs`, la configuración es ahora automática. Solo necesitas crear una escena básica y añadir un GameObject con el script `GameBootstrapper`. El sistema creará dinámicamente todos los componentes necesarios (Tierra, luz solar, cámara, UI, etc.).

### 2.1. Crear la Escena Principal

**Opción Automática (Recomendada):**
1. En Unity, ve a **Tools > Create Main Scene** (este menú se añade automáticamente con el script proporcionado).
2. Esto creará una nueva escena con el `GameBootstrapper` ya configurado y la guardará en `Assets/Scenes/MainScene.unity`.

**Opción Manual:**
1. En Unity, ve a **File > New Scene**.
2. Crea un GameObject vacío (Clic derecho > Create Empty) y nómbralo `GameBootstrapper`.
3. Adjunta el script `GameBootstrapper.cs` al GameObject.
4. Guarda la escena en `Assets/Scenes/MainScene.unity`.

5. Presiona **Play** para ejecutar. El `GameBootstrapper` inicializará todo automáticamente. La UI es opcional; si no se crea, la información de tiempo se muestra en la consola.

**Nota:** Si deseas UI visual, crea manualmente un Canvas con dos objetos Text (DateTimeText y SpeedText) y un UIManager en un panel, asignando las referencias en el Inspector.

**Nota:** Si ya existen GameObjects en la escena (como una Main Camera o Directional Light por defecto), el script los reutilizará. De lo contrario, los creará.

### 2.2. Componentes Creados Automáticamente

- **Esfera de la Tierra**: Con radio 10, material estándar y texturas mensuales cargadas dinámicamente.
- **Luz Solar**: Directional Light con `SunController` para ciclo día/noche.
- **Cámara Principal**: Con `MapCameraController` para controles de zoom y rotación estilo Google Earth.
- **UI**: Opcional. Si se crea manualmente un Canvas con UIManager, muestra fecha/hora y velocidad. De lo contrario, la información se muestra en la consola (Debug.Log).
- **TimeManager**: Sistema de tiempo que avanza automáticamente.

Si necesitas personalizar (ej. radio de la Tierra, posiciones iniciales), ajusta los valores en los scripts o en el Inspector después de que se creen.

## 3. Configuración de Materiales y Shaders

El script `WorldMap.cs` asigna la textura principal (`_MainTex`) del material de la esfera de la Tierra. Para transiciones suaves entre texturas mensuales, se recomienda un shader personalizado.

**Shader Recomendado (Concepto):**

Para una transición suave entre texturas mensuales, necesitará un shader que pueda mezclar dos texturas basándose en un valor de `blend` (0-1). Unity tiene shaders de ejemplo o puede crear uno con Shader Graph.

**Pasos para un Shader de Mezcla de Texturas (ejemplo con URP/HDRP Shader Graph):**

1.  Cree un nuevo Shader Graph (Clic derecho > Create > Shader Graph > URP/HDRP > Lit Shader Graph).
2.  Abra el Shader Graph y añada dos nodos `Sample Texture 2D`.
3.  Conecte los nodos `Texture 2D` a un nodo `Lerp` (Linear Interpolate).
4.  Exponga una propiedad `_Blend` (Float) y conéctela al puerto `T` del nodo `Lerp`.
5.  Conecte la salida del `Lerp` al puerto `Base Color` del `PBR Master`.
6.  Guarde el shader y cree un nuevo material (Clic derecho > Create > Material) usando este shader.
7.  Asigne este material al `Mesh Renderer` de su GameObject `Earth`.

**Ajustes en `WorldMap.cs` para el Shader:**

Si usa un shader de mezcla, necesitará modificar `UpdateEarthTexture` en `WorldMap.cs` para asignar las dos texturas y el valor de `_Blend` al material. Por ejemplo:

```csharp
// Dentro de UpdateEarthTexture
_earthMaterial.SetTexture("_Texture1", _monthlyTextures[currentMonthIndex]);
_earthMaterial.SetTexture("_Texture2", _monthlyTextures[nextMonthIndex]);
_earthMaterial.SetFloat("_Blend", dayOfMonthProgress); // dayOfMonthProgress sería el progreso del día en el mes
```

Actualmente, `WorldMap.cs` solo asigna `_MainTex`. Para usar un shader de mezcla, deberá adaptar el script para establecer las propiedades `_Texture1`, `_Texture2` y `_Blend` de su shader personalizado.

## 4. Configuración de la Luz Solar en Unity

La `Directional Light` en Unity representa una fuente de luz infinita, ideal para simular el sol. `SunController.cs` se encarga de su rotación.

1.  **Tipo de Luz:** Asegúrese de que el componente `Light` de su GameObject `Directional Light` sea de tipo `Directional`.
2.  **Modo de Renderizado:** Para un rendimiento óptimo y efectos de iluminación global, considere usar `Mixed` o `Realtime` para su `Directional Light`.
3.  **Sombras:** Configure las sombras según sea necesario (ej. `Soft Shadows` para un aspecto más realista).
4.  **Skybox:** Utilice un `Skybox` que cambie con el ciclo día/noche para complementar la iluminación. Puede usar un `Procedural Skybox` o uno personalizado que responda a la hora del día.

## 5. Orden de Inicialización (GameBootstrapper)

El script `GameBootstrapper.cs` está diseñado para asegurar que todos los componentes esenciales se inicialicen en el orden correcto. Se ejecuta en `Awake()` y `Start()`.

*   **Awake():** En esta fase, `GameBootstrapper` busca o crea instancias de `TimeManager`, `WorldMap`, `SunController`, `MapCameraController` y `UIManager`. Esto garantiza que los Singletons estén disponibles y que las referencias cruzadas se establezcan antes de que otros scripts intenten acceder a ellos.
*   **Start():** Después de que todos los `Awake()` se hayan completado, `GameBootstrapper` llama a métodos de inicialización específicos como `mapCameraController.InitializeCameraPosition()` para asegurar que la cámara se posicione correctamente una vez que `WorldMap` esté completamente configurado.

**Recomendación:** Coloque el script `GameBootstrapper.cs` en la configuración de Script Execution Order de Unity para que se ejecute antes que los demás scripts principales (`TimeManager`, `WorldMap`, etc.). Vaya a `Edit > Project Settings > Script Execution Order` y añada `GameBootstrapper` con un valor negativo (ej. `-100`).

## 6. Ajustes Adicionales

*   **Post-Processing:** Considere añadir un volumen de post-procesado a su escena para mejorar la calidad visual, incluyendo efectos como `Bloom`, `Color Grading` y `Ambient Occlusion`.
*   **Optimización:** Para un videojuego, la optimización es clave. Asegúrese de que sus modelos 3D (especialmente la esfera de la Tierra) tengan un nivel de detalle (LOD) apropiado y que las texturas estén comprimidas correctamente.

Con estos pasos, su proyecto Unity debería tener un sistema robusto y visualmente atractivo para la simulación planetaria con tiempo e iluminación realistas.
