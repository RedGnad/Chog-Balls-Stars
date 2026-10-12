Sept 30, 2024
Bot SDK 3.5.0 A1

* Visual Editor
- Changed top menu to "Tools/Quantum Bot SDK";
- Fixed issue in which double cliking an AI document asset without having the editor window opened would break the editor;
- Removed usage of Assets/Gizmos folder

* Utility Theory
- Added debugger to Utility Theory AI documents;
- Fixed issue in which Commitment and Rank fields would not be set to default after disconnecting its slots;

* Behaviour Tree
- BT Debugger now shows the progress of BTServices;


Sept 05, 2024
Bot SDK 3.4.8 A1

* Core
- Changed the Quantum.BotSDK.Core dll to make it possible to use Bot SDK in standalone projects;


Aug 13, 2024
Bot SDK 3.4.7 A1

* Visual Editor
- Fixed issue in which compiling an AI document whilst the export folder doesn't exist would break the compilation process;

* Behaviour Tree
- Fixed issue in which overriding BTNode.Init() and not calling base.Init() would break the internal BT state;
- Fixed issue in which the BTComposite nodes' Loaded callbacks would try to access possibly null collections;

* State Machine
- Fixed issue in which logical decision nodes (AND, OR, NOT) would not cleanup references to nodes baked on the previous compilation;


July 30, 2024
Bot SDK 3.4.6 A1

* Visual Editor
- Fixed sharing violation issue when opening the Bot SDK window for the first time;
- Moved the Bot SDK toolbar options from "Window" to "Tools";

* Bot SDK Core
- On Quantum.BotSDK.Core.dll, removed dependencies on UnityEngine.dll;

* AIConfig
- Added log message when trying to set a variable in AIConfig but the key is empty or null;


July 03, 2024
Bot SDK 3.4.5 A1

* AIConfig
- Fixed issue with AIConfig assets in which override versions could not be properly created;


July 02, 2024
Bot SDK 3.4.4 A1

* Visual Editor
- Fixed issue in which slots with AssetRef type would not be visible;


July 01, 2024
Bot SDK 3.4.3 A1

* Visual Editor
- Fixed issue in which the editor would not create nor open AI documents on Unity 2023 due to broken reference to dll;
- Changed default export folder to "Assets/QuantumUser/Resources/DB";


Jun 21, 2024
Bot SDK 3.4.2 A1

* Systems
- [BREAKING CHANGE] Changed the namespaces of the Bot SDK systems;
- Added [Preserve] attribute which is a requirement for Quantum 3 RC version;

* BT
- Fixed issue when trying to update a BTAgent without passing a pointer to a Blackboard component;
- Fixed issue in the debugger which would throw NRE exceptions when trying to debug a BTAgent;

* UT
- Fixed issue in the debugger which would throw NRE exceptions when trying to debug a BTAgent;
- Disabled the basic UT debugger;

* Unity Editor
- Removed outdated ways of creating AI documents from the context menu;


Jun 20, 2024
Bot SDK 3.4.1 A1

* Visual Editor
- Fixed issue in which the Bot SDK version file would not be properly created on the first time the addon is used;
- Changed the default export folder for compiled AI assets;


Jun 19, 2024
Bot SDK 3.4.0 A1

* GOAP
- [BREAKING CHANGE] Removed the GOAP simulation and editor code;

* BT
- [BREAKING CHANGE] Changed BTNode.Init() to receive the following parameters: BTParams and AIContext;

* Folders
- Moved Bot SDK location to "Assets/Photon/QuantumAddons/QuantumBotSDK";


May 22, 2024
Bot SDK 3.3.0 A1

* Visual Editor
- Cleaning up the editor selection when changing the graph being edited;
- Comment bubbles: pressing "Enter" does not save the text anymore, it creates a line breank instead (press Esc to save);
- Added possibility to create Response Curve nodes in more types of graph;

* Compiler
- Better error handling when a Blackboard or Constant Node is not present on the variables panel;
- Fixed issue when changing nodes types from the editor context menu which would not properly create a new asset with the new type (e.g when converting BT composite nodes);

* Utility Theory
- Fixed issue in assets caching which could lead to a desync on late joiners;
- Fixed issue in which every Consideration node would always be created from scratch and not re-use previously baked assets;

* HFSM
- Fixed issue in assets caching which could lead to a desync on late joiners;
- Changed from [HideInInspector] to [ExcludeFromPrototype] on the HFSMData struct;
- Removed unused "Prerequisite" variable from TransitionSets;

* Blackboard
- Fixed issue when trying to compile an AI document which referenced a missing asset in a Blackboard variable;
- Added TryGet methods

* BT
- Baking BT Service nodes nicknames into the created asset's Label field;
- Improved null pointer check when trying to clear the reactive decorators list;
- [BREAKING CHANGE] Renamed BTDecorator's method name from "DryRun" to "CheckConditions";


May 08, 2024
Bot SDK 3.2.1 A1

* Visual Editor
- Added [BotSDKTooltip] attribute which can be used in classes and fields;


Apr 23, 2024
Bot SDK 3.2.0 A1

* BT
- Fixed issue in Dynamic Decorators which would cause multiple nodes to run at the same time;
- Added BTSelectorRandom which randomly picks a single child node to be executed with chances evenly distributed;
- Fixed issue in which BTAgent code would try to de-allocate lists which were not yet allocated;

* UT
- Added back the OnComponentAdded/Removed signals to the BotSDKSystem;

* HFSM
- Fixed issue with the back-porting and baking of ANY Transitions, which would cause some transitions to not be baked
- Added import to HFSMData in the DSL so it can be used in other qtn files;

* Compiler
- Fixed issues when compiling AssetRefs;
- Added better error handling compiling AssetRefs;

* Visual Editor
- Fixed issue which would throw NRE when trying to handle Debug Points;
- Changed the referenced Unity dll to version 2019.4.28f1;
- Fixed issue which would cause the SettingsDatabase asset to be created from scratch;

* Debugger
- Fixed issues on the debugger enabled/disabled state which would causes issues when trying to run the game with Bot SDK open;
- Improved how debugger hierarchies are handled internally;


Mar 19, 2024
Bot SDK 3.1.0 A1

* Behaviour Tree
- Fixed issue in which BTServices would not be executed;
- Fixed issue in which BTServices would not be properly baked into assets when compiling the AI document;
- Changed BTParams.Frame variable type from FrameThreadSafe to FrameBase;
- Added [ExcludeFromPrototype] in most of BTAgent component fields;

* Bot SDK Systems
- Fixed issue in the BotSDKTimerSystem in which it would not write the FP current game time;

* AIConfig
- Changed its extension methods to use FrameBase instead of Frame;

* GOAP
- Added "import singleton component" on Unity, to import GOAPData;


Mar 14, 2024
Bot SDK 3.0.1 A1

* Compiler
- Fixed issue in which AIBlackboard and AIConfig assets would always be generated, even if not used;


Feb 28, 2024
Bot SDK 3.0.0 A1

* Bot SDK Core
- Added a new dll which contains all the Bot SDK core types and logic;
- Adapted all the assets and AssetRefs to function with the new Quantum 3 API;
- Changed how the Bot SDK internal Timer was located, moving it from Frame.Global to a Singleton Component;
- [BREAKING CHANGE] All the core methods on Bot SDK now use FrameBase instead of Frame;
- [BREAKING CHANGE] AIContext: added a new field called "UserData" which should be used for inserting the custom context struct;

* Visual Editor
- Reworked all the document compilers;
- Reworked the document debuggers;
- Adapted other algorithms to use new Quantum 3 API;

* Bot SDK Runtime
- Added, in Unity, only the necessary types;
- Adapted all the custom Unity code to work with the new Bot SDK Core dll;


Nov 09, 2023
Bot SDK 2.6.4 F1

* HFSM
- Fixed issue on the ANY Transition node Excluded List which would throw exceptions when pointing to already deleted State nodes;


Nov 07, 2023
Bot SDK 2.6.3 F1

* Visual Editor
- Fixed initialisation issues which could causa runtime errors when the game entered in Play Mode with a Bot SDK document open;

* HFSM
- Reworked the visuals of the ANY Transition node;
- Added alternative to toggle the ANY Transition node list to "Exclude List" or "Include List";

* Behaviour Tree
- Fixed issue when cleaning the BT Context data which could try to deallocate an unallocated collection;



Oct 10, 2023
Bot SDK 2.6.2 F1


* Visual Editor
- It is now possible to load types from external dlls using the SettingsDatabase asset;
- Forced the xml serializer to always write "\r\n" on new lines;
- Fixed issue where fields in SettingsDatabase would be set back to its default values;
- When a version upgrade is detected, an automatic fixer is started and the currently open documents are re-deserialized again;

