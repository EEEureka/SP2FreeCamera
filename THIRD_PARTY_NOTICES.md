# Third-party notices

The deployable release archive redistributes the unmodified official Windows x64 runtime from
BepInEx `5.4.23.5`. Its source archive is pinned by SHA-256 in the packaging scripts.

Included components:

- BepInEx 5.4.23.5 — MIT — https://github.com/BepInEx/BepInEx/tree/v5.4.23.5
- UnityDoorstop 4.5.0 — LGPL-2.1 — https://github.com/NeighTools/UnityDoorstop/tree/v4.5.0
- HarmonyX 2.9.0 — MIT — https://github.com/BepInEx/HarmonyX/tree/v2.9.0
- MonoMod 22.01.29.01 — MIT — https://github.com/MonoMod/MonoMod/tree/v22.01.29.01
- Mono.Cecil 0.10.4 — MIT — https://github.com/jbevain/cecil/tree/0.10.4

The corresponding license texts are stored in `third_party/licenses` in the repository and in
the `licenses` directory inside each release archive. UnityDoorstop remains a separate,
replaceable dynamic library (`winhttp.dll`); its exact upstream source is available at the link
above.

SimplePlanes 2 and Unity files are not redistributed by this repository.
