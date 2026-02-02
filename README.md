# ForkTrack Unity Package

Integrate ForkTrack narrative graphs into Unity games with a clean C# event API.

## Features

- **Static API**: Simple, clean API via `ForkTrack.LoadGraphFromJSON()`, `ForkTrack.CompleteNode()`
- **C# Events**: Subscribe to `OnNodeCompleted`, `OnVariableChanged`, `OnCustomEvent`, etc.
- **Subscriptions**: Pre-register for specific node/event callbacks that validate on graph load
- **Variable System**: NUMBER and BOOLEAN variables with SET/ADD/SUBTRACT/TOGGLE operations
- **Dependency Logic**: AND/OR/NOT conditions with variable-based edge conditions
- **Offline-First**: Fetch once, cache locally, work offline
- **Progress Persistence**: Save/load slots with JSON export for cloud saves
- **Secure**: Encrypted token storage, SSL validation in all builds
- **Schema Support**: v1.0.0, v1.1.0, and v1.2.0 with auto-migration

## Installation

### Unity Package Manager (UPM)

Add to your `manifest.json`:
```json
{
  "dependencies": {
    "com.forktrack.unity": "https://github.com/forktrack/unity-package.git"
  }
}
```

Or via Window > Package Manager > Add package from git URL.

### Manual Installation

Copy the `unity-package` folder to your project's `Packages` directory.

## Architecture

ForkTrack uses a **static singleton pattern** - no MonoBehaviour or setup required:

```
ForkTrack (static API) → ForkTrackRuntime (singleton) → ForkTrackGraph (data)
```

The runtime is created automatically on first access.

## Quick Start

### Option 1: Load from Local JSON File

```csharp
using ForkTrack;

public class GameManager : MonoBehaviour
{
    public TextAsset graphFile;

    void Start()
    {
        ForkTrack.OnGraphLoaded += OnGraphLoaded;
        ForkTrack.OnNodeCompleted += OnNodeCompleted;

        ForkTrack.LoadGraphFromJSON(graphFile.text);
    }

    void OnGraphLoaded(ForkTrackGraph graph)
    {
        Debug.Log($"Loaded graph with {graph.NodeCount} nodes");
    }

    void OnNodeCompleted(ForkTrackNode node)
    {
        Debug.Log($"Completed: {node.objectName} - {node.actionName}");
    }
}
```

### Option 2: Load from ForkTrack API

First, configure your API token via **Tools → ForkTrack → API Settings**.

```csharp
using ForkTrack;
using ForkTrack.API;
using System.Collections;

public class GameManager : MonoBehaviour
{
    IEnumerator Start()
    {
        // Create client using stored settings
        var client = TokenAuthenticator.CreateClient();

        if (client == null)
        {
            Debug.LogError("No API token! Configure in Tools > ForkTrack > API Settings");
            yield break;
        }

        // List available graphs
        yield return client.ListGraphs(
            onSuccess: response => {
                foreach (var g in response.graphs)
                    Debug.Log($"Found: {g.name} ({g.id})");
            },
            onError: Debug.LogError
        );

        // Fetch and load a specific graph
        yield return client.FetchGraph("your-graph-id",
            onSuccess: json => ForkTrack.LoadGraphFromJSON(json),
            onError: Debug.LogError
        );
    }
}
```

## Events Reference

### Node State Events

```csharp
// Node transitions from Locked → Unlocked
ForkTrack.OnNodeUnlocked += (ForkTrackNode node) => { };

// Node transitions from Unlocked → Completed
ForkTrack.OnNodeCompleted += (ForkTrackNode node) => { };

// Node reset to Locked
ForkTrack.OnNodeReset += (ForkTrackNode node) => { };

// Any state change (most flexible)
ForkTrack.OnNodeStateChanged += (ForkTrackNode node, NodeState oldState, NodeState newState) => { };
```

### Graph Events

```csharp
// Graph loaded successfully
ForkTrack.OnGraphLoaded += (ForkTrackGraph graph) => { };

// Graph failed to load
ForkTrack.OnGraphLoadError += (string error) => { };
```

### Variable Events

```csharp
// Variable value changed
ForkTrack.OnVariableChanged += (ForkTrackVariable var, object oldVal, object newVal) => { };
```

### Custom Events

Custom events defined in ForkTrack nodes are fired when nodes are unlocked/completed:

```csharp
ForkTrack.OnCustomEvent += (CustomEventData evt) => {
    Debug.Log($"Event: {evt.propertyId}");
    Debug.Log($"Value: {evt.selectedValue}");  // The randomly selected value
    Debug.Log($"All values: {string.Join(", ", evt.values)}");
    Debug.Log($"From node: {evt.nodeId}");
    Debug.Log($"Trigger: {evt.trigger}");  // OnUnlock or OnComplete
    Debug.Log($"Weight: {evt.weight}, Delay: {evt.delay}, Amount: {evt.amount}");
};
```

### Notes

```csharp
ForkTrack.OnNoteFired += (ForkTrackNode node, NodeNote note) => {
    Debug.Log($"Note from {node.id}: {note.text}");
};
```

## Node Queries

```csharp
// Get by unique ID
ForkTrackNode node = ForkTrack.GetNode("node_1234567890");

// Get by Object + Action (preferred - more readable)
ForkTrackNode node = ForkTrack.GetNode("Fred", "Interact");

// Get by title
ForkTrackNode node = ForkTrack.GetNodeByTitle("Talk to Fred");

// Get all nodes for an object
List<ForkTrackNode> fredNodes = ForkTrack.GetNodesByObject("Fred");

// Get all nodes for an action
List<ForkTrackNode> interactions = ForkTrack.GetNodesByAction("Interact");

// Get nodes by state
List<ForkTrackNode> available = ForkTrack.GetNodesInState(NodeState.Unlocked);
List<ForkTrackNode> completed = ForkTrack.GetNodesInState(NodeState.Completed);

// Get all nodes
List<ForkTrackNode> all = ForkTrack.GetAllNodes();
```

## Node Actions

```csharp
// Complete a node (Unlocked → Completed)
// This is the most common action - when a player does something
ForkTrack.CompleteNode("Fred", "Interact");
ForkTrack.CompleteNode("node_1234567890");

// Unlock a node manually (Locked → Unlocked)
// Usually handled automatically by dependency system
ForkTrack.UnlockNode("Fred", "Interact");

// Reset a node (any state → Locked)
ForkTrack.ResetNode("node_1234567890");

// Reset entire graph to initial state
ForkTrack.ResetAll();
```

## Subscriptions

Subscriptions let you pre-register callbacks for specific nodes/events. They're validated when a graph loads and warn you if the target doesn't exist.

### Node Subscriptions

```csharp
// Subscribe by Object + Action
ForkTrack.SubscribeNode("Fred", "Interact",
    onCompleted: node => {
        Debug.Log("Fred interaction complete!");
        UnlockDoor();
    },
    onUnlocked: node => {
        Debug.Log("Can now interact with Fred");
        ShowPrompt("Press E to talk");
    }
);

// Subscribe by ID
ForkTrack.SubscribeNode("node_1234567890",
    onCompleted: node => HandleCompletion(node)
);

// Subscribe by title
ForkTrack.SubscribeNodeByTitle("Final Boss Defeated",
    onCompleted: node => RollCredits()
);

// Unsubscribe
var sub = ForkTrack.SubscribeNode("Fred", "Interact", OnComplete);
ForkTrack.UnsubscribeNode(sub);
```

### Event Subscriptions

```csharp
// Subscribe to any event of a type
ForkTrack.SubscribeEvent("dialogue_choice",
    onTriggered: evt => {
        Debug.Log($"Player chose: {evt.selectedValue}");
        ProcessDialogueChoice(evt.selectedValue);
    }
);

// Subscribe to a specific value only
ForkTrack.SubscribeEvent("dialogue_choice",
    onTriggered: evt => Debug.Log("Player was rude!"),
    value: "rude_response"
);
```

## Variables

Variables are graph-level values that can be NUMBER or BOOLEAN:

```csharp
// Get values
float coins = ForkTrack.GetVariable<float>("coins");
bool hasKey = ForkTrack.GetVariable<bool>("hasKey");

// Set values (fires OnVariableChanged)
ForkTrack.SetVariable("coins", 100f);
ForkTrack.SetVariable("hasKey", true);

// Get all variable definitions
List<ForkTrackVariable> vars = ForkTrack.GetAllVariables();
foreach (var v in vars)
{
    Debug.Log($"{v.name} ({v.type}) = {v.defaultValue}");
}
```

Variables can also be modified automatically by **Variable Actions** on nodes (SET, ADD, SUBTRACT, TOGGLE) when nodes are unlocked/completed.

## Progress Persistence

### Save Slots (PlayerPrefs)

```csharp
// Save current progress
ForkTrack.SaveProgress("slot1");

// Load progress (returns false if no save exists)
if (ForkTrack.HasSavedProgress("slot1"))
{
    ForkTrack.LoadProgress("slot1");
}

// Delete a save
ForkTrack.DeleteProgress("slot1");
```