* Behaviour Tree
- Minor adjustment in BTAgent.User to load the root node using "FrameThreadSafe" instead of "Frame"

* AIBlackboard
- Adjusted the Reactive Decorators section to use "FrameThreadSafe" instead of "Frame"

* Utility Theory
- Fixed issue with copy/paste of UT Considerations;
- Fixed issue where newly created Considerations would not have correct NodeFlags set;


Sept 06, 2023
Bot SDK 2.6.1 F3

* AI Config
- Added back the AIConfig editor code which was mistakenly commented out;


Sept 04, 2023
Bot SDK 2.6.0 F3

* Visual Editor
- Added a "rendering culling" which prevents far away nodes from being rendered as to increase overall editor performance;

* Utility Theory
- Added a "Collapse" button to the Consideration nodes top right corner which hides the Response Curves to improve rendering performance;
- Fixed issue with Considerations which has no Response Curves and were ignoring the Base Score value;

* AIBlackboard
- Fixed issue on blackboard component's Free method which would try to clear empty lists of Reactive Decorators;

* Debugger
- Fixed issue with the Mono Behaviour debugger class which would throw exceptions when trying to poll the AIBlackboard component;


Aug 24, 2023
Bot SDK 2.6.0 F2

* Utility Theory
- Added "Use Nested Momentum" which allows a Consideration to use it's nested Considerations highest Momentum instead of its own;
- Added "Cooldown Cancels Momentum" which allows the user to decide if the Cooldown should be applied right away even if such Consideration has Momentum;
- Fixed issue in which nested Considerations cooldown would only be ticked when its parent Consideration was chosen that tick;
- Fixed issue in which the Momentum would have its first tick happening too soon;
- Automatic slots check for Considerations when a UT document is open, as to update the contained slots when needed;

* Actions
- Added Frame Number and IsVerified to the DebugAction message;


Aug 08, 2023
Bot SDK 2.6.0 F1

* BT
- Fixed issue where BTServices with "RunOnEnter" would not run immediately when its owner node was entered, but rather would only run when bubbling up;
- Fixed issue where the first Reactive Decorator would not stop the reaction chain right away;
- Fixed issue where Abort Lower Priority would be executed even if the Reactive Decorator did not fail;
- Added AIContext and BTAbort type to the OnAbort() callback parameters;
- Abort Lower Priority now triggers the OnAbort() callbacks on the affected nodes;
- Changed the root node asset on the BTAgent component type from BTNode to BTRoot;
- The BT Root node cannot be copy/pasted anymore;

* Blackboard
- Added a reference to AIBlackboardInitializer on the AIBlackboard assets. The initializer reference is done automatically during compilation;
- Fixed issue where deleting the last Blackboard variable would result in the asset not being compiled again, causing the last deleted entry to still exist in asset;

* Visual Editor
- Fixed issue where the Bot SDK type loader would not differentiate AssetObject classes from other classes, which resulted in type clashing when compiling the AI document;


July 28, 2023
Bot SDK 2.5.1 F1

* BT
- Fixed issue on the debugger in which the debugged AI Document would not change if the BT asset was swaped;


July 25, 2023
Bot SDK 2.5.0 F1

* HFSM
- Added the concept of State History, used to resume the execution of a sub state machine from the last state it updated before exiting (opt-in configuration)
- Fixed issue where "ANY Transitions" would not allow State self-transition
- Action Root nodes cannot be copy/pasted anymore
- Removed "NextAction" code which was an obsolete concept

* BT
- [Minor Breaking Change] BTComposites "ChildCompletedRunning" now returns a bool value
- Fixed BTLoop code
- Fixed BTCooldown code
- Fixed edge case issues where not every OnExit callback would be triggered for a sequence of Decorator nodes
- Changes to the "BTForceResult" code

* Visual Editor
- Added "Reload Document" button on the toolbar which can be used to reload and AI document from the saved asset
- Removed the Create Asset menu for most of Bot SDK nodes as to cleanup to context menu on Unity

* Quantum Code
- Adjusted variable type definition in order to avoid compilation issues with Ride IDE


June 16, 2023
Bot SDK 2.4.0 F1

* Debugger
- [Breaking Change] The method used to register an entity for debugging (BotSDKDebuggerSystem.AddToDebugger) now also takes the Agent component as a parameter
- It is now possible to debug entities which uses the "Compound Agents" concept


June 06, 2023
Bot SDK 2.3.7 F1

* Blackboard
- [Breaking Change] Renamed the Initialize and Free methods
- Improved the Blackboard Entry finding mechanism as to boost performance
- Move the Blackboard Keys to the game state. The BB data is now stored in a Dictionary rather than a List. No need to repeat FindAsset internally anymore
- The Free method now also frees the Reactive Decorators list (BT-specific)

* Visual Editor
- Added a new miscelaneous: the [Required] Attribute which shows a compilation error when a Required field is not set

* Response Curve
- It's code can now Resolve the curve Input via any source (hand-set, blackboard, AIFunction, etc)

* Behaviour Tree
- [Breaking Change] Renamed the Initialize method
- Added BTManager.Free
- Removed outdated concept of Compound Agents as this is now done with shared code snippets


March 15, 2023
Bot SDK 2.3.6 F1

* Visual Editor
- Fixed issue where assets with multiple levels of inheritance would be wrongly generated


Feb 28, 2023
Bot SDK 2.3.5 F1

* Visual Editor
- Compiled assets will now have a shortened Guid added at it's name as a postfix
- Added a tool which automatically checks the XML files to fix them into a proper format. Applied automatically upon version upgrade
- Fixed issue on the Nodes Replacement tool where the replaced nodes would not be properly compiled
- Fixed issue where compiling the currently open AI document would also try to deserialise other AI documents, which could leads to issues if other documents thrown errors during this process

* Utility Theory
- Considerations now have their names baked into the Quantum assets
- Fixed issue on the duplication of Consideration nodes
- Fixed issue on the duplication of Response Curve nodes
- Fixed issue on the FPAnimationCurve baking where the PostWrapMode would be not baked

* Behaviour Tree
- Fixed issue where the AIContext was not passed into the Update pipeline


Dec 23, 2022
Bot SDK 2.3.4 F1

* AIFunction
- Can now have more than one output link

* Utility Theory
- Fixes and improvements on the Multiply Factor usage and on the Compensation Formula used internally

* Visual Editor
- Added functionality to re-generate all relevant Nodes' Guids on AI documents



Dec 15, 2022
Bot SDK 2.3.3 F1

* Visual Editor
- Fixed issue related to previously added "PreferBinarySerialization". It corrupted some AI documents, the attribute was removed
- Fixed the default Export Folder where Bot SDK assets are generated into

* Debugger
- Fixed issue related to entities being destroyed in runtime which would lead to runtime errors

* GOAP
- Fixed issue in which "GOAPDefaultAction" and "GOAPDefaultGoal" would not receive the AIContext parameter

* Blackboard
- Added "AIBlackboardComponent.TryGetID()"

* Simulation Code
- Wrapped the simulation Editor events in safer classes with proper try-catch handling as to prevent the simulation from breaking due to visual debugger exceptions



Sept 30, 2022
Bot SDK 2.3.2 F1

* Compiler
- Fixed issue where referenced assets would be compiled with wrong Guid

* Visual Editor
- Added BotSDKCategory attribute, used to group nodes in the Editor's search box
- Added a Node Replacement tool
- Added functionality to automatically fix broken nested assets on the Refresh Documents button



Sept 23, 2022
Bot SDK 2.3.1 F1

* Visual Editor
- It now creates AssetRef Constants when the user drag and drop Quantum assets into the editor window

* Compiler
- Throws [Bot SDK] Warning messages for Blackboard variables which are not used

* Assets
- Forcing the Serialization Mode of all Bot SDK Documents to Binary as to get proper XML formatting

* Utilities
- Moved the Pool utility to the Bot SDK namespace to avoid class naming conflicts



Sept 01, 2022
Bot SDK 2.3.0 F1

* Simulation Code
- Added AIContext in the HFSM, BT, GOAP and UT pipelines. It is a struct where user can insert contextual data

* HFSM Editor
- Every State Node now shows the amount of children States it has

* Debugger
- Removing automatically entries of entitis which were destroyed in the simulation


Jul 20, 2022
Bot SDK 2.2.4 F1

* Visual Editor
- Fixed issue related to trying to find an icon image that was removed from the SDK


May 19, 2022
Bot SDK 2.2.3 F1

* AIFunction
- Added base type for all possible AIFunction<QList<T>>. Custom made list AIFunctions should now inherit from these base classes


May 18, 2022
Bot SDK 2.2.2 F1

* AIFunction
- Added support to `AIFunction<QList<T>>`


May 17, 2022
Bot SDK 2.2.1 F1

