# Fish Jam Fever Guidelines

## Project organization

- Keep project work under `Assets/_Game` and use the existing folder structure where practical. Avoid creating unrelated top-level folders.
- Keep each script under 300 lines where practical. Split by responsibility rather than splitting solely to meet a line count.
- Use clear, descriptive names for variables, methods, and types. Prefer explicit types over `var` when the type is not obvious.

## Unity and C# conventions

- Cache frequently used component references and `transform` when it improves clarity or avoids repeated lookups. Cache components used in trigger callbacks rather than calling `GetComponent` repeatedly.
- Prefer `CompareTag` over comparing `gameObject.tag` strings.
- Do not use object names to drive gameplay logic.
- Avoid `Find`, `FindObjectOfType`, `GetComponentInChildren`, and `GetComponentInParent` for routine runtime lookups. Prefer serialized references or explicit registration; scene initialization lookups should be justified.
- Avoid `Renderer.material`/material-instantiating accessors in gameplay code. Use shared materials, property blocks, or an established project helper as appropriate.
- Avoid direct hierarchy-index assumptions such as `transform.GetChild()` for gameplay logic. Use explicit references.
- Avoid comparing floats with `==`/`!=`; use a tolerance or an appropriate range check. For vectors, use a distance tolerance or squared-distance comparison rather than exact equality.
- Use `sqrMagnitude` for threshold/ranking checks when only squared distance is needed. `Vector3.Distance` is appropriate when the actual distance is needed; it does not eliminate the square-root calculation.
- Prefer `OnTrigger` to `OnCollision` when trigger semantics fit the mechanic. Avoid per-frame `OnTriggerStay`/`OnCollisionStay` when enter/exit events or explicit state are sufficient.
- Use pooling for frequently spawned gameplay objects and effects where it fits the lifetime and complexity of the object.
- Use `UIManager` for project UI flows. Disable raycast targeting on non-interactive UI graphics.
- Use `LateUpdate` for camera follow when it needs to run after target movement.
- Avoid negative scale for visual flipping; prefer an appropriate sprite/rendering flip property.
- Avoid empty Unity lifecycle and event methods. Remove them unless they are intentionally kept for a documented Unity/editor reason.
- Avoid unnecessary singletons; reserve them for genuinely global managers.
- Prefer constants or serialized configuration for repeated or meaningful strings and numeric tuning values. Do not introduce temporary variables for one-off literals without a readability or reuse benefit.
- Prefer direct component declarations/references when possible instead of storing a `GameObject` solely to retrieve a component later.
- Use one return per method only when it improves readability; do not contort control flow to satisfy this mechanically.
- Choose `for` or `foreach` based on clarity and the collection/API. Do not assume `for` is always faster; measure performance-sensitive code.
- Use `//TODO:`, `//FIXME:`, `//UNDONE:`, and `//TEST:` for their corresponding unfinished work, urgent fixes, incomplete sections, and temporary tests.

## Gameplay and review expectations

- For gameplay work, playtest the flow and check for bugs, folder placement, and design/implementation choices that look inconsistent or hard to maintain.
- Review code against the conventions above and report actionable findings with file and line references. If screenshots are useful and available, include them with comments; otherwise provide precise textual references.
- End reviews with one concise overall assessment and an encouraging note.

## Unity editor and performance checks

- Validate UI layout at 1080x1920 and 720x1600 where applicable.
- Consider Medium quality settings when checking behavior on a representative mid-range device.
- Mark only genuinely static objects as static. Enable GPU instancing for suitable repeated materials/objects after verifying compatibility.
- Choose texture compression based on platform, format, and alpha requirements (for example, an alpha-capable format for transparency); do not apply DXT formats indiscriminately.
- Use texture atlases and render textures only where they fit the asset/rendering workflow and show a measurable benefit.
- Prefer simpler colliders where they fit the required shape and gameplay accuracy; avoid MeshCollider for routine simple shapes.
- Use the Unity Profiler/Frame Debugger to investigate measured performance issues rather than assuming a setting is always beneficial.
