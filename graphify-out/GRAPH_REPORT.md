# Graph Report - BYOG_26  (2026-10-02)

## Corpus Check
- 59 files · ~78,239 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 155 file(s) not represented in the graph (top: .meta 105, .asset 32, .dll 5)

## Summary
- 2715 nodes · 5174 edges · 133 communities (75 shown, 58 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 292 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- vFolders File Management
- IMGUI Rendering & Layout
- vInspector Property Drawers
- vHierarchy Scene Tree
- Reflection & Utility Functions
- Unity Package Manifest
- Unity Package Manifest
- Reflection & Utility Functions
- vHierarchy Scene Tree
- vFolders File Management
- Editor Input & Event Handling
- Editor Input & Event Handling
- Editor Input & Event Handling
- vInspector Property Drawers
- vInspector Property Drawers
- Unity Package Manifest
- vHierarchy Scene Tree
- Vector2 Vector3
- vHierarchy Scene Tree
- vHierarchy Scene Tree
- IMGUI Rendering & Layout
- Unity Package Manifest
- Unity Package Manifest
- vInspector Property Drawers
- vInspector Property Drawers
- vFolders File Management
- vHierarchy Scene Tree
- vInspector Property Drawers
- vHierarchy Scene Tree
- vInspector Property Drawers
- Unity Package Manifest
- Unity Package Manifest
- Editor Input & Event Handling
- vInspector Property Drawers
- Unity Package Manifest
- Unity Package Manifest
- vFolders File Management
- vFolders File Management
- Unity Package Manifest
- Unity Package Manifest
- Unity Package Manifest
- vFolders File Management
- vFolders File Management
- IMGUI Rendering & Layout
- Unity Package Manifest
- IMGUI Rendering & Layout
- vHierarchy Scene Tree
- vFolders File Management
- vHierarchy Scene Tree
- vFolders File Management
- vInspector Property Drawers
- Vector2 Vector3
- Globalobjectid Idictionary
- vInspector Property Drawers
- vFolders File Management
- IMGUI Rendering & Layout
- Unity Package Manifest
- vFolders File Management
- vFolders File Management
- Globalobjectid Idictionary
- vFolders File Management
- Playmodestatechange Onplaymodeexit()
- Vector2 Vector3
- vHierarchy Scene Tree
- Unity Package Manifest
- Unity Package Manifest
- vInspector Property Drawers
- Dictionary Editorprefscached
- vInspector Property Drawers
- Unity Package Manifest
- vFolders File Management
- vFolders File Management
- vFolders File Management
- vInspector Property Drawers
- vInspector Property Drawers
- IMGUI Rendering & Layout
- vInspector Property Drawers
- Projectprefs Deletekey()
- vHierarchy Scene Tree
- Dictionary Collectionutils
- Projectprefs Deletekey()
- Projectprefs Deletekey()
- Unity Package Manifest
- Unity Package Manifest
- Dictionary Editorprefscached
- vInspector Property Drawers
- Unity Package Manifest
- Textutils Formatdistance()
- vHierarchy Scene Tree
- vInspector Property Drawers
- vFolders File Management
- vFolders File Management
- vHierarchy Scene Tree
- List Visualelement
- Textutils Formatdistance()
- vInspector Property Drawers
- IMGUI Rendering & Layout
- vFolders File Management
- List Serializeddictionary
- Gaussiankernel Array2D()
- Gaussiankernel Array2D()
- Unity Package Manifest
- IMGUI Rendering & Layout
- IMGUI Rendering & Layout
- IMGUI Rendering & Layout
- IMGUI Rendering & Layout
- Unity Package Manifest

## God Nodes (most connected - your core abstractions)
1. `VUtils` - 176 edges
2. `VUtils` - 170 edges
3. `VUtils` - 152 edges
4. `VHierarchy` - 68 edges
5. `VInspectorMenu` - 59 edges
6. `VHierarchyMenu` - 58 edges
7. `VFolders` - 55 edges
8. `VInspector` - 55 edges
9. `VGUI` - 50 edges
10. `VGUI` - 50 edges

## Surprising Connections (you probably didn't know these)
- `VFoldersController` --references--> `VFoldersGUI`  [EXTRACTED]
  Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersController.cs → Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersGUI.cs
- `VFoldersHistory` --references--> `VFoldersController`  [EXTRACTED]
  Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersHistory.cs → Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersController.cs
- `VFoldersNavbar` --references--> `VFoldersController`  [EXTRACTED]
  Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersNavbar.cs → Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersController.cs
- `VFoldersPaletteWindow` --references--> `VFoldersData`  [EXTRACTED]
  Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersPaletteWindow.cs → Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersData.cs
- `VFoldersHistory` --references--> `VFoldersGUI`  [EXTRACTED]
  Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersHistory.cs → Assets/Plugins/vFolders Fixed 6.3.7f1 unity/VFoldersGUI.cs

## Import Cycles
- None detected.

## Communities (133 total, 58 thin omitted)

### Community 0 - "vFolders File Management"
Cohesion: 0.16
Nodes (7): VInspector.Libs, VFolders.Libs, Project.Editor, VFolders, VHierarchy, VInspector, VHierarchy.Libs

### Community 1 - "IMGUI Rendering & Layout"
Cohesion: 0.04
Nodes (3): VUtils, allHierarchies, allProjectBrowsers

### Community 2 - "vInspector Property Drawers"
Cohesion: 0.06
Nodes (18): VInspectorMenu, attributesDisabled, collapseEverythingElseEnabled, collapseEverythingEnabled, componentAnimationsEnabled, componentWindowsEnabled, copyPasteButtonsEnabled, deleteEnabled (+10 more)

### Community 3 - "vHierarchy Scene Tree"
Cohesion: 0.06
Nodes (16): VHierarchyMenu, activationToggleEnabled, collapseEverythingEnabled, componentMinimapEnabled, deleteEnabled, focusEnabled, hierarchyLinesEnabled, isolateEnabled (+8 more)

### Community 4 - "Reflection & Utility Functions"
Cohesion: 0.04
Nodes (3): VUtils, allHierarchies, allProjectBrowsers

### Community 5 - "Unity Package Manifest"
Cohesion: 0.04
Nodes (56): com.unity.2d.sprite, com.unity.2d.tilemap, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+48 more)

