using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using ForkTrack.API;
using ForkTrack.Core;

namespace ForkTrack.Tests.Integration
{
    /// <summary>
    /// Integration tests that fetch the forktrack2 graph from the live API
    /// and validate that all data is correctly parsed by comparing the raw JSON
    /// with the loaded ForkTrackGraph data structure.
    ///
    /// These tests ensure no fields are lost during JSON parsing and that the
    /// data models correctly represent the API response.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("DataValidation")]
    public class Forktrack2APIDataValidationTests
    {
        private const string API_URL = "https://forktrack.pixfork.com";
        private const string TEST_TOKEN = "f4e879700b9bc6aa4a3fd4a3cf4b58f252224754dfa2591ffe37a44adb73c034";
        private const string FORKTRACK2_GRAPH_ID = "forktrack2";

        private ForkTrackAPIClient _client;
        private string _rawGraphJson;
        private RawGraphData _rawData;
        private ForkTrackGraph _loadedGraph;

        #region Raw JSON Data Classes (for direct comparison)

        [System.Serializable]
        private class RawGraphData
        {
            public string schemaVersion;
            public RawVersion version;
            public List<RawCategory> categories;
            public List<RawGroup> groups;
            public List<RawObject> objects;
            public List<RawAction> actions;
            public List<RawVariable> variables;
            public List<RawNode> nodes;
            public List<RawEdge> edges;
        }

        [System.Serializable]
        private class RawVersion { public int major; public int minor; }

        [System.Serializable]
        private class RawCategory { public string id; public string name; public string color; }

        [System.Serializable]
        private class RawGroup { public string id; public string name; public string color; public RawPosition position; public RawSize size; }

        [System.Serializable]
        private class RawObject { public string id; public string name; public string category; }

        [System.Serializable]
        private class RawAction { public string id; public string name; }

        [System.Serializable]
        private class RawVariable { public string id; public string name; public string type; public string defaultValue; }

        [System.Serializable]
        private class RawNode { public string id; public RawPosition position; public string type; public RawNodeData data; }

        [System.Serializable]
        private class RawPosition { public float x; public float y; }

        [System.Serializable]
        private class RawSize { public float width; public float height; }

        [System.Serializable]
        private class RawNodeData
        {
            public string objectId;
            public string actionId;
            public string title;
            public string description;
            public string category;
            public string groupId;
            public bool autoCompleteOnUnlock;
            public string music;
            public List<RawNote> notes;
            public List<RawTodo> todos;
            public List<RawCustomEvent> customEvents;
            public List<RawVariableAction> variableActions;
            // Denormalized names from API
            public string @object;
            public string action;
        }

        [System.Serializable]
        private class RawNote { public string text; public string trigger; }

        [System.Serializable]
        private class RawTodo { public string title; public string description; public string responsible; public bool resolved; }

        [System.Serializable]
        private class RawCustomEvent
        {
            public string propertyId;
            public string trigger;
            public List<RawCustomEventValue> values;
            public float weight;
            public float delay;
            public float amount;
        }

        [System.Serializable]
        private class RawCustomEventValue
        {
            public string text;
            public string value;
            public float weight;
            public float delay;
            public float amount;
        }

        [System.Serializable]
        private class RawVariableAction { public string id; public string variableId; public string trigger; public string operation; public string value; }

        [System.Serializable]
        private class RawEdge { public string id; public string sourceNodeId; public string targetNodeId; public RawEdgeData data; }

        [System.Serializable]
        private class RawEdgeData
        {
            public string requiredState;
            public string condition;
            public List<RawVariableCondition> variableConditions;
        }

        [System.Serializable]
        private class RawVariableCondition { public string id; public string variableId; public string @operator; public string value; }

        #endregion

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _client = new ForkTrackAPIClient(TEST_TOKEN, API_URL);
        }

        [SetUp]
        public void SetUp()
        {
            ForkTrack.ResetRuntime();
            _rawGraphJson = null;
            _rawData = null;
            _loadedGraph = null;
        }

        [TearDown]
        public void TearDown()
        {
            ForkTrack.ResetRuntime();
        }

        #region Data Fetching