### Cloud Saves (JSON Export/Import)

```csharp
// Export progress as JSON string
string progressJson = ForkTrack.ExportProgress();
SaveToCloud(progressJson);

// Import progress from JSON string
string cloudData = LoadFromCloud();
ForkTrack.ImportProgress(cloudData);
```

## Multiple Graphs

**Only one graph can be loaded at a time.** Loading a new graph replaces the current one.

```csharp
// To switch between graphs, save progress first
ForkTrack.SaveProgress("chapter1_progress");

// Load new graph
ForkTrack.LoadGraphFromJSON(chapter2Json);

// Later, return to chapter 1
ForkTrack.LoadGraphFromJSON(chapter1Json);
ForkTrack.LoadProgress("chapter1_progress");
```

## API Client Reference

### TokenAuthenticator

```csharp
using ForkTrack.API;

// Check if token is configured
bool hasToken = TokenAuthenticator.HasToken();

// Get stored token/URL
string token = TokenAuthenticator.GetToken();
string url = TokenAuthenticator.GetAPIUrl();

// Create pre-configured client
ForkTrackAPIClient client = TokenAuthenticator.CreateClient();

// Save token programmatically (for runtime configuration)
TokenAuthenticator.SaveToken("your-api-token");
TokenAuthenticator.SaveAPIUrl("https://forktrack.pixfork.com");
```

### ForkTrackAPIClient

```csharp
using ForkTrack.API;

// Create client (or use TokenAuthenticator.CreateClient())
var client = new ForkTrackAPIClient("your-token", "https://forktrack.pixfork.com");

// Validate token
yield return client.ValidateToken(isValid => {
    Debug.Log(isValid ? "Token valid" : "Token invalid");
});

// List all graphs
yield return client.ListGraphs(
    onSuccess: response => {
        foreach (var g in response.graphs)
            Debug.Log($"{g.name} (id: {g.id}, v{g.version_major}.{g.version_minor})");
    },
    onError: error => Debug.LogError(error)
);

// Fetch a specific graph
yield return client.FetchGraph("graph-id",
    onSuccess: json => ForkTrack.LoadGraphFromJSON(json),
    onError: error => Debug.LogError(error)
);
```

## Configuration

### Editor Settings

Access via **Tools → ForkTrack → API Settings**:

| Setting | Description |
|---------|-------------|
| API URL | ForkTrack server URL (default: https://forktrack.pixfork.com) |
| API Token | Bearer token for authentication |

### ScriptableObject Settings

Create via **Assets → Create → ForkTrack → Settings**, place in `Resources` folder:

| Setting | Description |
|---------|-------------|
| API URL | Server URL |
| Default Graph ID | Graph to load by default |
| Cache Freshness Hours | Hours before cached graphs are stale |
| Auto Save Progress | Save on every node state change |
| Auto Load Progress | Load progress when graph loads |
| Default Save Slot | Default slot name |
| Fallback Graph | TextAsset to use when offline |
| Log Level | Debug verbosity |

## ForkTrackNode Properties

```csharp
ForkTrackNode node = ForkTrack.GetNode("Fred", "Interact");

// Identity
node.id              // Unique ID: "node_1234567890"
node.objectId        // Reference to objects list
node.actionId        // Reference to actions list
node.title           // Optional display title

// Resolved names (looked up from objects/actions lists)
node.objectName      // "Fred"
node.actionName      // "Interact"
node.categoryName    // "Main Quest"
node.categoryColor   // "#FF5733"
node.groupName       // "Chapter 1"

// State
node.IsLocked        // true if Locked
node.IsUnlocked      // true if Unlocked
node.IsCompleted     // true if Completed

// Content
node.description     // Node description text
node.categoryId      // Category reference
node.groupId         // Group reference
node.autoCompleteOnUnlock  // Auto-complete when unlocked
node.music           // Music track reference

// Collections
node.notes           // List<NodeNote>
node.customEvents    // List<CustomEvent>
node.todos           // List<NodeTodo>
node.variableActions // List<VariableAction>

// Display
node.GetDisplayName()  // "Fred - Interact: Optional Title"
```

## Samples

Import samples via Package Manager:

1. **Basic Usage**: Loading, events, completion, variables
2. **Quest System**: Complete quest tracking with inventory

## Requirements

- Unity 2021.3 LTS or later
- No external dependencies

## License

See LICENSE.md