* Visual Editor
- Added Sticky Notes feature, which are comment bubbles that are not tied to nodes


May 15, 2022
Bot SDK 2.2.0 F1

* Utility Theory
- [Breaking Change] Changed types of some Response Curve node fields
- Removed the usage of pre-defined static arrays on it's algorithm
- Added "Edit" and "Delete" options to the Right Click menu on Consideration nodes
- Fixed issue on Considerations' deletion which would still keep some metadata saved in the document

* Response Curves
- Added support to it on both the Visual Editor and on the simulation code, for all the SDK's AI types (HFSM, BT, GOAP and UT)
- Multiply Factor is now part of the Response Curve itself (so it can be used outside the context of the UT)
- Added optional Clamp01 field

* AIParam
- Added support to AIParam<Enum> when the Enum is defined as [Flags]

* Blackboard
- Removed Assert.Check which could cause GC Alloc issues in very old Quantum versions

* Visual Editor
- Changed the position of the "+" button on the left side panels (Blackboard, Config, etc)
- Added log messages to specify when a Document has duplicate Guid (probably duplicated with Ctrl+D)
- Fixed issue on the Behaviour Tree debugger which would throw exceptions in runtime

* HFSM
- Added new icon to muted State nodes on the left panel Hierarchy

* BT
- Fixed issue which would cause desyncs on games with late joiners

* Assets
- Fixed the AIConfigEditor code to better support the usage of Addressables


Jan 28, 2021
Bot SDK 2.1.4 F1

* Utility Theory
- Fixed GC Alloc on the UT's internal methods


Jan 14, 2021
Bot SDK 2.1.3 F1

* Behaviour Tree
- Added component: CompoundBTAgent
- BTManager.Update and Init APIs updated with optional "compound id" parameter


Dec 23, 2021
Bot SDK 2.1.2 F1

* Behaviour Tree
- Added a boolean field named "RunOnEnter" to every Service. It allows an early execution of Services regardless of their interval value


Dec 13, 2021
Bot SDK 2.1.1 F1

* Visual Editor
- Improved the Node Finder tool. It now shows a list of nodes based on a given name


Nov 29, 2021
Bot SDK 2.1.0 F1

* Quantum Code
- Bot SDK now supports the usage of FrameThreadSafe for users who want to run Bots in parallel threads


Nov 29, 2021
Bot SDK 2.0.8 F1

* Visual Editor
- Added support for Enums marked as [Flags]


Nov 26, 2021
Bot SDK 2.0.7 F1

* Visual Editor
- Added proper serialisation for the type Quantum.LayerMask


Nov 26, 2021
Bot SDK 2.0.6 F1

* Visual Editor
- Fixed issue with Quantum.LayerMask slot drawers


Nov 18, 2021
Bot SDK 2.0.5 F1

* Visual Editor and Quantum Code
- The AssetRef type is now supported on Blackboard, AIConfig, Constants, AIParam and AIFunction
- The String type is now supported on AIParam and AIFunction


Nov 16, 2021
Bot SDK 2.0.4 F1

* GOAP
- Fixed issue with Heuristic Cost delegate which would result in a desync during matches with Late Join

* AI Functions
- [BREAKING CHANGE] AIFunctions are now implemented with generic type
Please consult the Migration Notes for instructions on how to adapt to it
- The logic operator AI Functions now algo can get values from the Blackboard and hardcoded values

* Utility Theory
- Fixed an issue with the baking process of ResponseCurves

* Visual Editor
- Fixed issue where invalid types on a Slot would crash the editor
- Added usage of [Description] attribute on Enums

* Blackboard
- Fixed GC Alloc which was present on Blackboard's Getters and Setters


Oct 18, 2021
Bot SDK 2.0.3 F1

* Visual Editor
- Added "Node Finder" button, which highlights a node using it's Guid on the search
- Fixed issue where the serialized Zoom would be wrong due to incorrect string format


Oct 07, 2021
Bot SDK 2.0.2 F1

* GOAP
- Fixed small issue on the GOAPManager class which prevented some IDEs from compiling


Oct 07, 2021
Bot SDK 2.0.1 F1

* Visual Editor
- Added a different icon for AIParam slots


Oct 06, 2021
Bot SDK 2.0.0 F1

* GOAP
- [BREAKING CHANGE] Reworked the GOAP quantum code and Unity code. Old GOAP documents are not compatible with the new codebase

* Visual Editor
- Added "Open in script editor" alternative to nodes
- Expanded the clickable area on the left side menu to cover all of the horiozntal extension. There's no need to click specifically on the variables names anymore
- The "Compile All" button now shows a dialog box for the user to confirm the compilation
- The "Duplicate Document" button now shows a dialog box for the user to confirm the compilation
- Added Nested Comments
- Allowed the renaming of BTLeafs and AIFunctions

* HFSM
- Added invisible "+" button to "OR" and "AND" logicoperators, to add more inbound slots
- Added right click menu to all of the left side panels
- Added "Delete" alternative to the context menu on Transition Sets and Portal nodes
- It is now possible to select and delete links between nodes with the mouse left click
- Dragging a link to a Decision which already has a link, will make a replacement instead of throwing an error
- Reduced the width of State nodes
- Fixed issue where deleting Events would not erase that event's instances on the graph

* BT
- Added icons to BT main nodes. The icons can be customized/removed by the user by using the asset "BotSDKIconsDatabase"
- During debug, the Decorators will use color coding to show which ones failed/succeeded. Visible on the root level and on the subgraphs too

* UT
- The breadcrumbs now shows	the name of the Consideration node being edited
- Muted Curves are not shown on the Consideration nodes, on the root level
- Moved from (EXPERIMENTAL) to (BETA)

* Debugger
- It is now possible to open and use the debugger after the game has started, with no need to have it previously opened
- Added the "Debug Point" system, which allows users to create breakpoints and/or log messages when the nodes are entered. Currently available only for the HFSM and the BT
- Fixed issue related to running Utility Theory agents without an entity attached (i.e running it as Global)
- Fixed issues where the debugger instance would be null during runtime

* Blackboard
- Using non-alloc override of Quantum's Assert.Check
- Fixed drawing issue on the Blackboard asset. The fields labels were overlapping


Aug 29, 2021
- Bot SDK 2.3.2 Beta3

* Assets
- Added fixes necessary for users to create Bot SDK assets from non-abstract classes, and from generic classes


Aug 26, 2021
- Bot SDK 2.3.1 Beta3

* Bot SDK Timer System
- Fixed issue where the timer was losing accuracy and always returning time rounded to seconds


Aug 25, 2021
- Bot SDK 2.3.0 Beta3

* Behaviour Tree Code
- [BREAKING CHANGE] Integrated the BotSDKTimerSystem into the BTServices. Enable this system if you use services


Aug 24, 2021
- Bot SDK 2.2.0 Beta3

* Behaviour Tree Code
- Fixed issue with the Reactive Decorators, which had incorrect behaviour. Due do that, it could lead to inconsistent results when trying to abort part of the tree


Aug 17, 2021
- Bot SDK 2.1.0 Beta3
 
* Simulation code
- [BREAKING CHANGE] Added a centralized Timer solution which is precise and reacts correctly to changes on Frame.DeltaTime. This is now used by the WaitLeaf, from the BehaviourTree nodes, to count time, meaning that if you use this node, you will need to enable the BotSDKTimerSystem;
 
* Document Compiler
- Added a series of callbacks for when AI documents are compiled, which the user can use to automatize post-compilation steps;
 
* Behaviour Tree Editor
- It is now possible to convert from/to a BTSelector and BTSequence with the Right Click menu, without needing to delete and re-create all of the transitions;
 
* HFSM Editor
- Allowing the creation of AIFunctions on Transitions subgraphs;
 
 
Aug 05, 2021
- Bot SDK 2.0.8 Beta3
 
* Circuit Editor
- Added importer of AIFunctionEntityRef types;
 
 
Aug 02, 2021
- Bot SDK 2.0.7 Beta3
 
* Visual Editor
- Fixed serialisation issue which resulted in FPVector2/3 fields being reset upon domain reload
 
 
Jul 12, 2021
- Bot SDK 2.0.6 Beta3
 
* Behaviour Tree
- Rework on the BTParams. Added a new struct called BTParamsUser which can be extended by the user in order;
 
* Utility Theory
- Fixed issue triggered on Quantum 2.0.2, related to the serialisation of the UTMomentumData;
 
* Unity
- Fixed issue on the custom drawers of the Blackboard Initializers and the AIConfig assets;
 
 
Jul 08, 2021
- Bot SDK 2.0.5 Beta3
 
* Behaviour Tree
- Fixed issue related to the BT update wiping out user BTParams data in its internal code;
 