        private IEnumerator FetchAndLoadForktrack2()
        {
            // Find forktrack2 graph
            string graphId = null;

            yield return _client.ListGraphs(
                onSuccess: r =>
                {
                    foreach (var graph in r.graphs)
                    {
                        if (graph.name.ToLower().Contains("forktrack2") || graph.id == FORKTRACK2_GRAPH_ID)
                        {
                            graphId = graph.id;
                            Debug.Log($"[DataValidation] Found forktrack2 graph: {graph.name} (id: {graphId})");
                            break;
                        }
                    }
                },
                onError: e => Assert.Fail($"ListGraphs failed: {e}")
            );

            if (string.IsNullOrEmpty(graphId))
            {
                Assert.Inconclusive("forktrack2 graph not found");
                yield break;
            }

            // Fetch the graph
            yield return _client.FetchGraph(graphId,
                onSuccess: json =>
                {
                    _rawGraphJson = json;
                    Debug.Log($"[DataValidation] Fetched graph JSON ({json.Length} chars)");
                },
                onError: e => Assert.Fail($"FetchGraph failed: {e}")
            );

            // Parse raw JSON for comparison
            _rawData = JsonUtility.FromJson<RawGraphData>(_rawGraphJson);

            // Load into ForkTrack
            ForkTrack.LoadGraphFromJSON(_rawGraphJson);
            _loadedGraph = ForkTrack.CurrentGraph;

            Assert.IsNotNull(_loadedGraph, "Graph should be loaded");
        }

        #endregion

        #region Schema and Basic Structure Tests

        [UnityTest]
        public IEnumerator Forktrack2API_SchemaVersion_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            Assert.AreEqual(_rawData.schemaVersion, _loadedGraph.schemaVersion,
                "Schema version should be preserved");
            Assert.AreEqual("1.2.0", _loadedGraph.schemaVersion,
                "Should be schema v1.2.0");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_NodeCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            Assert.AreEqual(_rawData.nodes.Count, _loadedGraph.NodeCount,
                $"Node count mismatch: raw={_rawData.nodes.Count}, loaded={_loadedGraph.NodeCount}");

            Debug.Log($"[DataValidation] Node count matches: {_loadedGraph.NodeCount}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_EdgeCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            Assert.AreEqual(_rawData.edges.Count, _loadedGraph.EdgeCount,
                $"Edge count mismatch: raw={_rawData.edges.Count}, loaded={_loadedGraph.EdgeCount}");

