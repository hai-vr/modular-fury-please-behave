Modular Fury, Please Behave!
=====

Barges into Modular Avatar and VRCFury compatibility issues by turning VRCFury into a NDMF plugin using Harmony patching.

## What does this do?

VRCFury, and Modular Avatar, and NDM Framework are non-destructive processors, but when used in the same project, there may be execution order issues when entering Play Mode, or when building the avatar.

Due to VRCFury's backwards compatibility, it's very difficult to make it depend on other projects such as NDMF as part of its source code.

This project barges into this issue by forcibly turning VRCFury into a [NDM Framework plugin](https://ndmf.nadena.dev/index.html).

The user may choose to execute VRCFury before Modular Avatar, or the other way around.

- If VRCFury is executed before Modular Avatar, it is executed during the Generating phase, before `nadena.dev.modular-avatar`.
- If VRCFury is executed after Modular Avatar, it is executed during the Transforming phase, after `nadena.dev.modular-avatar`.

Using Harmony code patching, the following is done:

### In Play Mode

- Prevent `VF.VrcHooks.PreuploadHook.OnPreprocessAvatar` from running if `nadena.dev.ndmf.config.Config.ApplyOnPlay` is true (and return true in place indicating a success).
  - The invocation of `PreuploadHook.OnPreprocessAvatar` during Play Mode is generally originating from Avatars 3.0 Emulator.

- Prevent `VF.PlayModeTrigger.Rescan` from running if `nadena.dev.ndmf.config.Config.ApplyOnPlay` is true, but:
  - When NDMF is processing an avatar: 
    - Temporaily rig `VF.Builder.VFGameObject.GetRoots(Scene)` to return an array containing only the avatar being processed by NDMF (instead of all the avatars in the scene).
    - Internally call `VF.PlayModeTrigger.Rescan(Scene=null)` by ourselves when NDMF is processing an avatar.
      - `VF.PlayModeTrigger.Rescan` will internally call `VF.Builder.VFGameObject.GetRoots(null)`, which will return only that avatar.
  - If NDMF processes multiple avatars, `VF.PlayModeTrigger.Rescan` will be executed multiple times.

### During uploads

- Prevent `VF.VrcHooks.PreuploadHook.OnPreprocessAvatar` from running (and return true in place indicating a success), but:
  - When NDMF is processing an avatar:
    - Internally instantiate `VF.VrcHooks.PreuploadHook` and call `VF.VrcHooks.PreuploadHook.OnPreprocessAvatar` by ourselves.

- Prevent `VF.PlayModeTrigger.Rescan` from running.

### In both cases

- Add a NDMF Plugin that will execute VRCFury:
  - In Play Mode, execute a rigged version of `VF.PlayModeTrigger.Rescan` per-avatar. See ["In Play Mode"](#in-play-mode) above.
  - During uploads, instantiate `VF.VrcHooks.PreuploadHook` and invoke `VF.VrcHooks.PreuploadHook.OnPreprocessAvatar` with the avatar.

## License

As per [VRCFury's License](https://github.com/VRCFury/VRCFury/blob/main/com.vrcfury.vrcfury/LICENSE.md), this package is currently under the same license (CC BY-NC 4.0) as per *"Modify or use parts of VRCFury for NON-COMMERCIAL USE ONLY"* on the basis that this file may be considered reuse of VRCFury, even though most of the essence of this file is code patching.

> Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)
> https://creativecommons.org/licenses/by-nc/4.0/
> https://creativecommons.org/licenses/by-nc/4.0/legalcode
>
> Copyright (c) VRCFury Developers. All unlicensed rights reserved.
