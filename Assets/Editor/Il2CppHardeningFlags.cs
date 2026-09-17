using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class Il2CppHardeningFlags : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string addlArgs = string.Empty;

        bool isWindows64 =
            report.summary.platform == BuildTarget.StandaloneWindows64;

        bool isIl2Cpp =
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) ==
            ScriptingImplementation.IL2CPP;

        if (isWindows64 && isIl2Cpp)
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(
                NamedBuildTarget.Standalone,
                Il2CppCompilerConfiguration.Master);

            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.Standalone,
                Il2CppCodeGeneration.OptimizeSpeed);

            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Standalone,
                ManagedStrippingLevel.High);

            PlayerSettings.stripEngineCode = true;

            addlArgs =
                "--compiler-flags=\"-EHs -GF -Gy -GL -Gw -guard:cf -guard:ehcont -Qspectre-load-cf\" " +
                "--linker-flags=\"-LTCG -OPT:REF -OPT:ICF -DYNAMICBASE -NXCOMPAT -HIGHENTROPYVA -GUARD:CF -CETCOMPAT\"";
        }

        PlayerSettings.SetAdditionalIl2CppArgs(addlArgs);
    }
}