* Debugger
- Fixed issue in which a null reference exception would be thrown when adding entities to the Debugger with the Bot SDK window closed;
 
* Settings Database
- Fixed issue where the “Change Folder” and “Reset Folder” buttons were not setting the asset to dirty;
 
 
July 07, 2021
- Bot SDK 2.0.4 Beta3
 
* Behaviour Tree
- It is now possible for users to add fields into the BTParams class, in order to pre-cache data per-frame
- The BTManager class is now partial
- The BTManager has a partial method which allows for users to insert their own data disposal logic
- The BTParams is now a class, and it is also partial
 
 
June 22, 2021
- Bot SDK 2.0.3 Beta3
 
* Debugger
- Agents now need to be registered on the Debugger Window in order to show up there. On the simulation, use this:
BotSDKDebuggerSystem.AddToDebugger(entitiRef, (optional) customLabel);
- Added an Hierarchy scheme on the Bot SDK Debugger Window
 
* Circuit
- Clicking on an outboundslot which is already linked will now start the process of reconnecting the link instead of deleting it
 
* Blackboard Component
- Added assertions to the AIBlackboardComponent to better express errors related to Key mismatching when performing Get/Set on it
- Added back the TryGetEntryID method
 
 
June 16, 2021
- Bot SDK 2.0.2 Beta3
 
* Compiler
- Fixed issue where the AIFunction nodes would not be correctly compiled
 
 
June 15, 2021
- Bot SDK 2.0.1 Beta3
 
* Visual Editor
- It is now possible to edit Composite and Leaf node’s sub-graphs from the right click menu;
- Added API for overriding the label of debugged entries;
 
 
June 09, 2021
- Bot SDK 2.0.0 Beta3
 
* Utility Theory
- Added an Experimental Utility Theory AI with both Quantum simulation code and a visual editor on Unity;
 
* Behaviour Tree
- Optimisations on the Behaviour Tree Editor;
 
* Visual Editor
- Fixed issue with the nicknames (custom labels) given to nodes on the HFSM and on the BT;
- Pressing F2 now starts editing BT nodes;
- It is now possible to perform panning by holding Ctrl (Windows) or Command (MacOS) and dragging with LMB;
- Fixed warnings related to Blackboard and Config assets
 
* Func Nodes
- Added a solution for the definition of Func nodes, which returns some value based on user specific logic;
- Func Nodes available are ByteFunc, BoolFunc, IntFunc, FPFunc, FPVector2Func, FPVector3Func and EntityRefFunc;
- Func classes created on Quantum will be available as Nodes on the Visual Editor;
- Func Nodes can be linked with AIParam fields
 
 
 
Apr 27, 2021
- Bot SDK 2.1.10 Beta2
 
* Visual Editor
- Fixed issue related to a Unity bug introduced in a few recent versions. It resulted in errors when creating the scripable objects;
- Fixed an issue which resulted in duplicate asset Guid after compiling the AI;
- Fixed issue in which FPVector3 fields would have its value reset to zero everytime;
- The Searchbox now disappears when clicking on the Breadcrumb buttons;
- The values on the slots does not disappear anymore during Runtime;
- Added a context menu (with the Right Click) on the Blackboard, Constants and Config panels;
 
 
 
Apr 15, 2021
- Bot SDK 2.1.9 Beta2
 
* Visual Editor
- Fixed issue where hidden nodes could be selected and accidentaly deleted;
 
 
Apr 13, 2021
- Bot SDK 2.1.8 Beta2
 
* Serialization
- Updated Protobuf version to 2.4.6
 
 
Apr 08, 2021
- Bot SDK 2.1.7 Beta2
 
* Behaviour Tree
- Changing the fields from Leaf/Decorators/Service nodes on the quantum code will automatically show those fields on pre-existing nodes at the Unity Editor;
- Fixed issue where Composite/Leaf nodes would keep in memory even after deleted;
 
* Migration from v1 to v2
- Added functionality to convert from Quantum v1’s string Guids to Quantum v2’s long Guids;
 
 
Apr 06, 2021
- Bot SDK 2.1.6 Beta2
 
* Behaviour Tree
- It is now possible to rename Decorator and Service nodes;
- Composite and Leaf nodes now display a number which represents its execution order;
- Composite nodes now has an increased width;
 
 
Apr 01, 2021
- Bot SDK 2.1.5 Beta2
 
* Visual Editor
- Fixed issue with the HFSM Hierarchy debugger related to the state indicator arrow;
- Fixed issue where renaming Transitions would only have effect after domain reload;
- Fixed issue which happened when having an “ANY Transition” but no State on the same hierarchy level;
 
- It is now possible to delete Transitions by using the “Delete” key;
- The Debugger buttons doesn’t disappear during runtime anymore;
- Added messages for when toggling the Debugger active state;
- Removed duplicate “Make Constant” button from the Configs panel;
- Removed the right click panel from the Action Root nodes;
- The Actions Root node is now named “Actions - <StateName>”;
 
 
Feb 26, 2021
- Bot SDK 2.1.4 Beta2
 
* Visual Editor
- Fixed issue upon creating new AI documents;
 
 
Feb 24, 2021
- Bot SDK 2.1.3 Beta2
 
* Visual Editor
- Fixed issue in which closing and reopening AI documents would cause errors upon compilation and upon playing the scene with the Circuit window opened;
 
 
Feb 24, 2021
- Bot SDK 2.1.2 Beta2
 
* Visual Editor
- Fixed issue in which closing and reopening AI documents would lead to a crash in the Bot SDK window;
 
 
Feb 23, 2021
- Bot SDK 2.1.1 Beta2
 
* Visual Editor
- Fixed issue regarding Asset Refs fields in any node type;
 
 
Feb 16, 2021
- Bot SDK 2.1.0 Beta2
 
* Visual Editor
- Major improvements on the Circuit Editor performance;
- Blackboard Variables can now be converted to Constants and Config. On existing nodes, the “Key” slot connection is lost and the “Value” slot’s connection is maintained;
- Constants and Configs can now be converted to Blackboard Variables. On existing nodes, the “Key” is added and the “Value” slot’s connection is maintained;
- Fixed issue in which the button Duplicate Document was generating a new document with invalid Guids on its nodes;
- Added success message on the Circuit window when duplicating a document;
 
* Quantum code
- Added new API for resolving AIParams which only needs the Frame and the EntityRef as parameter;
 
 
 
Jan 19, 2021
- Bot SDK 2.0.0 Beta2
 
* Debugger
- The debugger window now also shows the HFSM/BT asset names alongside with the entity number;
 
* Behaviour Tree
- Added an Asset Ref to AIConfig on the BTAgent, and a “btAgent.GetConfig(frame)” helper method;
 
* Systems
- Created BotSDKDebuggerSystem, which is important for the Debugger on Unity;
 
* Circuit
- Fixed “Debugger Inspector” button on Dark Mode;
 
 
Jan 08, 2021
- Bot SDK 2.0.0 Beta1
 
* Debugger
- Created a custom inspector window for the Debugger. It is now possible to see all HFSMAgents and BTAgents to be debugged there, even if it doesn’t have a View game object;
 
* Behaviour Tree
- Added a debugger for the Behaviour Tree;
- Fixed issue in which Sequences and Selectors did not have their Status updated when canceled by a child node;
- Optimized the logic on the initial memory allocation on the BTAgent. Also improved the amount of data allocated;
- On BotSDKSystem, listening to component added/removed callbacks to initialize and free the BTAgent’s data;
 
* HFSM
- Added method overload for HFSMManager.TriggerEvent, which doesn’t needs a HFSMData* as parameter;
- On the debugger: the transition which shows many points is the most recent transition taken. That same transition is also blue, while the others are dark;
- On BotSDKSystem, listening to component added to initialize a HFSMAgent;
- Removed possibility of deleting the ActionRoot node (the main action node which lies inside States);
- Changed the label used upon asset creation from “State Machine” to “HFSM”;
- Fixed issue which happened when there were “ANYTransitions” but no State node existed;
- Fixed issue in which Actions couldn't receive more than one input (like from both OnEnter and OnUpdate at the same time);
 
* Blackboard
- On BotSDKSystem, listening to component removed to free the Blackboard memory;
 
* Systems
- Created the BotSDKSystem which listens to many callbacks to initialize/free components and data;
 
* DSL
- Removed many “asset import” from the DSL, which were no more needed;
 
 
 
Dec 21, 2020
- Bot SDK 2.0.0 Alpha9
 
* Assets
- BotSDK.unitypackage will not import "AssetLinkDatabase.asset" and "SettingsDatabase" anymore, to prevent issues on asset serialization on old Unity versions.
These assets are generated automatically when opening Bot SDK window.
 
 
Nov 17, 2020
- Bot SDK 2.0.0 Alpha8
 