            Debug.Log($"[DataValidation] Edge count matches: {_loadedGraph.EdgeCount}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_CategoryCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            int rawCount = _rawData.categories?.Count ?? 0;
            Assert.AreEqual(rawCount, _loadedGraph.categories.Count,
                $"Category count mismatch: raw={rawCount}, loaded={_loadedGraph.categories.Count}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_GroupCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            int rawCount = _rawData.groups?.Count ?? 0;
            Assert.AreEqual(rawCount, _loadedGraph.groups.Count,
                $"Group count mismatch: raw={rawCount}, loaded={_loadedGraph.groups.Count}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_ObjectCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            int rawCount = _rawData.objects?.Count ?? 0;
            Assert.AreEqual(rawCount, _loadedGraph.objects.Count,
                $"Object count mismatch: raw={rawCount}, loaded={_loadedGraph.objects.Count}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_ActionCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            int rawCount = _rawData.actions?.Count ?? 0;
            Assert.AreEqual(rawCount, _loadedGraph.actions.Count,
                $"Action count mismatch: raw={rawCount}, loaded={_loadedGraph.actions.Count}");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_VariableCount_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            int rawCount = _rawData.variables?.Count ?? 0;
            Assert.AreEqual(rawCount, _loadedGraph.VariableCount,
                $"Variable count mismatch: raw={rawCount}, loaded={_loadedGraph.VariableCount}");
        }

        #endregion

        #region Node Data Validation Tests

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingIds()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                Assert.IsNotNull(loadedNode, $"Node {rawNode.id} should exist in loaded graph");
            }

            Debug.Log($"[DataValidation] All {_rawData.nodes.Count} node IDs match");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingPositions()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                Assert.IsNotNull(loadedNode, $"Node {rawNode.id} not found");

                if (rawNode.position != null)
                {
                    Assert.AreEqual(rawNode.position.x, loadedNode.position.x, 0.001f,
                        $"Node {rawNode.id} X position mismatch");
                    Assert.AreEqual(rawNode.position.y, loadedNode.position.y, 0.001f,
                        $"Node {rawNode.id} Y position mismatch");
                }
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingObjectIds()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawObjectId = rawNode.data?.objectId;

                Assert.AreEqual(rawObjectId, loadedNode.objectId,
                    $"Node {rawNode.id} objectId mismatch: raw='{rawObjectId}', loaded='{loadedNode.objectId}'");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingActionIds()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawActionId = rawNode.data?.actionId;

                Assert.AreEqual(rawActionId, loadedNode.actionId,
                    $"Node {rawNode.id} actionId mismatch: raw='{rawActionId}', loaded='{loadedNode.actionId}'");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingTitles()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawTitle = rawNode.data?.title;

                Assert.AreEqual(rawTitle, loadedNode.title,
                    $"Node {rawNode.id} title mismatch: raw='{rawTitle}', loaded='{loadedNode.title}'");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingDescriptions()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawDescription = rawNode.data?.description ?? "";

                Assert.AreEqual(rawDescription, loadedNode.description,
                    $"Node {rawNode.id} description mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingGroupIds()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawGroupId = rawNode.data?.groupId;

                Assert.AreEqual(rawGroupId, loadedNode.groupId,
                    $"Node {rawNode.id} groupId mismatch: raw='{rawGroupId}', loaded='{loadedNode.groupId}'");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllNodes_ShouldHaveMatchingAutoCompleteFlag()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawFlag = rawNode.data?.autoCompleteOnUnlock ?? false;

                Assert.AreEqual(rawFlag, loadedNode.autoCompleteOnUnlock,
                    $"Node {rawNode.id} autoCompleteOnUnlock mismatch");
            }
        }

        #endregion

        #region Object/Action Name Resolution Tests

        [UnityTest]
        public IEnumerator Forktrack2API_NodesWithObjectId_ShouldHaveObjectNameResolved()
        {
            yield return FetchAndLoadForktrack2();

            int resolved = 0;
            int withObjectId = 0;

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawObjectId = rawNode.data?.objectId;

                if (!string.IsNullOrEmpty(rawObjectId))
                {
                    withObjectId++;
                    Assert.IsNotNull(loadedNode.objectName,
                        $"Node {rawNode.id} with objectId '{rawObjectId}' should have objectName resolved");
                    resolved++;
                }
            }

            Debug.Log($"[DataValidation] Resolved {resolved}/{withObjectId} object names");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_NodesWithActionId_ShouldHaveActionNameResolved()
        {
            yield return FetchAndLoadForktrack2();

            int resolved = 0;
            int withActionId = 0;

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawActionId = rawNode.data?.actionId;

                if (!string.IsNullOrEmpty(rawActionId))
                {
                    withActionId++;
                    Assert.IsNotNull(loadedNode.actionName,
                        $"Node {rawNode.id} with actionId '{rawActionId}' should have actionName resolved");
                    resolved++;
                }
            }

            Debug.Log($"[DataValidation] Resolved {resolved}/{withActionId} action names");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_ObjectNames_ShouldMatchObjectsList()
        {
            yield return FetchAndLoadForktrack2();

            var objectsById = new Dictionary<string, string>();
            foreach (var obj in _rawData.objects ?? new List<RawObject>())
            {
                objectsById[obj.id] = obj.name;
            }

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawObjectId = rawNode.data?.objectId;

                if (!string.IsNullOrEmpty(rawObjectId) && objectsById.TryGetValue(rawObjectId, out var expectedName))
                {
                    Assert.AreEqual(expectedName, loadedNode.objectName,
                        $"Node {rawNode.id} objectName should match objects list");
                }
            }
        }

        #endregion

        #region Custom Event Data Validation Tests

        [UnityTest]
        public IEnumerator Forktrack2API_CustomEvents_CountShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                int rawCount = rawNode.data?.customEvents?.Count ?? 0;

                Assert.AreEqual(rawCount, loadedNode.customEvents.Count,
                    $"Node {rawNode.id} customEvents count mismatch: raw={rawCount}, loaded={loadedNode.customEvents.Count}");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_CustomEvents_PropertyIdsShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawEvents = rawNode.data?.customEvents ?? new List<RawCustomEvent>();

