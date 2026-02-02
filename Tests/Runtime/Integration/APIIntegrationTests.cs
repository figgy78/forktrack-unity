using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Text.RegularExpressions;
using ForkTrack.API;
using ForkTrack.Core;

namespace ForkTrack.Tests.Integration
{
    /// <summary>
    /// Integration tests for the ForkTrack API client.
    /// Tests connectivity to the production ForkTrack API.
    ///
    /// NOTE: These tests require network access and a valid API token.
    /// They are marked with [Category("Integration")] for selective execution.
    ///
    /// To run these tests:
    /// 1. Set FORKTRACK_TEST_TOKEN environment variable, or
    /// 2. Use the test token in ForkTrackSettingsWindow
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class APIIntegrationTests
    {
        private const string API_URL = "https://forktrack.pixfork.com";

        // Test token - in production tests, this should come from environment/settings
        // This token is for testing only and has limited permissions
        private const string TEST_TOKEN = "f4e879700b9bc6aa4a3fd4a3cf4b58f252224754dfa2591ffe37a44adb73c034";

        private ForkTrackAPIClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new ForkTrackAPIClient(TEST_TOKEN, API_URL);
        }

        #region Token Validation Tests

        [UnityTest]
        public IEnumerator ValidateToken_WithValidToken_ShouldReturnTrue()
        {
            bool? result = null;

            yield return _client.ValidateToken(isValid =>
            {
                result = isValid;
            });

            Assert.IsNotNull(result, "Token validation should complete");
            Assert.IsTrue(result.Value, "Valid token should pass validation");
        }

        [UnityTest]
        public IEnumerator ValidateToken_WithInvalidToken_ShouldReturnFalse()
        {
            var invalidClient = new ForkTrackAPIClient("invalid_token_12345", API_URL);
            bool? result = null;

            // Expect the error log for 401 Unauthorized
            LogAssert.Expect(LogType.Error, new Regex(@"\[ForkTrackAPIClient\] Token validation failed.*401.*"));

            yield return invalidClient.ValidateToken(isValid =>
            {
                result = isValid;
            });

            Assert.IsNotNull(result, "Token validation should complete");
            Assert.IsFalse(result.Value, "Invalid token should fail validation");
        }

        #endregion

        #region List Graphs Tests

        [UnityTest]
        public IEnumerator ListGraphs_ShouldReturnGraphList()
        {
            ForkTrackAPIClient.GraphListResponse response = null;
            string error = null;

            yield return _client.ListGraphs(
                onSuccess: r => response = r,
                onError: e => error = e
            );

            Assert.IsNull(error, $"ListGraphs should not error: {error}");
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsNotNull(response.graphs, "Graphs array should not be null");

            Debug.Log($"[APIIntegrationTest] Found {response.graphs.Length} graphs");

            // Log graph details for verification
            foreach (var graph in response.graphs)
            {
                Debug.Log($"  - {graph.name} (id: {graph.id})");
            }
        }

        [UnityTest]
        public IEnumerator ListGraphs_GraphMetadata_ShouldHaveRequiredFields()
        {
            ForkTrackAPIClient.GraphListResponse response = null;

            yield return _client.ListGraphs(
                onSuccess: r => response = r,
                onError: e => Assert.Fail($"ListGraphs failed: {e}")
            );

            Assert.IsNotNull(response?.graphs, "Should have graphs");

            if (response.graphs.Length > 0)
            {
                var graph = response.graphs[0];

                Assert.IsNotNull(graph.id, "Graph should have id");
                Assert.IsNotNull(graph.name, "Graph should have name");
                // These fields may be null/empty but should exist
                Assert.IsNotNull(graph.owner_id, "Graph should have owner_id");
            }
        }

        #endregion

        #region Fetch Graph Tests