* Bugfix
- Fixed an issue in which Blackboard/Constant Nodes would be created with the parent Guid
 
 
Nov 16, 2020
- Bot SDK 2.0.0 Alpha7
 
* Compilation
- Added a Boolean on the SettingsDatabase scripable object named "CreateBlackboardInitializer" which makes it possible to avoid creating the asset, if needed
- The Events baked into the HFSM assets are now ordered alphabetically
 
 
Oct 26, 2020
- Bot SDK 2.0.0 Alpha6
 
* Folders structure
- It is now possible to safely change Bot SDK's root folder
 
 
Oct 20, 2020
- Bot SDK 2.0.0 Alpha5
 
* Behaviour Tree Editor
- Release of the alpha version of our Behaviour Tree Editor, composed by:
- Deterministic Behaviour Tree implementation on Quantum solution
- Circuit editor on the Unity side
 
* HFSM
- Changed how “ANY Transitions” are compiled. Their priorities are now organized with the origin states transitions’ priorities
 
* Visual Editor
- Added “Duplicate Document” on Bot SDK window which creates a copy of the currently opened document
- Fixed issue regarding the fields serializer getting null during edit time
- Fixed issue on FP fields which could, in some specific scenarios, get corrupted during edit time
- Fixed issue that created null values during deserialization of field, which could lead the editor to crash
- Added MacOS command “CMD + Backspace” and “CMD + Delete” for deleting Nodes
- Added “Delete Node” button to the Nodes context menu
- The Save History is now provided with a Max save amount of 0. Possible to change via the asset SettingsDatabase, field SaveHistoryCount
 
 
Jun 16, 2020
- Bot SDK 2.0.0 Alpha4
 
* GOAP Editor
- Included the Blackboard panel
- Included the Constants panel
- Included the Configs panel
- Using assets cache to reduce the compile time
- Added complete solution to slots baker, to consider the Blackboards/Constants/Configs;
- Added a reference to an AIConfig on the GOAPAgent component
- Added Hotkey: press “F2” to edit Tasks
 
* Visual Editor
- On the History panel, highlighting the last compiled entry, and the active entry;
- Added “Compilation succeeded” message when Bot SDK finishes compiling;
- The left size panel is now resizable
- The compilation button now turns green when the current state of the circuit was already compiled;
- Support for the creation of AIParam<T> with an Enum as the type. Connections enabled with Blackboard/Constant/Config nodes
- Support for implicit cast on numerical nodes/slots. It is possible now to connect Byte/Int32/FP slots and nodes
- Added button which shows a panel with the most important Hotkeys
- Included panel for correcting Actions/Decisions types if they had their names changed
- The middle mouse button now closes tabs
- Fixed issue regarding duplicating existing Nodes, which was leading to duplicated Guid errors
- When Action/Decision nodes types are broken, they appear on the Left Panel upon compilation. Circuit window will focus on the broken node if the user click on the error message
 
* Quantum code
- HFSM and GOAP main methods doesn’t receive an AIContext as parameter anymore
- Added method GetConfig to the HFSMAgent component
- Removed the parameter “HFSMData* fsm” from the abstract Decide method
- Created method overloads for HFSMManager’s Init and Update methods. No need always to pass the HFSMData* anymore
- Added “asset import” declaractions to all Bot SDK files on its internal .qtn files. Needed for correct DB serialization on newer Quantum 2.0 versions
 
* Folders and files
- Changed the folder structure. It was previously on “Assets/Plugins/Circuit” and it was moved to “Assets/Photon/BotSDK”
- On Unity, changed the debugger script name from HFSMDebugComponent to HFSMDebugger
- Added panel to select the output folder for the result of Bot SDK’s compilation process. The Panel can be found at the bottom of the asset SettingsDatabase, the field name is BotSDKOutputFolder
- Config files are now generated on the subfolder AIConfig_Assets
 
Apr 28, 2020
- Bot SDK 2.0.0 Alpha3
 
* GOAP Editor
- Fixed issue regarding duplicated paths for Actions with the same type
- Fixed issue regarding duplicated paths for Tasks with the same name
- Fixed issue which prevented users to give custom names to Action nodes
 
* Visual Editor
- Fixed AssetRef drawer for Actions and Decisions fields
 
 
Apr 24, 2020
- Bot SDK 2.0.0 Alpha2
 
* Visual Editor
- Added Filter button on the top bar;
- Added “Refresh Documents” button to re-import all types on all Bot SDK documents at the project;
- Added Bot SDK version label to the top bar;
- Removed the “Promote to Variable” alternative from Action/Decision fields
 
* Issues
- Adapted Bot SDK dll to work with the new dlls structure present on Quantum SDK 2.0.0 A1 260 and on
 
Apr 16, 2020
- Bot SDK 2.0.0 Alpha
 
* Portability to Quantum 2.0.0 Alpha
- This version has no major change in comparison to version Bot SDK 1.0.0 Final. Only minor changes to make it work with Quantum 2.0
 
 
March 16, 2020
- Bot SDK 1.0.0 Final
 
* Visual Editor
- Support for adding Labels to Actions/Decision Nodes (from the right click menu)
- Allowed connection between Event Nodes and String/AIParamString slots
- Fixed the Constant Nodes drawer: its width is now defined by the size of the Node value instead of its name
- Removed log message when adding Constant nodes to the graph
- Fixed issue in which uninitialized AIParam fields would generate broken Nodes on the Visual Editor
- Fixed issue in which the compilation process tried to find for the Events on the Constants panel, which would lead to unnecessary logs showing up
- Fixed issues regarding the History view
- Fixed issue that happened when drag-and-dropping assets into AssetLink fields on Actions and Decisions
- Fixed issue in which the Priority values were initialized as null instead of zero
- Fixed issue with the “Compile Project” functionality
- Changed the tooltip from “Compile Project” to “Compile All”
- On the Unity top toolbar, changed the tool name from “Circuit” to “Bot SDK”
 
* Hotkeys
- Added Hotkey: press “M” to mute and unmute States/Actions/Transitions Links
- Added Hotkey: press “F2” to edit States/Actions/Decisions/Transitions Nodes/TransitionSets
- Added Hotkey: press “Esc” to go upper on the states hierarchy or out of a transiton graph
 
* Configs panel
- Created new panel on the left side menu named Config
This panel should be used for agents which use the same HFSM but need different constant values from each other
 
* Blackboard
- The Blackboard getter method is now generated automatically. Instead of Entity.GetBlackboardComponent(entity), use Entity.GetAIBlackboardComponent(entity)
- Fixed issue regarding destroying and re-initializing Blackboard components
 
* GOAP
- Fixed issue on muting Tasks
 
* Unity files
- Moved the AssetLinkDatabase and SettingsDatabase to a new folder. It is not on the Resources folder anymore
- Removed duplicated Gizmos Icon images
 
* Debugger
- Added support to debugging Blackboard Vars
- Event nodes can now be linked to String fields
- Fixed transition color when there is only one transition to be debugged
 
 
Dec 17, 2019
- Bot SDK 1.0.0 Release Candidate 3
 
* Visual Editor
- Changed the line drawers which are used to link states, actions, decisions, etc
- Keyboard numeric Enter is now also used to confirm some editing actions
- Clicking on some transition slot doesn't delete that transition anymore. Instead, drag and drop it to re-direct some transition to another state
- Right click transitions to access the Delete button
- Variables on the left side menu are now ordered by alphabetical order
- Actions and Decisions are also now ordered by alphabetical order
- Actions and Decisions nodes can now have its names changed on the Visual Editor (right click on it to edit). This makes no difference to the actual actions and decisions classes
- Created a menu to fix events/blackboard variables from former Bot SDK versions
- Added the transition Priority to the top view on the graph, on the state node
- No longer generates a Blackboard asset when there is no Blackboard Variable on the visual eitor
 
* ANY Transition
- Added Excluded List to it, which allows the user to inform states that should not consider that ANY transition
 
* Compilation
- Removed code which was causing the compilation process to take the double of the time needed
- Improved a lot the compilation time by re-using the previous data assets structure to prevent re-creating assets unnecessarily
 
* Debugging
- Added new toolset for debugging HFSM agents. Its main features are:
Select some Game Object on your Hierarchy tab which represents some HFSM Agent and has the HFSMDebugComponent added to it in order to open its HFSM editor
See what is the current state in which the HFSM Agent is
See what are the last three transitions taken by that HFSM Agent (works hierarchically)
See what are the current states on the left panel, to easily see all of the current states considering the hierarchy
 
* Quantum code
- Created the AIContext, which is used to reference Constants and Config data assets and can be set differently for bots which has the same HFSM but uses different values
- Changed the way that Transition Sets were handled on the HFSM core code
 
