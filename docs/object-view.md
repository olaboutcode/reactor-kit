# ObjectView colors

`Color` is a tint multiplied over the mesh. It does not replace the colors that came from the mesh. The default is white, `#FFFFFF`, so a `.glb` keeps the colors stored in the file.

`Mesh.Triangles`, `Mesh.Sphere`, and `Mesh.Box` have no color of their own. They stay white until the page sets `Color`. The sample paints the sphere:

```csharp
ObjectView()
    .Mesh(Mesh.Sphere(1f, slices: 48, stacks: 32))
    .Color(Color.FromArgb("#C8C2B8"))
```

Setting `Color` on a loaded file tints every part of that file.

## File colors

A `.glb` is drawn as one group per base color. A group is the primitives that share a `baseColorFactor` and a `baseColorTexture`. A factor is already linear. RGB is the group color. A missing factor is white.

The page tint is converted from sRGB to linear. The base-color image is stored as sRGB and sampled in linear. A missing image samples white. JPEG and PNG are read from the `.glb`, from a `data:` URI, or from a file next to the `.glb`. The mesh uses `TEXCOORD_0`.

```
albedo = tintLinear * groupColor * baseColor
```

## Shading

Light stays the view-space direction `(0.4, 0.8, 0.6)`. The shader works in linear color.

```
ndotl = saturate(dot(normal, lightDir))
halfVector = normalize(lightDir + float3(0, 0, 1))
specular = pow(saturate(dot(normal, halfVector)), 32) * 0.25
rgb = albedo * (0.25 + 0.75 * ndotl) + specular
```

Ambient is `0.25`, diffuse is `0.75`, and the highlight is white. The same numbers go in the Metal, GLSL, and HLSL shaders. The shader writes linear RGB. The surface stores sRGB.

`Background` is only the clear color. It is converted from sRGB to linear the same way, and it is not multiplied into the mesh. Alpha `0` shows the page behind the view. Alpha `255` makes the surface opaque.

Alpha on `Color`, the alpha of a `baseColorFactor`, and the alpha of the base-color image do not fade the object. The mesh is opaque.

## Ignored

Vertex colors (`COLOR_0`), metallic, roughness, normal maps, and UV sets other than `TEXCOORD_0` are not read. A model that has vertex colors, a white factor, and no base-color image draws white, tinted by `Color`.

| Member | Default | Role |
|---|---|---|
| `Color` | `#FFFFFF` | sRGB tint. Multiplied over every group. |
| `Background` | transparent | sRGB clear color. Does not tint the mesh. |
