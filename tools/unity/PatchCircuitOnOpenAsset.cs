// Remove only the obsolete OnOpenAsset registration from the precompiled Bot SDK editor.
// A Unity 6000.6-compatible callback in CircuitWindowUnityCallbacks.cs reproduces the
// original method using EntityIdToObject, so Bot SDK documents still open in Circuit.
using System;
using Mono.Cecil;

internal static class PatchCircuitOnOpenAsset {
  private const string AttributeName = "UnityEditor.Callbacks.OnOpenAssetAttribute";

  private static int Main(string[] args) {
    if (args.Length != 2) {
      Console.Error.WriteLine("Usage: PatchCircuitOnOpenAsset <input.dll> <output.dll>");
      return 2;
    }

    try {
      using (var assembly = AssemblyDefinition.ReadAssembly(args[0])) {
        var type = assembly.MainModule.GetType("Circuit.CircuitEditorCallbacks");
        if (type == null) throw new InvalidOperationException("Circuit callback type not found");

        MethodDefinition method = null;
        foreach (var candidate in type.Methods) {
          if (candidate.Name == "OpenCircuitAsset" && candidate.IsPublic && candidate.IsStatic &&
              candidate.Parameters.Count == 2 &&
              candidate.Parameters[0].ParameterType.FullName == "System.Int32" &&
              candidate.Parameters[1].ParameterType.FullName == "System.Int32" &&
              candidate.ReturnType.FullName == "System.Boolean") {
            if (method != null) throw new InvalidOperationException("Multiple matching callback methods");
            method = candidate;
          }
        }
        if (method == null) throw new InvalidOperationException("Original callback method not found");

        CustomAttribute obsoleteRegistration = null;
        foreach (var attribute in method.CustomAttributes) {
          if (attribute.AttributeType.FullName == AttributeName) {
            if (obsoleteRegistration != null) throw new InvalidOperationException("Multiple OnOpenAsset attributes");
            obsoleteRegistration = attribute;
          }
        }
        if (obsoleteRegistration == null) throw new InvalidOperationException("OnOpenAsset attribute not found; assembly may already be patched");

        method.CustomAttributes.Remove(obsoleteRegistration);
        assembly.Write(args[1]);
      }

      using (var check = AssemblyDefinition.ReadAssembly(args[1])) {
        var type = check.MainModule.GetType("Circuit.CircuitEditorCallbacks");
        foreach (var method in type.Methods) {
          foreach (var attribute in method.CustomAttributes) {
            if (attribute.AttributeType.FullName == AttributeName) {
              throw new InvalidOperationException("Patched callback still has OnOpenAsset");
            }
          }
        }
      }

      Console.WriteLine("Removed obsolete OnOpenAsset registration; original callback method retained.");
      return 0;
    } catch (Exception exception) {
      Console.Error.WriteLine(exception);
      return 1;
    }
  }
}
