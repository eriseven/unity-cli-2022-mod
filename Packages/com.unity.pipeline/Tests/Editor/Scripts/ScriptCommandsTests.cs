using System;
using System.Collections;
using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Scripts;
using Unity.Pipeline.Models;
using Unity.Pipeline.Tests.Runtime; // AttachByPathFixture lives in the runtime test assembly (addable)
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Unity.Pipeline;

namespace Unity.Pipeline.Tests.Editor.Scripts
{
    /// <summary>Script authoring and serialized-field command coverage.</summary>
    class ScriptCommandsTests
    {
        private GameObject m_Go;
        private GameObject m_RefTarget;

        [SetUp]
        public void SetUp()
        {
            m_Go = new GameObject("CLI195_Subject");
            m_RefTarget = new GameObject("CLI195_RefTarget");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Go != null) Object.DestroyImmediate(m_Go);
            if (m_RefTarget != null) Object.DestroyImmediate(m_RefTarget);
            m_Go = null;
            m_RefTarget = null;
            ProjectPaths.ResetAuthoringRoot();

        }

        private static ObjectRef ById(Object o) => new ObjectRef { InstanceId = PipelineUtils.GetObjectId(o) };

        #region Direct — set/get round-trips

        [Test]
        public void SetThenGet_Primitive_RoundTrips()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            SerializedFieldCommands.SetField(ById(comp), "m_Speed",
                Newtonsoft.Json.Linq.JToken.FromObject(42));

            Assert.AreEqual(42, comp.Speed, "Direct field value should reflect the set");