### Community 6 - "Unity Package Manifest"
Cohesion: 0.04
Nodes (57): dependencies, com.coplaydev.unity-mcp, com.unity.2d.animation, com.unity.2d.aseprite, com.unity.2d.psdimporter, com.unity.2d.sprite, com.unity.2d.spriteshape, com.unity.2d.tilemap (+49 more)

### Community 7 - "Reflection & Utility Functions"
Cohesion: 0.04
Nodes (3): VUtils, allHierarchies, allProjectBrowsers

### Community 8 - "vHierarchy Scene Tree"
Cohesion: 0.06
Nodes (20): IconRow, iconCount, isCustom, isEmpty, VHierarchyPalette, colorsCount, VHierarchyPaletteWindow, cellSize (+12 more)

### Community 9 - "vFolders File Management"
Cohesion: 0.07
Nodes (14): VFoldersMenu, autoIconsEnabled, backgroundColorsEnabled, collapseEverythingElseEnabled, collapseEverythingEnabled, contentMinimapEnabled, foldersFirstEnabled, hierarchyLinesEnabled (+6 more)

### Community 10 - "Editor Input & Event Handling"
Cohesion: 0.04
Nodes (41): WrappedEvent, characted, clickCount, commandName, holdingAlt, holdingAltOnly, holdingAnyModifierKey, holdingCmd (+33 more)

### Community 11 - "Editor Input & Event Handling"
Cohesion: 0.04
Nodes (40): WrappedEvent, characted, clickCount, commandName, holdingAlt, holdingAltOnly, holdingAnyModifierKey, holdingCmd (+32 more)

### Community 12 - "Editor Input & Event Handling"
Cohesion: 0.05
Nodes (40): WrappedEvent, characted, clickCount, commandName, holdingAlt, holdingAltOnly, holdingAnyModifierKey, holdingCmd (+32 more)

### Community 13 - "vInspector Property Drawers"
Cohesion: 0.09
Nodes (10): EditorIcons, GUIColors, greyedOutTint, pressedButtonBackground, selectedBackground, windowBackground, VGUI, curEvent (+2 more)

### Community 14 - "vInspector Property Drawers"
Cohesion: 0.08
Nodes (4): VGUI, curEvent, isDarkTheme, lastRect