* Constants
- Created the Constants menu on the left side part of the editor
- Constants can be used to define nodes with a default type, and the same constants node can be used as input for many fields
 
* Blackboard
- On the Visual Eitor, added a new slot on every Blackboard node: the value slot, which can be used to define where some Action/Decision reads some value from
- Fixed serialization issue for entities which has the Blackboard Component and are Created -> Destroyed-> Created (respawned)
- Fixed serialization issue regarding using the Blackboard component during matches between different platforms (cross platform)
- Fixed serialization issue regardinsg usint the Blackboard component on replays
 
* AI Param
- Created a new type that easily changes the source from where it reads values: the AIParam
- Public AIParam fields can be defined, on the Visual Editor, from a value set by hand (click to edit), from a Blackboard node or a Constant node
- AIParams are strongly typed, so they are used as AIParamInt, AIParamFP, AIParamBool, etc
 
* Unity Assets
- Added new asset: HFSMDebugComponent
 
* Issues
- Fixed issue on using Events on ANY transitions
- Reduced the amount of SetDirty called for the data assets created, which was causing a bug during compilation on some Mac OS machines
- Added custom header on the default Bot SDK actions/decisions to prevent the Reset method to be created. It was also the cause of issues during compilation
- Fixed precision issues regarding closing/opening circuits with FP values. Every re-open would make the FP value change. FPs are now stored as their internal long value
- Fixed issue when (de)serializing the XML document. It didn't use InvariantCulture so it would cause errors depending on the machine's localization. Issue happened with some czech localized Windows machines 
- Fixed issue in which OnExit actions would be called right after entering some state which has children states
- Fixed compilation and runtime issues when some states were named the same
- Fixed nodes being in incorrect states if it was created as a result of drawing a new transition. It was generating broken nodes after copying and pasting it
- Documents are now saved before assembly recompilation to prevent loss of document modifications
- Fixed the Circuit window when the game is on Play mode
 
 
Aug 15, 2019
- Bot SDK 1.0.0 Release Candidate 2
 
* Visual Editor
- Mute Transition option, accessible by right clicking on a any transition's line or on
its in/outbound slot. Muted transitions will be ignored during the compilation process;
- Mute State option, accessible by right clicking on a State node. Muted states are
ignored during the compilation process, so no transition will lead to that state. The
HFSM will not compile if you mute the Default State;
- Mute Action option, accessible by right clicking on a Action node. It is possible to
mute any Action, no matter its position on the actions list. If a muted action is linked
to another action, then the other action will still be executed;
- Editor screen panning using direcional arrow keys;
- Actions and Decision nodes will automatically add or remove its fields list based on
changes performed on the quantum_code (such as adding/removing fields);
- Actions and Decisions nodes fields are initialized with the values defined on quantum_code;
- Warning logs if some Action/Decision isn't serialized. Those Actions/Decisions wont show on the pop up menu in this case;
- Actions/Decision fields always showing its values. There is no need to select the node to see the values anymore;
- Transitions are now labelled with their Event name if there is no custom label defined;
- Added menu to create Blackboard information within the editor view from which your
custom Blackboards are created and from where you can drag and drop the Blackboard keys as nodes.
 
* Blackboard
- Blackboard Initializer asset, which can be used to define initial values for blackboard instances;
- Fixed issue on Blackboard's memory allocation;
- Added optional compilation symbol (USE_BLACKBOARD_FRAME_METHODS) to define if the Blackboard will not use Frame's partial methods.
 
* Unity Assets
- Compiling a HFSM generates both a Blackboard asset and a BlackboardInitializer asset for that agent;
- Fixed redundancy between SettingsDatabase and AssetLinksDatabase assets which led to invalid cast errors;
- Downgraded all assets versions. Assets were previously generated from Unity 2019.1.0f2, now it is generated from Unity 2018.4.0f1 LTS.
 
* GOAP
- Reduced the amount of GOAP re-planning: it now happens when the agent's Goal changes and when the agent's Current State changes
 
* Changes on quantum_code BotSDK folder's content:
- Included the Samples folder with some very simple pre-defined Actions and Decisions
 
 
Jul 1, 2019
- Bot SDK 1.0.0 Release Candidate 1Jul 12, 2021
- Bot SDK 2.0.6 Beta3
 
* Behaviour Tree
- Rework on the BTParams. Added a new struct called BTParamsUser which can be extended by the user in order;
 
* Utility Theory
- Fixed issue triggered on Quantum 2.0.2, related to the serialisation of the UTMomentumData;
 
* Unity
- Fixed issue on the custom drawers of the Blackboard Initializers and the AIConfig assets;
 
 
Jul 08, 2021
- Bot SDK 2.0.5 Beta3
 
* Behaviour Tree
- Fixed issue related to the BT update wiping out user BTParams data in its internal code;
 
* Debugger
- Fixed issue in which a null reference exception would be thrown when adding entities to the Debugger with the Bot SDK window closed;
 
* Settings Database
- Fixed issue where the “Change Folder” and “Reset Folder” buttons were not setting the asset to dirty;
 
 
July 07, 2021
- Bot SDK 2.0.4 Beta3
 
* Behaviour Tree
- It is now possible for users to add fields into the BTParams class, in order to pre-cache data per-frame
- The BTManager class is now partial
- The BTManager has a partial method which allows for users to insert their own data disposal logic
- The BTParams is now a class, and it is also partial
 
 
June 22, 2021
- Bot SDK 2.0.3 Beta3
 
* Debugger
- Agents now need to be registered on the Debugger Window in order to show up there. On the simulation, use this:
BotSDKDebuggerSystem.AddToDebugger(entitiRef, (optional) customLabel);
- Added an Hierarchy scheme on the Bot SDK Debugger Window
 
* Circuit
- Clicking on an outbound slot which is already linked will now start the process of reconnecting the link instead of deleting it
 
* Blackboard Component
- Added assertions to the AIBlackboardComponent to better express errors related to Key mismatching when performing Get/Set on it
- Added back the TryGetEntryID method
 
 
 
June 15, 2021
- Bot SDK 2.0.1 Beta3
 
* Visual Editor
- It is now possible to edit Composite and Leaf node’s sub-graphs from the right click menu;
- Added API for overriding the label of debugged entries;
 
 
June 09, 2021
- Bot SDK 2.0.0 Beta3
 
* Utility Theory
- Added an Experimental Utility Theory AI with both Quantum simulation code and a visual editor on Unity;
 
* Behaviour Tree
- Optimisations on the Behaviour Tree Editor;
 
* Visual Editor
- Fixed issue with the nicknames (custom labels) given to nodes on the HFSM and on the BT;
- Pressing F2 now starts editing BT nodes;
- It is now possible to perform panning by holding Ctrl (Windows) or Command (MacOS) and dragging with LMB;
- Fixed warnings related to Blackboard and Config assets
 
* Func Nodes
- Added a solution for the definition of Func nodes, which returns some value based on user specific logic;
- Func Nodes available are ByteFunc, BoolFunc, IntFunc, FPFunc, FPVector2Func, FPVector3Func and EntityRefFunc;
- Func classes created on Quantum will be available as Nodes on the Visual Editor;
- Func Nodes can be linked with AIParam fields
 
 
 
Apr 27, 2021
- Bot SDK 2.1.10 Beta2
 
* Visual Editor
- Fixed issue related to a Unity bug introduced in a few recent versions. It resulted in errors when creating the scripable objects;
- Fixed an issue which resulted in duplicate asset Guid after compiling the AI;
- Fixed issue in which FPVector3 fields would have its value reset to zero everytime;
- The Searchbox now disappears when clicking on the Breadcrumb buttons;
- The values on the slots does not disappear anymore during Runtime;
- Added a context menu (with the Right Click) on the Blackboard, Constants and Config panels;
 
 
Apr 15, 2021
- Bot SDK 2.1.9 Beta2
 
* Visual Editor
- Fixed issue where hidden nodes could be selected and accidentaly deleted;
 
 
Apr 13, 2021
- Bot SDK 2.1.8 Beta2
 
* Serialization
- Updaged Protobuf version to 2.4.6
 
 
Apr 08, 2021
- Bot SDK 2.1.7 Beta2
 
* Behaviour Tree
- Changing the fields from Leaf/Decorators/Service nodes on the quantum code will automatically show those fields on pre-existing nodes at the Unity Editor;
- Fixed issue where Composite/Leaf nodes would keep in memory even after deleted;
 
* Migration from v1 to v2
- Added functionality to convert from Quantum v1’s string Guids to Quantum v2’s long Guids;
 
 
Apr 06, 2021
- Bot SDK 2.1.6 Beta2
 
