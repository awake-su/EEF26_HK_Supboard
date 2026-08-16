# Custom Function ports

Имя функции: `AwakeHDRPTriplanarTerrain`. Precision: `Float`.

## Inputs

| Port | Type | Default / binding |
|---|---|---|
| SandAlbedo | Texture 2D | property `_SandAlbedo` |
| SandNormal | Texture 2D | property `_SandNormal` |
| SandMask | Texture 2D | property `_SandMask` |
| GrassAlbedo | Texture 2D | property `_GrassAlbedo` |
| GrassNormal | Texture 2D | property `_GrassNormal` |
| GrassMask | Texture 2D | property `_GrassMask` |
| RockAlbedo | Texture 2D | property `_RockAlbedo` |
| RockNormal | Texture 2D | property `_RockNormal` |
| RockMask | Texture 2D | property `_RockMask` |
| PositionWS | Vector 3 | Position node, World |
| PositionOS | Vector 3 | Position node, Object |
| NormalWS | Vector 3 | Normal Vector node, World |
| NormalOS | Vector 3 | Normal Vector node, Object |
| UseObjectSpace | Float | 0 |
| SandTiling | Float | 0.25 |
| GrassTiling | Float | 0.25 |
| RockTiling | Float | 0.25 |
| TriplanarSharpness | Float | 4 |
| SandNormalStrength | Float | 1 |
| GrassNormalStrength | Float | 1 |
| RockNormalStrength | Float | 1 |
| SandHeight | Float | 1 |
| SandHeightBlend | Float | 0.5 |
| GrassHeight | Float | 3 |
| GrassHeightBlend | Float | 4 |
| RockSlopeStart | Float | 0.35 |
| RockSlopeBlend | Float | 0.1 |

## Outputs

| Port | Type |
|---|---|
| BaseColor | Vector 3 |
| NormalWorld | Vector 3 |
| Metallic | Float |
| Occlusion | Float |
| Smoothness | Float |
| LayerWeights | Vector 3 |

`LayerWeights`: R=Sand, G=Grass, B=Rock; можно временно подключать к Base Color
для отладки переходов.
