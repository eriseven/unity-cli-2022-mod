using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Reflection
{
    /// <summary>
    /// Opt-in reflection bridge for diagnostics and tightly controlled editor automation.
    /// Invocation is deliberately confirmation-gated: calling arbitrary project methods can mutate
    /// scenes, assets, or external state just as surely as a purpose-built command can.
    /// </summary>
    public static class ReflectionCommands
    {
        private const BindingFlags PublicFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private const BindingFlags NonPublicFlags = PublicFlags | BindingFlags.NonPublic;

        [CliCommand("find_methods", "Find methods on a loaded type and return their callable signatures (read-only). Use a fully-qualified type name when a short name is ambiguous.")]
        public static ReflectionMethodListResult FindMethods(
            [CliArg("type", "Fully-qualified (preferred) or unambiguous short type name.", Required = true)] string type,
            [CliArg("name", "Optional exact method-name filter.")] string name = null,
            [CliArg("include_non_public", "Include non-public methods. Defaults to false for safer discovery.")] bool includeNonPublic = false)
        {
            var resolvedType = ResolveType(type);
            var flags = includeNonPublic ? NonPublicFlags : PublicFlags;
            var methods = resolvedType.GetMethods(flags)
                .Where(method => !method.IsSpecialName)
                .Where(method => string.IsNullOrWhiteSpace(name) || string.Equals(method.Name, name, StringComparison.Ordinal))
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ThenBy(method => method.ToString(), StringComparer.Ordinal)
                .Select(DescribeMethod)
                .ToArray();

            return new ReflectionMethodListResult
            {
                Type = resolvedType.FullName,
                Methods = methods
            };
        }

        [CliCommand("get_type_schema", "Describe a loaded type's public fields, properties, and methods in a JSON-friendly schema (read-only). This is reflection metadata, not a Unity serialized asset schema.")]
        public static ReflectionTypeSchema GetTypeSchema(
            [CliArg("type", "Fully-qualified (preferred) or unambiguous short type name.", Required = true)] string type,
            [CliArg("include_non_public", "Include non-public members. Defaults to false.")] bool includeNonPublic = false)
        {
            var resolvedType = ResolveType(type);
            var flags = includeNonPublic ? NonPublicFlags : PublicFlags;

            return new ReflectionTypeSchema
            {
                Type = resolvedType.FullName,
                BaseType = resolvedType.BaseType?.FullName,
                IsAbstract = resolvedType.IsAbstract,
                IsEnum = resolvedType.IsEnum,
                Fields = resolvedType.GetFields(flags)
                    .Where(field => !field.IsSpecialName)
                    .OrderBy(field => field.Name, StringComparer.Ordinal)
                    .Select(field => new ReflectionMemberInfo
                    {
                        Name = field.Name,
                        Type = FriendlyTypeName(field.FieldType),
                        IsStatic = field.IsStatic,
                        CanRead = true,
                        CanWrite = !field.IsInitOnly
                    }).ToArray(),
                Properties = resolvedType.GetProperties(flags)
                    .Where(property => property.GetIndexParameters().Length == 0)
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new ReflectionMemberInfo
                    {
                        Name = property.Name,
                        Type = FriendlyTypeName(property.PropertyType),
                        IsStatic = (property.GetMethod ?? property.SetMethod)?.IsStatic ?? false,
                        CanRead = property.GetMethod != null,
                        CanWrite = property.SetMethod != null
                    }).ToArray(),
                Methods = resolvedType.GetMethods(flags)
                    .Where(method => !method.IsSpecialName)
                    .OrderBy(method => method.Name, StringComparer.Ordinal)
                    .ThenBy(method => method.ToString(), StringComparer.Ordinal)
                    .Select(DescribeMethod)
                    .ToArray()
            };
        }

        [CliCommand("invoke_method", "Invoke a reflected public method. confirm=true is required because arbitrary method calls can mutate project state; use dry_run first to validate overload selection.")]
        public static ReflectionInvocationResult InvokeMethod(
            [CliArg("method", "Exact method name to invoke.", Required = true)] string method,
            [CliArg("target", "Optional ObjectRef target for an instance method. Omit for a static method.")] ObjectRef target = null,
            [CliArg("type", "Required for static methods; optional for instance methods when target is supplied.")] string type = null,
            [CliArg("arguments_json", "JSON array of arguments, e.g. [42,\"name\"]. Unity object parameters accept an ObjectRef-shaped JSON value. Defaults to [].")] string argumentsJson = "[]",
            [CliArg("include_non_public", "Allow non-public method invocation. Requires confirm=true and should be used only for project-owned code.")] bool includeNonPublic = false,
            [CliArg("confirm", "Must be true because the target method may mutate state or invoke external code.", Required = true)] bool confirm = false,
            [CliArg("dry_run", "When true, return the resolved signature without invoking it.")] bool dryRun = false)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("method is required.");
            if (!confirm)
                throw new ArgumentException("invoke_method requires confirm=true because reflected method execution can mutate arbitrary state. Use dry_run=true first.");

            Object targetObject = null;
            if (target != null && !target.IsEmpty)
            {
                if (!ObjectResolver.TryResolve(target, out targetObject, out var targetError))
                    throw new ArgumentException($"Could not resolve target: {targetError}");
            }

            var resolvedType = targetObject != null ? targetObject.GetType() : ResolveType(type);
            if (targetObject != null && !string.IsNullOrWhiteSpace(type))
            {
                var declaredType = ResolveType(type);
                if (!declaredType.IsAssignableFrom(resolvedType))
                    throw new ArgumentException($"Target type '{resolvedType.FullName}' is not assignable to declared type '{declaredType.FullName}'.");
                resolvedType = declaredType;
            }
            if (targetObject == null && string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("type is required when target is omitted (static method invocation).");

            JArray arguments;
            try { arguments = JArray.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "[]" : argumentsJson); }
            catch (JsonReaderException ex) { throw new ArgumentException($"arguments_json must be a valid JSON array: {ex.Message}"); }

            var flags = includeNonPublic ? NonPublicFlags : PublicFlags;
            var candidates = resolvedType.GetMethods(flags)
                .Where(candidate => !candidate.IsSpecialName && !candidate.ContainsGenericParameters)
                .Where(candidate => string.Equals(candidate.Name, method, StringComparison.Ordinal))
                .Where(candidate => targetObject == null ? candidate.IsStatic : !candidate.IsStatic)
                .Where(candidate => candidate.GetParameters().Length == arguments.Count)
                .ToArray();

            MethodInfo selected = null;
            object[] selectedArgs = null;
            foreach (var candidate in candidates)
            {
                if (!TryConvertArguments(arguments, candidate.GetParameters(), out var converted))
                    continue;

                if (selected != null)
                    throw new ArgumentException($"Method '{method}' on '{resolvedType.FullName}' is ambiguous for the supplied arguments. Use a less ambiguous type or argument shape.");

                selected = candidate;
                selectedArgs = converted;
            }

            if (selected == null)
            {
                var available = candidates.Length == 0
                    ? "No method with the requested static/instance mode and argument count was found."
                    : "No overload could accept the supplied JSON argument values.";
                throw new ArgumentException($"Could not resolve '{method}' on '{resolvedType.FullName}'. {available}");
            }

            var signature = DescribeMethod(selected);
            if (dryRun)
            {
                return new ReflectionInvocationResult
                {
                    Invoked = false,
                    DryRun = true,
                    Type = resolvedType.FullName,
                    Signature = signature
                };
            }

            object returnValue;
            try
            {
                returnValue = selected.Invoke(targetObject, selectedArgs);
            }
            catch (TargetInvocationException ex)
            {
                var cause = ex.InnerException ?? ex;
                throw new InvalidOperationException($"'{signature.Signature}' threw {cause.GetType().Name}: {cause.Message}", cause);
            }

            return new ReflectionInvocationResult
            {
                Invoked = true,
                DryRun = false,
                Type = resolvedType.FullName,
                Signature = signature,
                ReturnValue = NormalizeReturnValue(returnValue)
            };
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("type is required.");

            var direct = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (direct != null)
                return direct;

            var matches = new List<Type>();
            foreach (var assembly in PipelineUtils.GetLoadedAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(value => value != null).ToArray(); }

                matches.AddRange(types.Where(value =>
                    string.Equals(value.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(value.Name, typeName, StringComparison.Ordinal)));
            }

            matches = matches.Distinct().ToList();
            if (matches.Count == 1)
                return matches[0];
            if (matches.Count == 0)
                throw new ArgumentException($"Could not resolve type '{typeName}'. Use an assembly-qualified or fully-qualified type name.");
            throw new ArgumentException($"Type name '{typeName}' is ambiguous. Use an assembly-qualified name.");
        }

        private static bool TryConvertArguments(JArray supplied, ParameterInfo[] parameters, out object[] converted)
        {
            converted = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (parameter.IsOut || parameter.ParameterType.IsByRef || parameter.ParameterType.IsPointer)
                    return false;
                if (!TryConvert(supplied[index], parameter.ParameterType, out converted[index]))
                    return false;
            }
            return true;
        }

        private static bool TryConvert(JToken token, Type destination, out object value)
        {
            value = null;
            if (token == null || token.Type == JTokenType.Null)
            {
                if (!destination.IsValueType || Nullable.GetUnderlyingType(destination) != null)
                    return true;
                return false;
            }

            try
            {
                var nullable = Nullable.GetUnderlyingType(destination);
                if (nullable != null)
                    destination = nullable;

                if (typeof(Object).IsAssignableFrom(destination))
                {
                    var reference = token.ToObject<ObjectRef>();
                    if (reference == null || reference.IsEmpty || !ObjectResolver.TryResolve(reference, out var referenced, out _))
                        return false;
                    if (!destination.IsInstanceOfType(referenced))
                        return false;
                    value = referenced;
                    return true;
                }

                if (destination.IsEnum)
                {
                    value = token.Type == JTokenType.String
                        ? Enum.Parse(destination, token.Value<string>(), ignoreCase: true)
                        : Enum.ToObject(destination, token.Value<long>());
                    return true;
                }

                if (destination == typeof(string))
                {
                    value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None);
                    return true;
                }

                if (destination == typeof(bool))
                {
                    value = token.Value<bool>();
                    return true;
                }

                if (destination == typeof(char))
                {
                    var text = token.Value<string>();
                    if (string.IsNullOrEmpty(text) || text.Length != 1)
                        return false;
                    value = text[0];
                    return true;
                }

                if (destination.IsPrimitive || destination == typeof(decimal))
                {
                    value = Convert.ChangeType(((JValue)token).Value, destination, CultureInfo.InvariantCulture);
                    return true;
                }

                value = token.ToObject(destination);
                return value != null || !destination.IsValueType;
            }
            catch
            {
                return false;
            }
        }

        private static object NormalizeReturnValue(object value)
        {
            if (value is Object unityObject)
                return ObjectResolver.Describe(unityObject);
            return value;
        }

        private static ReflectionMethodInfo DescribeMethod(MethodInfo method)
        {
            return new ReflectionMethodInfo
            {
                Name = method.Name,
                Signature = FriendlyMethodSignature(method),
                ReturnType = FriendlyTypeName(method.ReturnType),
                IsStatic = method.IsStatic,
                IsPublic = method.IsPublic,
                Parameters = method.GetParameters().Select(parameter => new ReflectionParameterInfo
                {
                    Name = parameter.Name,
                    Type = FriendlyTypeName(parameter.ParameterType),
                    Optional = parameter.IsOptional
                }).ToArray()
            };
        }

        private static string FriendlyMethodSignature(MethodInfo method)
        {
            return $"{FriendlyTypeName(method.ReturnType)} {method.DeclaringType?.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => $"{FriendlyTypeName(parameter.ParameterType)} {parameter.Name}"))})";
        }

        private static string FriendlyTypeName(Type type)
        {
            return type?.FullName ?? type?.Name ?? "void";
        }
    }

    [Serializable]
    public sealed class ReflectionMethodListResult
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("methods")] public ReflectionMethodInfo[] Methods { get; set; }
    }

    [Serializable]
    public sealed class ReflectionTypeSchema
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("baseType")] public string BaseType { get; set; }
        [JsonProperty("isAbstract")] public bool IsAbstract { get; set; }
        [JsonProperty("isEnum")] public bool IsEnum { get; set; }
        [JsonProperty("fields")] public ReflectionMemberInfo[] Fields { get; set; }
        [JsonProperty("properties")] public ReflectionMemberInfo[] Properties { get; set; }
        [JsonProperty("methods")] public ReflectionMethodInfo[] Methods { get; set; }
    }

    [Serializable]
    public sealed class ReflectionMemberInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("isStatic")] public bool IsStatic { get; set; }
        [JsonProperty("canRead")] public bool CanRead { get; set; }
        [JsonProperty("canWrite")] public bool CanWrite { get; set; }
    }

    [Serializable]
    public sealed class ReflectionMethodInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("signature")] public string Signature { get; set; }
        [JsonProperty("returnType")] public string ReturnType { get; set; }
        [JsonProperty("isStatic")] public bool IsStatic { get; set; }
        [JsonProperty("isPublic")] public bool IsPublic { get; set; }
        [JsonProperty("parameters")] public ReflectionParameterInfo[] Parameters { get; set; }
    }

    [Serializable]
    public sealed class ReflectionParameterInfo
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("optional")] public bool Optional { get; set; }
    }

    [Serializable]
    public sealed class ReflectionInvocationResult
    {
        [JsonProperty("invoked")] public bool Invoked { get; set; }
        [JsonProperty("dryRun")] public bool DryRun { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("signature")] public ReflectionMethodInfo Signature { get; set; }
        [JsonProperty("returnValue")] public object ReturnValue { get; set; }
    }
}