        [UnityTest]
        public IEnumerator FetchGraph_WithValidId_ShouldReturnGraphJson()
        {
            // First, get a graph ID from the list
            string graphId = null;

            yield return _client.ListGraphs(
                onSuccess: r =>
                {
                    if (r.graphs.Length > 0)
                    {
                        graphId = r.graphs[0].id;
                    }
                },
                onError: e => Assert.Fail($"ListGraphs failed: {e}")
            );

            if (string.IsNullOrEmpty(graphId))
            {
                Assert.Inconclusive("No graphs available to test FetchGraph");
                yield break;
            }

            string graphJson = null;
            string fetchError = null;

            yield return _client.FetchGraph(graphId,
                onSuccess: json => graphJson = json,
                onError: e => fetchError = e
            );

            Assert.IsNull(fetchError, $"FetchGraph should not error: {fetchError}");
            Assert.IsNotNull(graphJson, "Should receive graph JSON");
            Assert.That(graphJson, Does.Contain("schemaVersion"), "JSON should contain schemaVersion");
            Assert.That(graphJson, Does.Contain("nodes"), "JSON should contain nodes array");

            Debug.Log($"[APIIntegrationTest] Fetched graph {graphId}, JSON length: {graphJson.Length}");
        }

        [UnityTest]
        public IEnumerator FetchGraph_WithInvalidId_ShouldReturnError()
        {
            string graphJson = null;
            string fetchError = null;

            // Expect the error log for 400 Bad Request
            LogAssert.Expect(LogType.Error, new Regex(@"\[ForkTrackAPIClient\] Failed to fetch graph.*400.*"));

            yield return _client.FetchGraph("nonexistent-graph-id-12345",
                onSuccess: json => graphJson = json,
                onError: e => fetchError = e
            );

            Assert.IsNotNull(fetchError, "Should receive error for invalid graph ID");
            Assert.IsNull(graphJson, "Should not receive JSON for invalid graph ID");
        }

        #endregion

        #region End-to-End Tests

        [UnityTest]
        public IEnumerator EndToEnd_FetchAndLoadGraph_ShouldWork()
        {
            // 1. List graphs
            string graphId = null;

            yield return _client.ListGraphs(
                onSuccess: r =>
                {
                    if (r.graphs.Length > 0)
                    {
                        graphId = r.graphs[0].id;
                        Debug.Log($"[E2E] Selected graph: {r.graphs[0].name}");
                    }
                },
                onError: e => Assert.Fail($"ListGraphs failed: {e}")
            );

            if (string.IsNullOrEmpty(graphId))
            {
                Assert.Inconclusive("No graphs available for E2E test");
                yield break;
            }

            // 2. Fetch graph JSON
            string graphJson = null;

            yield return _client.FetchGraph(graphId,
                onSuccess: json => graphJson = json,
                onError: e => Assert.Fail($"FetchGraph failed: {e}")
            );

            // 3. Load into ForkTrack
            ForkTrack.ResetRuntime();

            ForkTrackGraph loadedGraph = null;
            string loadError = null;

            ForkTrack.OnGraphLoaded += g => loadedGraph = g;
            ForkTrack.OnGraphLoadError += e => loadError = e;

            ForkTrack.LoadGraphFromJSON(graphJson);

            Assert.IsNull(loadError, $"Loading graph should not error: {loadError}");
            Assert.IsNotNull(loadedGraph, "Graph should be loaded");
            Assert.IsTrue(ForkTrack.IsLoaded, "ForkTrack.IsLoaded should be true");

            Debug.Log($"[E2E] Successfully loaded graph with {loadedGraph.NodeCount} nodes, {loadedGraph.EdgeCount} edges");

            // 4. Verify we can query the graph
            var allNodes = ForkTrack.GetAllNodes();
            Assert.IsNotNull(allNodes);
            Assert.That(allNodes.Count, Is.GreaterThan(0), "Should have at least one node");

            // Cleanup
            ForkTrack.ResetRuntime();
        }

        #endregion
    }
}