                for (int i = 0; i < rawEvents.Count; i++)
                {
                    var rawEvent = rawEvents[i];
                    var loadedEvent = loadedNode.customEvents[i];

                    Assert.AreEqual(rawEvent.propertyId, loadedEvent.propertyId,
                        $"Node {rawNode.id} event[{i}] propertyId mismatch");
                }
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_CustomEvents_ValuesShouldBeExtracted()
        {
            yield return FetchAndLoadForktrack2();

            int eventsWithValues = 0;
            int valuesExtracted = 0;

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawEvents = rawNode.data?.customEvents ?? new List<RawCustomEvent>();

                for (int i = 0; i < rawEvents.Count; i++)
                {
                    var rawEvent = rawEvents[i];
                    var loadedEvent = loadedNode.customEvents[i];

                    if (rawEvent.values != null && rawEvent.values.Count > 0)
                    {
                        eventsWithValues++;

                        // Build expected values from raw data
                        var expectedValues = new List<string>();
                        foreach (var v in rawEvent.values)
                        {
                            if (!string.IsNullOrEmpty(v.value))
                                expectedValues.Add(v.value);
                            else if (!string.IsNullOrEmpty(v.text))
                                expectedValues.Add(v.text);
                        }

                        Assert.AreEqual(expectedValues.Count, loadedEvent.values.Count,
                            $"Node {rawNode.id} event[{i}] values count mismatch");

                        for (int j = 0; j < expectedValues.Count; j++)
                        {
                            Assert.AreEqual(expectedValues[j], loadedEvent.values[j],
                                $"Node {rawNode.id} event[{i}] value[{j}] mismatch: expected='{expectedValues[j]}', actual='{loadedEvent.values[j]}'");
                            valuesExtracted++;
                        }
                    }
                }
            }