* Behaviour Tree
- It is now possible to rename Decorator and Service nodes;
- Composite and Leaf nodes now display a number which represents its execution order;
- Composite nodes now has an increased width;
 
 
Apr 01, 2021
- Bot SDK 2.1.5 Beta2
 
* Visual Editor
- Fixed issue with the HFSM Hierarchy debugger related to the state indicator arrow;
- Fixed issue where renaming Transitions would only have effect after domain reload;
- Fixed issue which happened when having an “ANY Transition” but no State on the same hierarchy level;
 
- It is now possible to delete Transitions by using the “Delete” key;
- The Debugger buttons doesn’t disappear during runtime anymore;
- Added messages for when toggling the Debugger active state;
- Removed duplicate “Make Constant” button from the Configs panel;
- Removed the right click panel from the Action Root nodes;
- The Actions Root node is now named “Actions - <StateName>”;
 
 
Feb 26, 2021
- Bot SDK 2.1.4 Beta2
 
* Visual Editor
- Fixed issue upon creating new AI documents;
 
 
Feb 24, 2021
- Bot SDK 2.1.3 Beta2
 
* Visual Editor
- Fixed issue in which closing and reopening AI documents would cause errors upon compilation and upon playing the scene with the Circuit window opened;
 
 
Feb 24, 2021
- Bot SDK 2.1.2 Beta2
 
* Visual Editor
- Fixed issue in which closing and reopening AI documents would lead to a crash in the Bot SDK window;
 
 
Feb 23, 2021
- Bot SDK 2.1.1 Beta2
 
* Visual Editor
- Fixed issue regarding Asset Refs fields in any node type;
 
 
Feb 16, 2021
- Bot SDK 2.1.0 Beta2
 
* Visual Editor
- Major improvements on the Circuit Editor performance;
- Blackboard Variables can now be converted to Constants and Config. On existing nodes, the “Key” slot connection is lost and the “Value” slot’s connection is maintained;
- Constants and Configs can now be converted to Blackboard Variables. On existing nodes, the “Key” is added and the “Value” slot’s connection is maintained;
- Fixed issue in which the button Duplicate Document was generating a new document with invalid Guids on its nodes;
- Added success message on the Circuit window when duplicating a document;
 
* Quantum code
- Added new API for resolving AIParams which only needs the Frame and the EntityRef as parameter;
 
 
 
Jan 19, 2021
- Bot SDK 2.0.0 Beta2
 
* Debugger
- The debugger window now also shows the HFSM/BT asset names alongside with the entity number;
 
* Behaviour Tree
- Added an Asset Ref to AIConfig on the BTAgent, and a “btAgent.GetConfig(frame)” helper method;
 
* Systems
- Created BotSDKDebuggerSystem, which is important for the Debugger on Unity;
 
* Circuit
- Fixed “Debugger Inspector” button on Dark Mode;
 
 
Jan 08, 2021
- Bot SDK 2.0.0 Beta1
 
* Debugger
- Created a custom inspector window for the Debugger. It is now possible to see all HFSMAgents and BTAgents to be debugged there, even if it doesn’t have a View game object;
 
* Behaviour Tree
- Added a debugger for the Behaviour Tree;
- Fixed issue in which Sequences and Selectors did not have their Status updated when canceled by a child node;
- Optimized the logic on the initial memory allocation on the BTAgent. Also improved the amount of data allocated;
- On BotSDKSystem, listening to component added/removed callbacks to initialize and free the BTAgent’s data;
 
* HFSM
- Added method overload for HFSMManager.TriggerEvent, which doesn’t needs a HFSMData* as parameter;
- On the debugger: the transition which shows many points is the most recent transition taken. That same transition is also blue, while the others are dark;
- On BotSDKSystem, listening to component added to initialize a HFSMAgent;
- Removed possibility of deleting the ActionRoot node (the main action node which lies inside States);
- Changed the label used upon asset creation from “State Machine” to “HFSM”;
- Fixed issue which happened when there were “ANYTransitions” but no State node existed;
- Fixed issue in which Actions couldn't receive more than one input (like from both OnEnter and OnUpdate at the same time);
 
* Blackboard
- On BotSDKSystem, listening to component removed to free the Blackboard memory;
 
* Systems
- Created the BotSDKSystem which listens to many callbacks to initialize/free components and data;
 
* DSL
- Removed many “asset import” from the DSL, which were no more needed;
 
 
 
Dec 21, 2020
- Bot SDK 2.0.0 Alpha9
 
* Assets
- BotSDK.unitypackage will not import "AssetLinkDatabase.asset" and "SettingsDatabase" anymore, to prevent issues on asset serialization on old Unity versions.
These assets are generated automatically when opening Bot SDK window.
 
 
Nov 17, 2020
- Bot SDK 2.0.0 Alpha8
 
* Bugfix
- Fixed an issue in which Blackboard/Constant Nodes would be created with the parent Guid
 
 
Nov 16, 2020
- Bot SDK 2.0.0 Alpha7
 
* Compilation
- Added a Boolean on the SettingsDatabase scripable object named "CreateBlackboardInitializer" which makes it possible to avoid creating the asset, if needed
- The Events baked into the HFSM assets are now ordered alphabetically
 
 
Oct 26, 2020
- Bot SDK 2.0.0 Alpha6
 
* Folders structure
- It is now possible to safely change Bot SDK's root folder
 
 
Oct 20, 2020
- Bot SDK 2.0.0 Alpha5
 
* Behaviour Tree Editor
- Release of the alpha version of our Behaviour Tree Editor, composed by:
- Deterministic Behaviour Tree implementation on Quantum solution
- Circuit editor on the Unity side
 
* HFSM
- Changed how “ANY Transitions” are compiled. Their priorities are now organized with the origin states transitions’ priorities
 
* Visual Editor
- Added “Duplicate Document” on Bot SDK window which creates a copy of the currently opened document
- Fixed issue regarding the fields serializer getting null during edit time
- Fixed issue on FP fields which could, in some specific scenarios, get corrupted during edit time
- Fixed issue that created null values during deserialization of field, which could lead the editor to crash
- Added MacOS command “CMD + Backspace” and “CMD + Delete” for deleting Nodes
- Added “Delete Node” button to the Nodes context menu
- The Save History is now provided with a Max save amount of 0. Possible to change via the asset SettingsDatabase, field SaveHistoryCount
 
 
Jun 16, 2020
- Bot SDK 2.0.0 Alpha4
 
* GOAP Editor
- Included the Blackboard panel
- Included the Constants panel
- Included the Configs panel
- Using assets cache to reduce the compile time
- Added complete solution to slots baker, to consider the Blackboards/Constants/Configs;
- Added a reference to an AIConfig on the GOAPAgent component
- Added Hotkey: press “F2” to edit Tasks
 
* Visual Editor
- On the History panel, highlighting the last compiled entry, and the active entry;
- Added “Compilation succeeded” message when Bot SDK finishes compiling;
- The left size panel is now resizable
- The compilation button now turns green when the current state of the circuit was already compiled;
- Support for the creation of AIParam<T> with an Enum as the type. Connections enabled with Blackboard/Constant/Config nodes
- Support for implicit cast on numerical nodes/slots. It is possible now to connect Byte/Int32/FP slots and nodes
- Added button which shows a panel with the most important Hotkeys
- Included panel for correcting Actions/Decisions types if they had their names changed
- The middle mouse button now closes tabs
- Fixed issue regarding duplicating existing Nodes, which was leading to duplicated Guid errors
- When Action/Decision nodes types are broken, they appear on the Left Panel upon compilation. Circuit window will focus on the broken node if the user click on the error message
 
* Quantum code
- HFSM and GOAP main methods doesn’t receive an AIContext as parameter anymore
- Added method GetConfig to the HFSMAgent component
- Removed the parameter “HFSMData* fsm” from the abstract Decide method
- Created method overloads for HFSMManager’s Init and Update methods. No need always to pass the HFSMData* anymore
- Added “asset import” declaractions to all Bot SDK files on its internal .qtn files. Needed for correct DB serialization on newer Quantum 2.0 versions
 
* Folders and files
- Changed the folder structure. It was previously on “Assets/Plugins/Circuit” and it was moved to “Assets/Photon/BotSDK”
- On Unity, changed the debugger script name from HFSMDebugComponent to HFSMDebugger
- Added panel to select the output folder for the result of Bot SDK’s compilation process. The Panel can be found at the bottom of the asset SettingsDatabase, the field name is BotSDKOutputFolder
- Config files are now generated on the subfolder AIConfig_Assets
 
Apr 28, 2020
- Bot SDK 2.0.0 Alpha3
 
* GOAP Editor
- Fixed issue regarding duplicated paths for Actions with the same type
- Fixed issue regarding duplicated paths for Tasks with the same name
- Fixed issue which prevented users to give custom names to Action nodes
 
