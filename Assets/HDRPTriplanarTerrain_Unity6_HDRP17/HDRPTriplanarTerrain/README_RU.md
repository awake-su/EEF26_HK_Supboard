# HDRP Triplanar Terrain — Unity 6 / HDRP 17

Трёхслойный материал без UV: песок, трава и камень. Проекция трипланарная,
смешивание идёт по мировой/локальной высоте и наклону поверхности.

## Подключение

1. Скопируйте папку `Assets/Awake/HDRPTriplanarTerrain` в `Assets` проекта.
2. В Unity создайте `Shader Graph > HDRP > Lit Shader Graph`.
3. Добавьте `Custom Function`:
   - Type: `File`;
   - Source: `HDRPTriplanarTerrain.hlsl`;
   - Name: `AwakeHDRPTriplanarTerrain`.
4. Создайте входы и выходы по таблице `ShaderGraph_Ports.md`.
5. Подключите выходы к HDRP Lit Master Stack:
   - `BaseColor` → Base Color;
   - `NormalWorld` → Normal (World Space);
   - `Metallic` → Metallic;
   - `Occlusion` → Ambient Occlusion;
   - `Smoothness` → Smoothness.
6. Сохраните граф, создайте Material и назначьте девять текстур.

Для статичного острова оставьте `Use Object Space = 0` — это World Space и
разумный дефолт. Для движущегося меша установите `1`, чтобы рисунок двигался
вместе с объектом.

## Импорт текстур

- Albedo: обычная sRGB-текстура.
- Normal: Texture Type = `Normal map`.
- Mask Map: sRGB выключен; каналы HDRP: R=Metallic, G=AO, B=Detail Mask,
  A=Smoothness.
- На всех картах включите Wrap Mode = `Repeat`.

`Sand Height` и `Grass Height` измеряются в единицах выбранного пространства.
Если объект стоит далеко от нулевой мировой высоты, удобнее включить Object
Space либо выставить пороги в его мировых координатах.

Кнопка `Tools > Awake > HDRP Triplanar Terrain > Validate Selection` проверяет,
что у выбранного материала заполнены все девять текстурных слотов.