### Community 15 - "Unity Package Manifest"
Cohesion: 0.05
Nodes (38): com.unity.2d.sprite, com.unity.2d.tilemap, com.unity.modules.androidjni, com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.director, com.unity.modules.imageconversion (+30 more)

### Community 16 - "vHierarchy Scene Tree"
Cohesion: 0.07
Nodes (22): AddIconWindow, cellSize, iconSize, iconSpacing, paddingLeft, paddingRight, rowHeight, AdjustColorsWindow (+14 more)

### Community 18 - "vHierarchy Scene Tree"
Cohesion: 0.08
Nodes (3): VHierarchy, allHierarchies, cache

### Community 19 - "vHierarchy Scene Tree"
Cohesion: 0.09
Nodes (10): name, SceneEntry, sceneName, VHierarchySceneSelectorWindow, dividerHeight, firstRowOffsetTop, gaps, rowHeight (+2 more)

### Community 20 - "IMGUI Rendering & Layout"
Cohesion: 0.08
Nodes (3): ReorderableRow, animatingItemMovement, gaps

### Community 21 - "Unity Package Manifest"
Cohesion: 0.07
Nodes (34): dependencies, depth, source, url, version, dependencies, depth, source (+26 more)

### Community 22 - "Unity Package Manifest"
Cohesion: 0.06
Nodes (32): dependencies, depth, source, version, dependencies, depth, source, version (+24 more)

### Community 23 - "vInspector Property Drawers"
Cohesion: 0.07
Nodes (8): VInspector, allInspectors, deleteAnimation_lerpSpeed, deleteAnimation_speedLimit, expandAnimation_lerpSpeed, expandAnimation_speedLimit, expandAnimation_unqueueAtDistance, hoveredComponent

### Community 24 - "vInspector Property Drawers"
Cohesion: 0.09
Nodes (13): Button, isExpanded, Foldout, isExpanded, StaticInspector, Tab, selectedSubtab, selectedSubtabIndex (+5 more)

### Community 25 - "vFolders File Management"
Cohesion: 0.10
Nodes (12): VFoldersPaletteWindow, cellSize, data, hoveredBackground, iconSize, iconSpacing, paddingX, paddingY (+4 more)

### Community 26 - "vHierarchy Scene Tree"
Cohesion: 0.09
Nodes (9): Bookmark, assetPath, go, isDeleted, isLoadable, GameObjectData, SceneData, VHierarchyData (+1 more)

### Community 27 - "vInspector Property Drawers"
Cohesion: 0.16
Nodes (17): RuleAttribute, ButtonAttribute, DisableIfAttribute, EnableIfAttribute, EndFoldoutAttribute, EndIfAttribute, EndTabAttribute, FoldoutAttribute (+9 more)

### Community 28 - "vHierarchy Scene Tree"
Cohesion: 0.13
Nodes (4): ExpandQueueEntry, VHierarchyController, animatingExpansion, gui

### Community 29 - "vInspector Property Drawers"
Cohesion: 0.12
Nodes (5): name, VInspectorNavbar, bookmarkWidth, gaps, iconSize

### Community 31 - "Unity Package Manifest"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 32 - "Unity Package Manifest"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 33 - "Editor Input & Event Handling"
Cohesion: 0.11
Nodes (10): EditorIcons, GUIColors, greyedOutTint, pressedButtonBackground, selectedBackground, windowBackground, VGUI, curEvent (+2 more)

### Community 35 - "Unity Package Manifest"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 36 - "Unity Package Manifest"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 37 - "vFolders File Management"
Cohesion: 0.16
Nodes (4): ExpandQueueEntry, VFoldersController, animatingExpansion, gui

### Community 39 - "Unity Package Manifest"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, url, version, dependencies, depth, source (+16 more)

### Community 41 - "Unity Package Manifest"
Cohesion: 0.09
Nodes (23): dependencies, depth, source, version, dependencies, depth, source, version (+15 more)

### Community 43 - "Unity Package Manifest"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, url (+14 more)

### Community 44 - "vFolders File Management"
Cohesion: 0.12
Nodes (3): VFoldersGUI, controller, isTwoColumns