            Debug.Log($"[DataValidation] Validated {valuesExtracted} values from {eventsWithValues} custom events");
        }

        [UnityTest]
        public IEnumerator Forktrack2API_CustomEvents_TriggersShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawEvents = rawNode.data?.customEvents ?? new List<RawCustomEvent>();

                for (int i = 0; i < rawEvents.Count; i++)
                {
                    var rawEvent = rawEvents[i];
                    var loadedEvent = loadedNode.customEvents[i];

                    Assert.AreEqual(rawEvent.trigger, loadedEvent.trigger,
                        $"Node {rawNode.id} event[{i}] trigger mismatch");
                }
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_CustomEvents_NumericFieldsShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawEvents = rawNode.data?.customEvents ?? new List<RawCustomEvent>();

                for (int i = 0; i < rawEvents.Count; i++)
                {
                    var rawEvent = rawEvents[i];
                    var loadedEvent = loadedNode.customEvents[i];

                    Assert.AreEqual(rawEvent.weight, loadedEvent.weight, 0.001f,
                        $"Node {rawNode.id} event[{i}] weight mismatch");
                    Assert.AreEqual(rawEvent.delay, loadedEvent.delay, 0.001f,
                        $"Node {rawNode.id} event[{i}] delay mismatch");
                    Assert.AreEqual(rawEvent.amount, loadedEvent.amount, 0.001f,
                        $"Node {rawNode.id} event[{i}] amount mismatch");
                }
            }
        }

        #endregion

        #region Edge Data Validation Tests

        [UnityTest]
        public IEnumerator Forktrack2API_AllEdges_ShouldHaveMatchingIds()
        {
            yield return FetchAndLoadForktrack2();

            var loadedEdgesById = new Dictionary<string, ForkTrackEdge>();
            foreach (var edge in _loadedGraph.edges)
            {
                loadedEdgesById[edge.id] = edge;
            }

            foreach (var rawEdge in _rawData.edges)
            {
                Assert.IsTrue(loadedEdgesById.ContainsKey(rawEdge.id),
                    $"Edge {rawEdge.id} should exist in loaded graph");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllEdges_ShouldHaveMatchingSourceTarget()
        {
            yield return FetchAndLoadForktrack2();

            var loadedEdgesById = new Dictionary<string, ForkTrackEdge>();
            foreach (var edge in _loadedGraph.edges)
            {
                loadedEdgesById[edge.id] = edge;
            }

            foreach (var rawEdge in _rawData.edges)
            {
                var loadedEdge = loadedEdgesById[rawEdge.id];

                Assert.AreEqual(rawEdge.sourceNodeId, loadedEdge.sourceNodeId,
                    $"Edge {rawEdge.id} sourceNodeId mismatch");
                Assert.AreEqual(rawEdge.targetNodeId, loadedEdge.targetNodeId,
                    $"Edge {rawEdge.id} targetNodeId mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllEdges_ShouldHaveMatchingRequiredState()
        {
            yield return FetchAndLoadForktrack2();

            var loadedEdgesById = new Dictionary<string, ForkTrackEdge>();
            foreach (var edge in _loadedGraph.edges)
            {
                loadedEdgesById[edge.id] = edge;
            }

            foreach (var rawEdge in _rawData.edges)
            {
                var loadedEdge = loadedEdgesById[rawEdge.id];
                // requiredState is stored as string in ForkTrackEdge
                var expectedState = rawEdge.data?.requiredState ?? "OnComplete";

                Assert.AreEqual(expectedState, loadedEdge.requiredState,
                    $"Edge {rawEdge.id} requiredState mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_EdgeVariableConditions_ShouldMatch()
        {
            yield return FetchAndLoadForktrack2();

            var loadedEdgesById = new Dictionary<string, ForkTrackEdge>();
            foreach (var edge in _loadedGraph.edges)
            {
                loadedEdgesById[edge.id] = edge;
            }

            int edgesWithConditions = 0;

            foreach (var rawEdge in _rawData.edges)
            {
                var loadedEdge = loadedEdgesById[rawEdge.id];
                var rawConditions = rawEdge.data?.variableConditions ?? new List<RawVariableCondition>();

                Assert.AreEqual(rawConditions.Count, loadedEdge.variableConditions.Count,
                    $"Edge {rawEdge.id} variableConditions count mismatch");

                if (rawConditions.Count > 0)
                {
                    edgesWithConditions++;

                    for (int i = 0; i < rawConditions.Count; i++)
                    {
                        var rawCond = rawConditions[i];
                        var loadedCond = loadedEdge.variableConditions[i];

                        Assert.AreEqual(rawCond.id, loadedCond.id,
                            $"Edge {rawEdge.id} condition[{i}] id mismatch");
                        Assert.AreEqual(rawCond.variableId, loadedCond.variableId,
                            $"Edge {rawEdge.id} condition[{i}] variableId mismatch");
                    }
                }
            }

            Debug.Log($"[DataValidation] Validated {edgesWithConditions} edges with variable conditions");
        }

        #endregion

        #region Variable Data Validation Tests

        [UnityTest]
        public IEnumerator Forktrack2API_AllVariables_ShouldHaveMatchingData()
        {
            yield return FetchAndLoadForktrack2();

            var rawVariables = _rawData.variables ?? new List<RawVariable>();

            foreach (var rawVar in rawVariables)
            {
                var loadedVar = _loadedGraph.GetVariableById(rawVar.id);
                Assert.IsNotNull(loadedVar, $"Variable {rawVar.id} should exist");

                Assert.AreEqual(rawVar.name, loadedVar.name,
                    $"Variable {rawVar.id} name mismatch");

                // Check type - type is stored as string in ForkTrackVariable
                // Normalize to uppercase for comparison
                var expectedType = rawVar.type?.ToUpper() ?? "NUMBER";
                Assert.AreEqual(expectedType, loadedVar.type,
                    $"Variable {rawVar.id} type mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_Variables_ShouldBeLookupableByName()
        {
            yield return FetchAndLoadForktrack2();

            var rawVariables = _rawData.variables ?? new List<RawVariable>();

            foreach (var rawVar in rawVariables)
            {
                var loadedVar = _loadedGraph.GetVariableByName(rawVar.name);
                Assert.IsNotNull(loadedVar, $"Variable with name '{rawVar.name}' should be findable");
                Assert.AreEqual(rawVar.id, loadedVar.id);
            }
        }

        #endregion

        #region Category and Group Data Validation Tests

        [UnityTest]
        public IEnumerator Forktrack2API_AllCategories_ShouldHaveMatchingData()
        {
            yield return FetchAndLoadForktrack2();

            var rawCategories = _rawData.categories ?? new List<RawCategory>();

            foreach (var rawCat in rawCategories)
            {
                var loadedCat = _loadedGraph.GetCategoryById(rawCat.id);
                Assert.IsNotNull(loadedCat, $"Category {rawCat.id} should exist");

                Assert.AreEqual(rawCat.name, loadedCat.name,
                    $"Category {rawCat.id} name mismatch");
                Assert.AreEqual(rawCat.color, loadedCat.color,
                    $"Category {rawCat.id} color mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_AllGroups_ShouldHaveMatchingData()
        {
            yield return FetchAndLoadForktrack2();

            var rawGroups = _rawData.groups ?? new List<RawGroup>();

            foreach (var rawGroup in rawGroups)
            {
                var loadedGroup = _loadedGraph.GetGroupById(rawGroup.id);
                Assert.IsNotNull(loadedGroup, $"Group {rawGroup.id} should exist");

                Assert.AreEqual(rawGroup.name, loadedGroup.name,
                    $"Group {rawGroup.id} name mismatch");
            }
        }

        [UnityTest]
        public IEnumerator Forktrack2API_NodesInGroups_ShouldHaveGroupNameResolved()
        {
            yield return FetchAndLoadForktrack2();

            int nodesInGroups = 0;

            foreach (var rawNode in _rawData.nodes)
            {
                var loadedNode = _loadedGraph.GetNodeById(rawNode.id);
                var rawGroupId = rawNode.data?.groupId;

                if (!string.IsNullOrEmpty(rawGroupId))
                {
                    nodesInGroups++;
                    Assert.IsNotNull(loadedNode.groupName,
                        $"Node {rawNode.id} with groupId should have groupName resolved");
                }
            }

            Debug.Log($"[DataValidation] Validated {nodesInGroups} nodes in groups");
        }

        #endregion

        #region Complete Data Summary Test

        [UnityTest]
        public IEnumerator Forktrack2API_CompleteSummary_ShouldPass()
        {
            yield return FetchAndLoadForktrack2();

            Debug.Log("=== FORKTRACK2 API DATA VALIDATION SUMMARY ===");
            Debug.Log($"Schema Version: {_loadedGraph.schemaVersion}");
            Debug.Log($"Nodes: {_loadedGraph.NodeCount}");
            Debug.Log($"Edges: {_loadedGraph.EdgeCount}");
            Debug.Log($"Categories: {_loadedGraph.categories.Count}");
            Debug.Log($"Groups: {_loadedGraph.groups.Count}");
            Debug.Log($"Objects: {_loadedGraph.objects.Count}");
            Debug.Log($"Actions: {_loadedGraph.actions.Count}");
            Debug.Log($"Variables: {_loadedGraph.VariableCount}");

            // Count nodes with various data
            int nodesWithObject = 0;
            int nodesWithAction = 0;
            int nodesWithEvents = 0;
            int totalEvents = 0;
            int totalEventValues = 0;

            foreach (var node in _loadedGraph.nodes)
            {
                if (!string.IsNullOrEmpty(node.objectId)) nodesWithObject++;
                if (!string.IsNullOrEmpty(node.actionId)) nodesWithAction++;
                if (node.customEvents.Count > 0)
                {
                    nodesWithEvents++;
                    totalEvents += node.customEvents.Count;
                    foreach (var evt in node.customEvents)
                    {
                        totalEventValues += evt.values.Count;
                    }
                }
            }

            Debug.Log($"Nodes with objectId: {nodesWithObject}");
            Debug.Log($"Nodes with actionId: {nodesWithAction}");
            Debug.Log($"Nodes with custom events: {nodesWithEvents}");
            Debug.Log($"Total custom events: {totalEvents}");
            Debug.Log($"Total event values: {totalEventValues}");
            Debug.Log("==============================================");

            // Final assertion - we successfully loaded and analyzed the graph
            Assert.IsTrue(ForkTrack.IsLoaded, "Graph should be loaded successfully");
        }

        #endregion
    }
}