* Visual Editor
- Fixed AssetRef drawer for Actions and Decisions fields
 
 
Apr 24, 2020
- Bot SDK 2.0.0 Alpha2
 
* Visual Editor
- Added Filter button on the top bar;
- Added “Refresh Documents” button to re-import all types on all Bot SDK documents at the project;
- Added Bot SDK version label to the top bar;
- Removed the “Promote to Variable” alternative from Action/Decision fields
 
* Issues
- Adapted Bot SDK dll to work with the new dlls structure present on Quantum SDK 2.0.0 A1 260 and on
 
Apr 16, 2020
- Bot SDK 2.0.0 Alpha
 
* Portability to Quantum 2.0.0 Alpha
- This version has no major change in comparison to version Bot SDK 1.0.0 Final. Only minor changes to make it work with Quantum 2.0
 
 
March 16, 2020
- Bot SDK 1.0.0 Final
 
* Visual Editor
- Support for adding Labels to Actions/Decision Nodes (from the right click menu)
- Allowed connection between Event Nodes and String/AIParamString slots
- Fixed the Constant Nodes drawer: its width is now defined by the size of the Node value instead of its name
- Removed log message when adding Constant nodes to the graph
- Fixed issue in which uninitialized AIParam fields would generate broken Nodes on the Visual Editor
- Fixed issue in which the compilation process tried to find for the Events on the Constants panel, which would lead to unnecessary logs showing up
- Fixed issues regarding the History view
- Fixed issue that happened when drag-and-dropping assets into AssetLink fields on Actions and Decisions
- Fixed issue in which the Priority values were initialized as null instead of zero
- Fixed issue with the “Compile Project” functionality
- Changed the tooltip from “Compile Project” to “Compile All”
- On the Unity top toolbar, changed the tool name from “Circuit” to “Bot SDK”
 
* Hotkeys
- Added Hotkey: press “M” to mute and unmute States/Actions/Transitions Links
- Added Hotkey: press “F2” to edit States/Actions/Decisions/Transitions Nodes/TransitionSets
- Added Hotkey: press “Esc” to go upper on the states hierarchy or out of a transiton graph
 
* Configs panel
- Created new panel on the left side menu named Config
This panel should be used for agents which use the same HFSM but need different constant values from each other
 
* Blackboard
- The Blackboard getter method is now generated automatically. Instead of Entity.GetBlackboardComponent(entity), use Entity.GetAIBlackboardComponent(entity)
- Fixed issue regarding destroying and re-initializing Blackboard components
 
* GOAP
- Fixed issue on muting Tasks
 
* Unity files
- Moved the AssetLinkDatabase and SettingsDatabase to a new folder. It is not on the Resources folder anymore
- Removed duplicated Gizmos Icon images
 
* Debugger
- Added support to debugging Blackboard Vars
- Event nodes can now be linked to String fields
- Fixed transition color when there is only one transition to be debugged
 
 
Dec 17, 2019
- Bot SDK 1.0.0 Release Candidate 3
 
* Visual Editor
- Changed the line drawers which are used to link states, actions, decisions, etc
- Keyboard numeric Enter is now also used to confirm some editing actions
- Clicking on some transition slot doesn't delete that transition anymore. Instead, drag and drop it to re-direct some transition to another state
- Right click transitions to access the Delete button
- Variables on the left side menu are now ordered by alphabetical order
- Actions and Decisions are also now ordered by alphabetical order
- Actions and Decisions nodes can now have its names changed on the Visual Editor (right click on it to edit). This makes no difference to the actual actions and decisions classes
- Created a menu to fix events/blackboard variables from former Bot SDK versions
- Added the transition Priority to the top view on the graph, on the state node
- No longer generates a Blackboard asset when there is no Blackboard Variable on the visual eitor
 
* ANY Transition
- Added Excluded List to it, which allows the user to inform states that should not consider that ANY transition
 
* Compilation
- Removed code which was causing the compilation process to take the double of the time needed
- Improved a lot the compilation time by re-using the previous data assets structure to prevent re-creating assets unnecessarily
 
* Debugging
- Added new toolset for debugging HFSM agents. Its main features are:
Select some Game Object on your Hierarchy tab which represents some HFSM Agent and has the HFSMDebugComponent added to it in order to open its HFSM editor
See what is the current state in which the HFSM Agent is
See what are the last three transitions taken by that HFSM Agent (works hierarchically)
See what are the current states on the left panel, to easily see all of the current states considering the hierarchy
 
* Quantum code
- Created the AIContext, which is used to reference Constants and Config data assets and can be set differently for bots which has the same HFSM but uses different values
- Changed the way that Transition Sets were handled on the HFSM core code
 
* Constants
- Created the Constants menu on the left side part of the editor
- Constants can be used to define nodes with a default type, and the same constants node can be used as input for many fields
 
* Blackboard
- On the Visual Eitor, added a new slot on every Blackboard node: the value slot, which can be used to define where some Action/Decision reads some value from
- Fixed serialization issue for entities which has the Blackboard Component and are Created -> Destroyed-> Created (respawned)
- Fixed serialization issue regarding using the Blackboard component during matches between different platforms (cross platform)
- Fixed serialization issue regardinsg usint the Blackboard component on replays
 
* AI Param
- Created a new type that easily changes the source from where it reads values: the AIParam
- Public AIParam fields can be defined, on the Visual Editor, from a value set by hand (click to edit), from a Blackboard node or a Constant node
- AIParams are strongly typed, so they are used as AIParamInt, AIParamFP, AIParamBool, etc
 
* Unity Assets
- Added new asset: HFSMDebugComponent
 
* Issues
- Fixed issue on using Events on ANY transitions
- Reduced the amount of SetDirty called for the data assets created, which was causing a bug during compilation on some Mac OS machines
- Added custom header on the default Bot SDK actions/decisions to prevent the Reset method to be created. It was also the cause of issues during compilation
- Fixed precision issues regarding closing/opening circuits with FP values. Every re-open would make the FP value change. FPs are now stored as their internal long value
- Fixed issue when (de)serializing the XML document. It didn't use InvariantCulture so it would cause errors depending on the machine's localization. Issue happened with some czech localized Windows machines 
- Fixed issue in which OnExit actions would be called right after entering some state which has children states
- Fixed compilation and runtime issues when some states were named the same
- Fixed nodes being in incorrect states if it was created as a result of drawing a new transition. It was generating broken nodes after copying and pasting it
- Documents are now saved before assembly recompilation to prevent loss of document modifications
- Fixed the Circuit window when the game is on Play mode
 
 
Aug 15, 2019
- Bot SDK 1.0.0 Release Candidate 2
 
* Visual Editor
- Mute Transition option, accessible by right clicking on a any transition's line or on
its in/outbound slot. Muted transitions will be ignored during the compilation process;
- Mute State option, accessible by right clicking on a State node. Muted states are
ignored during the compilation process, so no transition will lead to that state. The
HFSM will not compile if you mute the Default State;
- Mute Action option, accessible by right clicking on a Action node. It is possible to
mute any Action, no matter its position on the actions list. If a muted action is linked
to another action, then the other action will still be executed;
- Editor screen panning using direcional arrow keys;
- Actions and Decision nodes will automatically add or remove its fields list based on
changes performed on the quantum_code (such as adding/removing fields);
- Actions and Decisions nodes fields are initialized with the values defined on quantum_code;
- Warning logs if some Action/Decision isn't serialized. Those Actions/Decisions wont show on the pop up menu in this case;
- Actions/Decision fields always showing its values. There is no need to select the node to see the values anymore;
- Transitions are now labelled with their Event name if there is no custom label defined;
- Added menu to create Blackboard information within the editor view from which your
custom Blackboards are created and from where you can drag and drop the Blackboard keys as nodes.
 
* Blackboard
- Blackboard Initializer asset, which can be used to define initial values for blackboard instances;
- Fixed issue on Blackboard's memory allocation;
- Added optional compilation symbol (USE_BLACKBOARD_FRAME_METHODS) to define if the Blackboard will not use Frame's partial methods.
 
* Unity Assets
- Compiling a HFSM generates both a Blackboard asset and a BlackboardInitializer asset for that agent;
- Fixed redundancy between SettingsDatabase and AssetLinksDatabase assets which led to invalid cast errors;
- Downgraded all assets versions. Assets were previously generated from Unity 2019.1.0f2, now it is generated from Unity 2018.4.0f1 LTS.
 
* GOAP
- Reduced the amount of GOAP re-planning: it now happens when the agent's Goal changes and when the agent's Current State changes
 
* Changes on quantum_code BotSDK folder's content:
- Included the Samples folder with some very simple pre-defined Actions and Decisions
 
 
Jul 1, 2019
- Bot SDK 1.0.0 Release Candidate 1