### Community 46 - "IMGUI Rendering & Layout"
Cohesion: 0.22
Nodes (3): MinMaxSliderDrawer, SerializedDictionaryDrawer, VariantsDrawer

### Community 47 - "Unity Package Manifest"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, url, version, dependencies, depth, source (+13 more)

### Community 50 - "vHierarchy Scene Tree"
Cohesion: 0.13
Nodes (6): VHierarchyNavbar, bookmarkSpacing, bookmarkWidth, controller, iconSize, iconSpacing

### Community 51 - "vFolders File Management"
Cohesion: 0.19
Nodes (6): VFoldersNavbar, animatingBookmarks, controller, gaps, isCompactMode, isTwoColumns

### Community 55 - "vInspector Property Drawers"
Cohesion: 0.13
Nodes (7): Editor, Editor, Editor, AbstractEditor, useUITK, ScriptableObjectEditor, ScriptEditor

### Community 58 - "Globalobjectid Idictionary"
Cohesion: 0.11
Nodes (9): GlobalID, fileId, globalObjectId, guid, idType, isAsset, isNull, isSceneObject (+1 more)

### Community 59 - "vInspector Property Drawers"
Cohesion: 0.29
Nodes (3): ExpandAnimation, collapsedInspectorHeight, targetInspectorHeight

### Community 60 - "vFolders File Management"
Cohesion: 0.14
Nodes (6): Bookmark, isDeleted, name, FolderData, VFoldersData, storeDataInMetaFiles

### Community 61 - "IMGUI Rendering & Layout"
Cohesion: 0.15
Nodes (7): AddIconWindow, cellSize, iconSize, iconSpacing, paddingLeft, paddingRight, rowHeight

### Community 63 - "Unity Package Manifest"
Cohesion: 0.12
Nodes (17): dependencies, dependencies, depth, source, url, version, depth, dependencies (+9 more)

### Community 64 - "vFolders File Management"
Cohesion: 0.14
Nodes (3): FolderStateChangeDetector, allBrowsers, cache

### Community 66 - "Globalobjectid Idictionary"
Cohesion: 0.12
Nodes (7): GlobalID, fileId, globalObjectId, guid, isAsset, isNull, isSceneObject

### Community 67 - "vFolders File Management"
Cohesion: 0.12
Nodes (14): VFoldersPaletteEditor, animatingCrossIcon, cellSize, curFirstEnabledRow, disabledRowTint, draggedRowBackground, hoveredRowBackground, iconSize (+6 more)

### Community 68 - "Playmodestatechange Onplaymodeexit()"
Cohesion: 0.12
Nodes (9): GlobalID, fileId, globalObjectId, guid, idType, isAsset, isNull, isSceneObject (+1 more)

### Community 71 - "Unity Package Manifest"
Cohesion: 0.13
Nodes (16): dependencies, depth, dependencies, depth, source, url, version, source (+8 more)

### Community 72 - "Unity Package Manifest"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, url, version, dependencies, depth, source (+8 more)

### Community 75 - "vInspector Property Drawers"
Cohesion: 0.21
Nodes (3): GameObjectInfo, ObjectInfo, RuleAttribute

### Community 76 - "Unity Package Manifest"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 77 - "vFolders File Management"
Cohesion: 0.20
Nodes (3): FolderState, TextureData, VFoldersCache

### Community 78 - "vFolders File Management"
Cohesion: 0.18
Nodes (6): TreeState, VFoldersHistory, controller, gui, isTwoColumns, VFoldersHistorySingleton

### Community 79 - "vFolders File Management"
Cohesion: 0.21
Nodes (5): IconRow, iconCount, isCustom, VFoldersPalette, colorsCount

### Community 81 - "vInspector Property Drawers"
Cohesion: 0.19
Nodes (3): VInspectorComponentWindow, isDragged, useUITK

### Community 82 - "IMGUI Rendering & Layout"
Cohesion: 0.18
Nodes (3): GaussianKernel, sigma, size

### Community 85 - "vInspector Property Drawers"
Cohesion: 0.15
Nodes (7): Bookmark, assetPath, isDeleted, isLoadable, obj, type, VInspectorData

### Community 93 - "Unity Package Manifest"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 94 - "Unity Package Manifest"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 98 - "Unity Package Manifest"
Cohesion: 0.18
Nodes (11): dependencies, depth, hash, source, version, dependencies, depth, source (+3 more)