            // Read it back through the command and confirm the reported value matches.
            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Speed");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
            Assert.AreEqual(42, (int)json["fields"][0]["value"]);
        }

        [Test]
        public void SetThenGet_Enum_RoundTripsByName()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            SerializedFieldCommands.SetField(ById(comp), "m_Mode",
                Newtonsoft.Json.Linq.JToken.FromObject("Aggressive"));

            Assert.AreEqual(ScriptCommandTestBehaviour.EnemyMode.Aggressive, comp.Mode);

            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Mode");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
            Assert.AreEqual("Aggressive", (string)json["fields"][0]["value"]);
        }

        [Test]
        public void SetThenGet_Vector3_RoundTrips()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            SerializedFieldCommands.SetField(ById(comp), "m_Offset",
                Newtonsoft.Json.Linq.JToken.FromObject(new { x = 1f, y = 2f, z = 3f }));

            Assert.AreEqual(new Vector3(1, 2, 3), comp.Offset);
        }

        #endregion

        #region Direct — object-reference wiring

        [Test]
        public void SetObjectReference_WiresAndReadsBackAsHandle()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            // Wire the [SerializeField] GameObject reference to another scene object by instanceId.
            SerializedFieldCommands.SetField(ById(comp), "m_Target",
                Newtonsoft.Json.Linq.JToken.FromObject(new { instanceId = PipelineUtils.GetObjectId(m_RefTarget) }));

            Assert.AreSame(m_RefTarget, comp.Target, "The reference should point at the wired object");

            // Reading it back should describe the referenced object as a re-usable handle.
            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Target");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
            var value = json["fields"][0]["value"];
            Assert.AreEqual(PipelineUtils.GetObjectId(m_RefTarget), value["instanceId"].ToObject<ObjectId>());
        }

        [Test]
        public void SetArrayElement_ResizesAndWiresObjectReference_RoundTrips()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            // Grow the array to one element via the native 'Array.size' path...
            SerializedFieldCommands.SetField(ById(comp), "m_Waypoints.Array.size",
                Newtonsoft.Json.Linq.JToken.FromObject(1));
            Assert.AreEqual(1, comp.Waypoints.Length, "Array.size should resize the backing array");

            // ...then wire element [0] to a scene object via the 'Array.data[i]' path.
            SerializedFieldCommands.SetField(ById(comp), "m_Waypoints.Array.data[0]",
                Newtonsoft.Json.Linq.JToken.FromObject(new { instanceId = PipelineUtils.GetObjectId(m_RefTarget) }));
            Assert.AreSame(m_RefTarget, comp.Waypoints[0], "Array element should point at the wired object");

            // The whole array reads back as an object reporting its length...
            var arr = SerializedFieldCommands.GetFields(ById(comp), "m_Waypoints");
            var arrJson = Newtonsoft.Json.Linq.JObject.FromObject(arr);
            Assert.AreEqual(1, (int)arrJson["fields"][0]["arrayLength"]);

            // ...and the element reads back as a re-usable handle to the referenced object.
            var elem = SerializedFieldCommands.GetFields(ById(comp), "m_Waypoints.Array.data[0]");
            var elemJson = Newtonsoft.Json.Linq.JObject.FromObject(elem);
            Assert.AreEqual(PipelineUtils.GetObjectId(m_RefTarget), elemJson["fields"][0]["value"]["instanceId"].ToObject<ObjectId>());
        }

        #endregion

        #region Direct — attach

        [Test]
        public void AttachScript_CompiledType_AddsComponent()
        {
            var result = AttachScriptCommand.Attach(ById(m_Go), nameof(ScriptCommandTestBehaviour));

            Assert.IsNotNull(m_Go.GetComponent<ScriptCommandTestBehaviour>(), "Component should be attached");
            Assert.AreEqual(nameof(ScriptCommandTestBehaviour), result.Type);
        }

        // CLI-224: explicitly pass type via the named arg form (script left null).
        [Test]
        public void AttachScript_ByType_NamedArg_AddsComponent()
        {
            var result = AttachScriptCommand.Attach(
                ById(m_Go), type: nameof(ScriptCommandTestBehaviour), script: null);

            Assert.IsNotNull(m_Go.GetComponent<ScriptCommandTestBehaviour>(), "Component should be attached by type");
            Assert.AreEqual(nameof(ScriptCommandTestBehaviour), result.Type);
        }

        // CLI-224: attach by ASSET PATH. The backing class is resolved from the .cs asset via
        // MonoScript.GetClass() (the agent passes a path, not a class name). Unity requires a
        // MonoBehaviour's file name to match its class name to add it, so the fixture file/class share
        // a name; the feature under test is path-based resolution rather than the name itself.
        [Test]
        public void AttachScript_ByScriptPath_ResolvesClassViaGetClass_AddsComponent()
        {
            var scriptPath = FixtureScriptPath();

            var result = AttachScriptCommand.Attach(ById(m_Go), type: null, script: scriptPath);

            Assert.IsNotNull(m_Go.GetComponent<AttachByPathFixture>(),
                "The fixture component should be attached by resolving its script asset path");
            Assert.AreEqual(nameof(AttachByPathFixture), result.Type,
                "Resolved type should come from MonoScript.GetClass() on the supplied path");
        }

        [Test]
        public void AttachScript_BothTypeAndScript_Throws()
        {
            var scriptPath = FixtureScriptPath();
            var ex = Assert.Throws<ArgumentException>(() =>
                AttachScriptCommand.Attach(
                    ById(m_Go), type: nameof(ScriptCommandTestBehaviour), script: scriptPath));
            StringAssert.Contains("not both", ex.Message);
            Assert.IsNull(m_Go.GetComponent<ScriptCommandTestBehaviour>(), "Nothing should attach on a bad-arg call");
            Assert.IsNull(m_Go.GetComponent<AttachByPathFixture>(), "Nothing should attach on a bad-arg call");
        }

        [Test]
        public void AttachScript_NeitherTypeNorScript_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                AttachScriptCommand.Attach(ById(m_Go), type: null, script: null));
            StringAssert.Contains("Provide either", ex.Message);
        }

        [Test]
        public void AttachScript_ByScriptPath_NoMonoScriptAtPath_ThrowsArgumentException()
        {
            // A path that resolves to no MonoScript asset is a caller input mistake (parameter
            // validation), not a recompile-recoverable failure — so it surfaces as ArgumentException.
            var ex = Assert.Throws<ArgumentException>(() =>
                AttachScriptCommand.Attach(
                    ById(m_Go), type: null, script: "Assets/__NoSuch__/Missing.cs"));
            StringAssert.Contains("No MonoScript", ex.Message);
        }

        /// <summary>
        /// Locate the on-disk script asset backing <see cref="AttachByPathFixture"/> in a
        /// path-independent way (the package may be imported from anywhere): search the AssetDatabase
        /// for the MonoScript by file name and confirm it via <see cref="MonoScript.GetClass"/>.
        /// </summary>
        private static string FixtureScriptPath()
        {
            // Locate the .cs asset backing AttachByPathFixture by file name, confirmed via
            // MonoScript.GetClass(). FindAssets indexes package assets, so this is robust regardless of
            // where the package is imported — and avoids MonoScript.FromMonoBehaviour, whose
            // AssetDatabase path can come back empty for a type in a package assembly.
            foreach (var guid in AssetDatabase.FindAssets("AttachByPathFixture t:MonoScript"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.EndsWith("/AttachByPathFixture.cs"))
                    continue;

                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
                if (mono != null && mono.GetClass() == typeof(AttachByPathFixture))
                    return p;
            }

            Assert.Ignore("AttachByPathFixture.cs MonoScript not found in the AssetDatabase; cannot exercise attach-by-path.");
            return null;
        }

        [Test]
        public void AttachScript_UncompiledType_ReturnsRecoverableError()
        {
            // Simulate the create-before-compile case: a type name that no loaded assembly knows.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AttachScriptCommand.Attach(ById(m_Go), "ThisTypeWasJustCreatedAndNotCompiledYet"));

            // The message must be recoverable: it should point the agent at the recompile flow.
            StringAssert.Contains("script recompile", ex.Message.ToLowerInvariant());
            Assert.IsNull(m_Go.GetComponent("ThisTypeWasJustCreatedAndNotCompiledYet"),
                "Nothing should be attached when the type is unknown");
        }

        [Test]
        public void AttachScript_NonMonoBehaviourType_Throws()
        {
            // A component target is fine (we read its gameObject), but a non-MonoBehaviour type is not.
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AttachScriptCommand.Attach(ById(comp), nameof(System.String)));
            StringAssert.Contains("MonoBehaviour", ex.Message);
        }

        #endregion

        #region ViaClient

        [Test]
        public void GetSerializedFields_ViaClient_NullValue_ReturnsExplicitNullResult()
        {
            // The full /api/exec path for a genuinely-null command result: a format="value" read
            // of an unassigned object-reference field is a SUCCESS whose value is null, and the
            // wire reply must carry an explicit "result":null — distinguishable from a reply with
            // no result at all (AUTHAPI-21 review).
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("component get_fields", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(comp) },
                    field = "m_Target",
                    format = "value"
                });

                Assert.IsTrue(response.IsSuccess, $"get should succeed: {response.Error} / {response.RawResponse}");
                Assert.IsTrue(response.HasValidJson);
                Assert.IsTrue(response.JsonResponse["success"].ToObject<bool>());
                StringAssert.Contains("\"result\":null", response.RawResponse,
                    "A null field value must arrive as an explicit null result on the wire");
            }
        }

        [Test]
        public void SetThenGet_ViaClient_RoundTrips()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            using (var server = new PipelineTestServer())
            {
                var setResponse = server.Execute("component set_field", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(comp) },
                    field = "m_Speed",
                    value = 7
                });
                Assert.IsTrue(setResponse.IsSuccess, $"set should succeed: {setResponse.Error} / {setResponse.RawResponse}");
                Assert.AreEqual(7, comp.Speed);

                var getResponse = server.Execute("component get_fields", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(comp) },
                    field = "m_Speed"
                });
                Assert.IsTrue(getResponse.IsSuccess, $"get should succeed: {getResponse.Error}");
                Assert.IsTrue(getResponse.HasValidJson);
                var value = getResponse.JsonResponse["result"]["fields"][0]["value"];
                Assert.AreEqual(7, (int)value);
            }
        }

        [Test]
        public void AttachScript_ViaClient_UncompiledType_ReturnsErrorResponse()
        {
            using (var server = new PipelineTestServer())
            {
                // attach_script logs a Unity [Error] for the uncompiled type (the server surfaces the
                // command failure via Debug.LogError). Expect it so the unhandled-log check passes.
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                    new System.Text.RegularExpressions.Regex("was not found in any loaded assembly"));

                var response = server.Execute("script attach", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(m_Go) },
                    type = "DefinitelyNotCompiledYetComponent"
                });

                // The recoverable error is surfaced as a command failure (HTTP 400), not a crash.
                Assert.IsFalse(response.IsSuccess, "Attaching an uncompiled type should fail at the command level");
                StringAssert.Contains("script recompile", response.RawResponse.ToLowerInvariant(),
                    "The error should tell the agent to recompile and retry");
            }
        }

        // CLI-224: attach by --script (asset path) over HTTP.
        [Test]
        public void AttachScript_ViaClient_ByScriptPath_AddsComponent()
        {
            var scriptPath = FixtureScriptPath();
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("script attach", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(m_Go) },
                    script = scriptPath
                });

                Assert.IsTrue(response.IsSuccess, $"attach by script path should succeed: {response.Error} / {response.RawResponse}");
                Assert.IsNotNull(m_Go.GetComponent<AttachByPathFixture>(),
                    "Component should be attached via the script asset path over HTTP");
            }
        }

        #endregion

        #region CLI-225 — get/set serialized fields by GameObject + component

        [Test]
        public void GetSerializedFields_ByGameObjectAndComponent_ReturnsFields()
        {
            var rb = m_Go.AddComponent<Rigidbody>();

            // Address the component via the GameObject handle + a component type name.
            var read = SerializedFieldCommands.GetFields(ById(m_Go), field: "m_Mass", component: "Rigidbody");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);

            Assert.AreEqual("Rigidbody", (string)json["type"], "Resolved object should be the Rigidbody, not the GameObject");
            Assert.AreEqual(rb.mass, (double)json["fields"][0]["value"], 0.001,
                "Reported value should be the component's live field value");
        }

        [Test]
        public void SetSerializedField_ByGameObjectAndComponent_SetsAndReadsBack()
        {
            var rb = m_Go.AddComponent<Rigidbody>();

            SerializedFieldCommands.SetField(ById(m_Go), "m_Mass",
                Newtonsoft.Json.Linq.JToken.FromObject(13.5), component: "Rigidbody");

            Assert.AreEqual(13.5f, rb.mass, 0.001f, "Mass should be set on the GO's Rigidbody");

            var read = SerializedFieldCommands.GetFields(ById(m_Go), field: "m_Mass", component: "Rigidbody");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
            Assert.AreEqual(13.5, (double)json["fields"][0]["value"], 0.001, "Read-back should reflect the set value");
        }

        [Test]
        public void GetSerializedFields_MultipleSameTypeComponents_ThrowsListingInstanceIds()
        {
            // Two Rigidbodies aren't allowed ([DisallowMultipleComponent]); use a test MonoBehaviour
            // that permits duplicates so the multi-match disambiguation path is reachable.
            var first = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            var second = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            var ex = Assert.Throws<ArgumentException>(() =>
                SerializedFieldCommands.GetFields(
                    ById(m_Go), component: nameof(ScriptCommandTestBehaviour)));

            // The error must list EACH instanceId so the agent can re-address unambiguously.
            StringAssert.Contains(PipelineUtils.GetObjectId(first).ToString(), ex.Message);
            StringAssert.Contains(PipelineUtils.GetObjectId(second).ToString(), ex.Message);
        }

        [Test]
        public void GetSerializedFields_AssetTargetWithComponent_Throws()
        {
            // --component is only meaningful for a GameObject target. Supplying it for an asset (here a
            // ScriptableObject, which has no components) is a misrouted request and must be rejected,
            // not silently ignored.
            var so = ScriptableObject.CreateInstance<ScriptCommandTestScriptable>();
            try
            {
                var ex = Assert.Throws<ArgumentException>(() =>
                    SerializedFieldCommands.GetFields(ById(so), component: "Rigidbody"));
                StringAssert.Contains("asset", ex.Message.ToLowerInvariant(),
                    "The error should explain that an asset has no components");
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        [Test]
        public void GetSerializedFields_GameObjectWithoutComponent_ErrorMentionsComponentOption()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                SerializedFieldCommands.GetFields(ById(m_Go)));

            StringAssert.Contains("--component", ex.Message,
                "The GameObject-without-component error should point at the new --component option");
        }

        [Test]
        public void GetSerializedFields_BareComponentInstanceId_StillWorks()
        {
            // The original addressing form (a component handle, no 'component' arg) must keep working.
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();

            SerializedFieldCommands.SetField(ById(comp), "m_Speed",
                Newtonsoft.Json.Linq.JToken.FromObject(5));
            Assert.AreEqual(5, comp.Speed);

            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Speed");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
            Assert.AreEqual(5, (int)json["fields"][0]["value"]);
        }

        [Test]
        public void SetSerializedField_ByGameObjectAndComponent_ViaClient_RoundTrips()
        {
            var rb = m_Go.AddComponent<Rigidbody>();
            using (var server = new PipelineTestServer())
            {
                var setResponse = server.Execute("component set_field", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(m_Go) },
                    component = "Rigidbody",
                    field = "m_Mass",
                    value = 8.25
                });
                Assert.IsTrue(setResponse.IsSuccess, $"set by GO+component should succeed: {setResponse.Error} / {setResponse.RawResponse}");
                Assert.AreEqual(8.25f, rb.mass, 0.001f);

                var getResponse = server.Execute("component get_fields", new
                {
                    target = new { instanceId = PipelineUtils.GetObjectId(m_Go) },
                    component = "Rigidbody",
                    field = "m_Mass"
                });
                Assert.IsTrue(getResponse.IsSuccess, $"get by GO+component should succeed: {getResponse.Error}");
                var value = getResponse.JsonResponse["result"]["fields"][0]["value"];
                Assert.AreEqual(8.25, (double)value, 0.001);
            }
        }

        [Test]
        public void SetSerializedField_WholeArray_OfObjectRefs_RoundTrips()
        {
            // CLI-220 follow-up parity: set a whole array field from a single JSON array (rather than
            // element-by-element via "field.Array.data[i]").
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            var a = new GameObject("WP_A");
            var b = new GameObject("WP_B");
            try
            {
                SerializedFieldCommands.SetField(ById(comp), "m_Waypoints",
                    new Newtonsoft.Json.Linq.JArray(
                        Newtonsoft.Json.Linq.JToken.FromObject(new { instanceId = PipelineUtils.GetObjectId(a) }),
                        Newtonsoft.Json.Linq.JToken.FromObject(new { instanceId = PipelineUtils.GetObjectId(b) })));

                Assert.AreEqual(2, comp.Waypoints.Length, "Whole array should be set from a JSON array");
                Assert.AreSame(a, comp.Waypoints[0]);
                Assert.AreSame(b, comp.Waypoints[1]);

                var read = SerializedFieldCommands.GetFields(ById(comp), "m_Waypoints");
                var json = Newtonsoft.Json.Linq.JObject.FromObject(read);
                Assert.AreEqual(2, (int)json["fields"][0]["arrayLength"], "get should report the array length");
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void SetSerializedField_UnresolvableObjectRef_Throws()
        {
            // Previously an unresolved handle was silently dropped (no-op success); it must now throw.
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            Assert.Throws<ArgumentException>(() =>
                SerializedFieldCommands.SetField(ById(comp), "m_Target",
                    Newtonsoft.Json.Linq.JToken.FromObject(new { instanceId = 999999999 })));
        }

        #endregion

        #region AUTHAPI-21 — value projection (format=value)

        [Test]
        public void GetSerializedFields_FormatValue_SingleField_ReturnsRawValue()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            SerializedFieldCommands.SetField(ById(comp), "m_Speed",
                Newtonsoft.Json.Linq.JToken.FromObject(9));

            // format=value returns the scalar directly rather than a { type, fields:[{...}] } descriptor.
            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Speed", format: "value");

            Assert.IsFalse(read is System.Collections.Generic.Dictionary<string, object>,
                "A single-field value read should return the raw value, not a map");
            Assert.AreEqual(9L, Convert.ToInt64(read), "value mode should return the raw field value");
        }

        [Test]
        public void GetSerializedFields_FormatValue_AllFields_ReturnsNameValueMap()
        {
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            SerializedFieldCommands.SetField(ById(comp), "m_Speed",
                Newtonsoft.Json.Linq.JToken.FromObject(3));

            var read = SerializedFieldCommands.GetFields(ById(comp), format: "value");

            var map = read as System.Collections.Generic.Dictionary<string, object>;
            Assert.IsNotNull(map, "value mode over all fields should return a name->value map");
            Assert.IsTrue(map.ContainsKey("m_Speed"), "map should be keyed by field name");
            Assert.AreEqual(3L, Convert.ToInt64(map["m_Speed"]), "map value should be the raw field value");
        }

        [Test]
        public void GetSerializedFields_DefaultFormat_StillReturnsDescriptor()
        {
            // Back-compat: with no format arg the full per-field descriptor is unchanged.
            var comp = m_Go.AddComponent<ScriptCommandTestBehaviour>();
            SerializedFieldCommands.SetField(ById(comp), "m_Speed",
                Newtonsoft.Json.Linq.JToken.FromObject(4));

            var read = SerializedFieldCommands.GetFields(ById(comp), "m_Speed");
            var json = Newtonsoft.Json.Linq.JObject.FromObject(read);

            Assert.AreEqual("m_Speed", (string)json["fields"][0]["name"], "Descriptor should carry the field name");
            Assert.AreEqual(4, (int)json["fields"][0]["value"], "Descriptor should carry the value");
        }

        #endregion
    }

    /// <summary>
    /// An already-compiled MonoBehaviour living in the test assembly, used as the subject for the
    /// set/get/attach tests so they don't need a domain reload. Mirrors the kinds of fields an agent
    /// authors: a primitive, an enum, a Vector3, and a [SerializeField] object reference.
    /// </summary>
    class ScriptCommandTestBehaviour : MonoBehaviour
    {
        public enum EnemyMode { Passive, Aggressive, Patrol }

        [SerializeField] private int m_Speed;
        [SerializeField] private EnemyMode m_Mode;
        [SerializeField] private Vector3 m_Offset;
        [SerializeField] private GameObject m_Target;
        [SerializeField] private GameObject[] m_Waypoints = Array.Empty<GameObject>();

        // SerializedProperty paths use the serialized field names; with m_ private fields Unity's
        // serialized name is the field name itself ("m_Speed"). The tests address them via the
        // editor-friendly accessors below for assertions, and via the serialized name for the
        // command 'field' argument.
        public int Speed => m_Speed;
        public EnemyMode Mode => m_Mode;
        public Vector3 Offset => m_Offset;
        public GameObject Target => m_Target;
        public GameObject[] Waypoints => m_Waypoints;
    }

    /// <summary>
    /// A trivial ScriptableObject fixture used to exercise the asset-target path of the serialized-field
    /// commands (an asset is neither a Component nor a GameObject).
    /// </summary>
    class ScriptCommandTestScriptable : ScriptableObject
    {
        [SerializeField] private int m_Value;
        public int Value => m_Value;
    }

    // Keep compilation in a dedicated fixture's UnitySetUp/UnityTearDown. Test Framework 1.4
    // restores coroutine program counters across domain reloads; the body must not share
    // that state with the ordinary script-command fixture after earlier PlayMode tests.
    class CreateScriptCommandTests
    {
        const string Folder = "Assets/__CLI195Test";
        const string AssetPath = Folder + "/CLI195Generated.cs";
        const string OwnedKey = "Pipeline.Tests.CLI195TestFolderOwned";

        [UnitySetUp]
        public IEnumerator CreateAndCompile()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(Folder), "Test folder must not already exist");
            AssetDatabase.CreateFolder("Assets", "__CLI195Test");
            SessionState.SetBool(OwnedKey, true);
            var result = CreateScriptCommand.Create("CLI195Generated", Folder, "Game.Generated");
            Assert.AreEqual(AssetPath, result.AssetPath);
            Assert.IsTrue(System.IO.File.Exists(AssetPath));
            yield return new RecompileScripts();
        }

        [Test]
        public void CreateScript_WritesFileAndReturnsAssetIdentity()
        {
            Assert.IsTrue(System.IO.File.Exists(AssetPath));
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetPath);
            Assert.IsNotNull(script);
            Assert.IsNotNull(script.GetClass(), "The generated script must compile to an attachable type");
            Assert.AreEqual("Game.Generated.CLI195Generated", script.GetClass().FullName);
        }

        [UnityTearDown]
        public IEnumerator DeleteAndRecompile()
        {
            if (!SessionState.GetBool(OwnedKey, false)) yield break;
            SessionState.EraseBool(OwnedKey);
            AssetDatabase.DeleteAsset(Folder);
            yield return new RecompileScripts();
        }
    }

}