### Community 100 - "vHierarchy Scene Tree"
Cohesion: 0.20
Nodes (3): Editor, VHierarchyDataComponent, SerializableDictionary

### Community 104 - "vFolders File Management"
Cohesion: 0.28
Nodes (3): Folder, name, path

### Community 110 - "vInspector Property Drawers"
Cohesion: 0.29
Nodes (3): AttributesState, BookmarkState, VInspectorState

### Community 113 - "vFolders File Management"
Cohesion: 0.29
Nodes (5): FolderDataWrapper, colorIndex, iconNameOrGuid, isColorRecursive, isIconRecursive

### Community 117 - "Gaussiankernel Array2D()"
Cohesion: 0.40
Nodes (3): GaussianKernel, sigma, size

### Community 118 - "Gaussiankernel Array2D()"
Cohesion: 0.40
Nodes (3): GaussianKernel, sigma, size

### Community 119 - "Unity Package Manifest"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 121 - "IMGUI Rendering & Layout"
Cohesion: 0.40
Nodes (5): GUIColors, greyedOutTint, pressedButtonBackground, selectedBackground, windowBackground

### Community 124 - "Unity Package Manifest"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.ai

## Knowledge Gaps
- **779 isolated node(s):** `histories_byWindow`, `cache`, `allBrowsers`, `path`, `name` (+774 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 1261 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **58 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `VUtils` connect `Reflection & Utility Functions` to `vFolders File Management`, `Editor Input & Event Handling`, `Playmodestatechange Onplaymodeexit()`, `Projectprefs Deletekey()`, `vHierarchy Scene Tree`, `vInspector Property Drawers`, `IMGUI Rendering & Layout`, `vFolders File Management`, `Vector2 Vector3`, `IMGUI Rendering & Layout`, `Fieldinfo Methodinfo`, `Gaussiankernel Array2D()`, `IMGUI Rendering & Layout`, `Dictionary Collectionutils`, `IMGUI Rendering & Layout`, `Getglobalid() Globalid`, `IMGUI Rendering & Layout`?**
  _High betweenness centrality (0.164) - this node is a cross-community bridge._
- **Why does `VUtils` connect `IMGUI Rendering & Layout` to `vFolders File Management`, `vFolders File Management`, `Globalobjectid Idictionary`, `Textutils Formatdistance()`, `vInspector Property Drawers`, `Fieldinfo Methodinfo`, `Dictionary Editorprefscached`, `vInspector Property Drawers`, `vInspector Property Drawers`, `Func Ienumerable`, `IMGUI Rendering & Layout`, `IMGUI Rendering & Layout`, `Projectprefs Deletekey()`, `IMGUI Rendering & Layout`, `Getglobalid() Globalid`, `Vector2 Vector3`, `Ensuredirexists() Ensuredirexistsandrevealinfinder()`?**
  _High betweenness centrality (0.146) - this node is a cross-community bridge._
- **Why does `VUtils` connect `Reflection & Utility Functions` to `vFolders File Management`, `Dictionary Editorprefscached`, `Vector2 Vector3`, `vFolders File Management`, `IMGUI Rendering & Layout`, `Textutils Formatdistance()`, `vInspector Property Drawers`, `Bounds Gameobject`, `Func Ienumerable`, `Gaussiankernel Array2D()`, `vInspector Property Drawers`, `Globalobjectid Idictionary`, `Fieldinfo Methodinfo`, `Projectprefs Deletekey()`, `IMGUI Rendering & Layout`?**
  _High betweenness centrality (0.096) - this node is a cross-community bridge._
- **What connects `histories_byWindow`, `cache`, `allBrowsers` to the rest of the system?**
  _779 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `IMGUI Rendering & Layout` be split into smaller, more focused modules?**
  _Cohesion score 0.0380517503805175 - nodes in this community are weakly interconnected._
- **Should `vInspector Property Drawers` be split into smaller, more focused modules?**
  _Cohesion score 0.05837173579109063 - nodes in this community are weakly interconnected._
- **Should `vHierarchy Scene Tree` be split into smaller, more focused modules?**
  _Cohesion score 0.05786090005844535 - nodes in this community are weakly interconnected._